// Source/WindowsPrivacyPlatform.CLI/TuiHost.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.CLI
{
    /// <summary>
    /// Thin read-only terminal UI. Presentation only.
    /// Consumes NavigationBuilder + SettingsQuery + SettingDetailView.
    /// No business logic, no writes, no elevation.
    /// </summary>
    internal static class TuiHost
    {
        private enum Screen
        {
            Domains,
            Features,
            Settings,
            Detail,
            Search
        }

        private static bool _altScreen;

        public static void Run(IReadOnlyList<ManagedObject> catalog, SettingsQuery query)
        {
            if (catalog is null || catalog.Count == 0)
            {
                Console.WriteLine("No catalog entries available.");
                return;
            }

            query ??= new SettingsQuery(catalog);
            var root = NavigationBuilder.BuildDomainTree(catalog);

            var screen = Screen.Domains;
            NavigationNode? currentDomain = null;
            NavigationNode? currentFeature = null;
            int cursor = 0;
            string searchTerm = string.Empty;
            List<NavigationNode> searchResults = new();
            int detailScroll = 0;

            EnterAltScreen();
            Console.CursorVisible = false;
            try
            {
                while (true)
                {
                    BeginFrame();
                    WriteHeader(screen, currentDomain, currentFeature);

                    switch (screen)
                    {
                        case Screen.Domains:
                            RenderNodeList(root.Children, cursor, "Choose a domain", root);
                            break;
                        case Screen.Features:
                            if (currentDomain is not null)
                                RenderNodeList(currentDomain.Children, cursor, currentDomain.Title, currentDomain);
                            break;
                        case Screen.Settings:
                            if (currentFeature is not null)
                                RenderNodeList(currentFeature.Children, cursor, currentFeature.Title, currentFeature);
                            break;
                        case Screen.Detail:
                            if (currentFeature is not null && cursor >= 0 && cursor < currentFeature.Children.Count)
                            {
                                var settingNode = currentFeature.Children[cursor];
                                var mo = query.GetById(settingNode.ObjectId ?? string.Empty);
                                if (mo is not null)
                                    RenderDetail(NavigationBuilder.BuildDetail(mo, query), mo, detailScroll);
                                else
                                    Console.WriteLine("  Setting not found in catalog.");
                            }
                            break;
                        case Screen.Search:
                            RenderSearch(searchTerm, searchResults, cursor);
                            break;
                    }

                    WriteFooter(screen);

                    var key = Console.ReadKey(intercept: true);

                    if (key.Key == ConsoleKey.Q && screen != Screen.Search)
                        break;

                    // Detail page scroll
                    if (screen == Screen.Detail)
                    {
                        if (key.Key == ConsoleKey.DownArrow || key.Key == ConsoleKey.PageDown)
                        {
                            detailScroll = Math.Min(detailScroll + 1, 40);
                            continue;
                        }
                        if (key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.PageUp)
                        {
                            detailScroll = Math.Max(detailScroll - 1, 0);
                            continue;
                        }
                    }

                    if (key.Key == ConsoleKey.Escape ||
                        (key.Key == ConsoleKey.Backspace && screen != Screen.Search))
                    {
                        detailScroll = 0;
                        if (screen == Screen.Detail)
                        {
                            screen = Screen.Settings;
                            continue;
                        }
                        if (screen == Screen.Settings)
                        {
                            screen = Screen.Features;
                            cursor = SafeIndex(currentDomain?.Children, currentFeature);
                            continue;
                        }
                        if (screen == Screen.Features)
                        {
                            screen = Screen.Domains;
                            cursor = SafeIndex(root.Children, currentDomain);
                            currentFeature = null;
                            continue;
                        }
                        if (screen == Screen.Search)
                        {
                            screen = Screen.Domains;
                            searchTerm = string.Empty;
                            searchResults.Clear();
                            cursor = 0;
                            continue;
                        }
                        break;
                    }

                    if (key.KeyChar == '/' && screen != Screen.Search && screen != Screen.Detail)
                    {
                        screen = Screen.Search;
                        searchTerm = string.Empty;
                        searchResults = new List<NavigationNode>();
                        cursor = 0;
                        detailScroll = 0;
                        continue;
                    }

                    switch (screen)
                    {
                        case Screen.Domains:
                            HandleListNav(key, root.Children, ref cursor, out var openDomain);
                            if (openDomain && root.Children.Count > 0)
                            {
                                currentDomain = root.Children[cursor];
                                currentFeature = null;
                                cursor = 0;
                                screen = Screen.Features;
                            }
                            break;

                        case Screen.Features:
                            if (currentDomain is null) break;
                            HandleListNav(key, currentDomain.Children, ref cursor, out var openFeature);
                            if (openFeature && currentDomain.Children.Count > 0)
                            {
                                currentFeature = currentDomain.Children[cursor];
                                cursor = 0;
                                screen = Screen.Settings;
                            }
                            break;

                        case Screen.Settings:
                            if (currentFeature is null) break;
                            HandleListNav(key, currentFeature.Children, ref cursor, out var openSetting);
                            if (openSetting && currentFeature.Children.Count > 0)
                            {
                                detailScroll = 0;
                                screen = Screen.Detail;
                            }
                            break;

                        case Screen.Search:
                            HandleSearchInput(key, ref searchTerm, ref searchResults, ref cursor, catalog, query, root,
                                ref screen, ref currentDomain, ref currentFeature, ref detailScroll);
                            break;
                    }
                }
            }
            finally
            {
                Console.CursorVisible = true;
                LeaveAltScreen();
                Console.WriteLine("Exited explorer. Session was read-only.");
            }
        }

        private static void EnterAltScreen()
        {
            try
            {
                // Alternate screen buffer (xterm / Windows Terminal / modern consoles).
                Console.Write("\u001b[?1049h\u001b[H\u001b[2J");
                _altScreen = true;
            }
            catch
            {
                _altScreen = false;
                try { Console.Clear(); } catch { /* ignore */ }
            }
        }

        private static void LeaveAltScreen()
        {
            try
            {
                if (_altScreen)
                    Console.Write("\u001b[?1049l");
                else
                    Console.Clear();
            }
            catch
            {
                try { Console.Clear(); } catch { /* ignore */ }
            }
        }

        private static void BeginFrame()
        {
            try
            {
                // Home cursor and clear from cursor down — deterministic single-frame repaint.
                Console.Write("\u001b[H\u001b[J");
            }
            catch
            {
                try { Console.Clear(); } catch { /* ignore */ }
            }
        }

        private static void WriteHeader(Screen screen, NavigationNode? domain, NavigationNode? feature)
        {
            Console.WriteLine("Windows Privacy Platform  ·  v0.7  ·  Read-only knowledge explorer");
            var crumb = screen switch
            {
                Screen.Domains => "Domains",
                Screen.Features => domain?.Title ?? "Domain",
                Screen.Settings => $"{domain?.Title} › {feature?.Title}",
                Screen.Detail => $"{domain?.Title} › {feature?.Title} › detail",
                Screen.Search => "Search",
                _ => ""
            };
            Console.WriteLine(crumb);
            Console.WriteLine(new string('─', ContentWidth()));
            Console.WriteLine();
        }

        private static void WriteFooter(Screen screen)
        {
            Console.WriteLine();
            Console.WriteLine(new string('─', ContentWidth()));
            var help = screen switch
            {
                Screen.Detail => "↑↓ scroll card   ·   Esc return   ·   Q quit",
                Screen.Search => "Type to filter   ·   Enter open   ·   Esc cancel",
                _ => "↑↓ move   ·   Enter open   ·   / search   ·   Esc back   ·   Q quit"
            };
            Console.WriteLine(help);
        }

        private static void RenderNodeList(IReadOnlyList<NavigationNode> nodes, int cursor, string title, NavigationNode parent)
        {
            Console.WriteLine($"  {title}");
            var meta = new List<string>();
            if (parent.ChildCount > 0) meta.Add($"{parent.ChildCount} items");
            if (parent.ConflictCount > 0) meta.Add($"{parent.ConflictCount} layer conflicts");
            if (parent.HighRiskCount > 0) meta.Add($"{parent.HighRiskCount} high-impact");
            if (meta.Count > 0)
                Console.WriteLine("  " + string.Join("  ·  ", meta));
            Console.WriteLine();

            if (nodes.Count == 0)
            {
                Console.WriteLine("  (empty)");
                return;
            }

            var maxVisible = Math.Max(6, Math.Min(Console.WindowHeight - 9, 24));
            var start = Math.Max(0, Math.Min(cursor - maxVisible / 2, nodes.Count - maxVisible));
            if (start < 0) start = 0;
            var end = Math.Min(nodes.Count, start + maxVisible);

            for (var i = start; i < end; i++)
            {
                var n = nodes[i];
                var marker = i == cursor ? "›" : " ";
                var flags = new StringBuilder();
                if (n.HasConflict || n.ConflictCount > 0) flags.Append("  !conflict");
                if (n.RiskLevel == RiskLevel.High) flags.Append("  · high-impact");
                else if (n.RiskLevel == RiskLevel.Medium) flags.Append("  · medium-impact");
                var subtitle = string.IsNullOrWhiteSpace(n.Subtitle) ? "" : $"  ·  {Truncate(n.Subtitle, 24)}";
                var count = n.ChildCount > 0 ? $" ({n.ChildCount})" : "";
                Console.WriteLine($"  {marker} {n.Title}{count}{flags}{subtitle}");
            }

            if (nodes.Count > maxVisible)
                Console.WriteLine($"\n  Showing {start + 1}–{end} of {nodes.Count}");
        }

        private static void RenderDetail(SettingDetailView? card, ManagedObject mo, int scroll)
        {
            if (card is null)
            {
                Console.WriteLine("  (no detail available)");
                return;
            }

            // Build full card as lines, then window by scroll for long content.
            var lines = new List<string>();
            var width = ContentWidth();

            lines.Add(new string('═', width));
            lines.Add(card.Title);
            lines.Add(new string('═', width));
            lines.Add(string.Empty);

            // --- Overview ---
            AddSection(lines, "Overview");
            AddField(lines, "Domain", card.DomainPath);
            AddField(lines, "Impact", card.Explanation.ImpactLabel);
            AddField(lines, "Control", HumanControl(card.ControlLevel));
            lines.Add(string.Empty);
            AddWrapped(lines, card.Explanation.RiskSummary);
            lines.Add(string.Empty);

            // --- What / why (documentation) ---
            AddSection(lines, "What this is");
            AddWrapped(lines, card.Explanation.WhatIsIt);
            lines.Add(string.Empty);

            AddSection(lines, "Why Windows has this");
            AddWrapped(lines, card.Explanation.WhyItMatters);
            lines.Add(string.Empty);

            // --- Observed facts ---
            AddSection(lines, "Observed");
            AddField(lines, "Raw value", DisplayOrUnknown(card.CurrentStateDisplay));
            if (card.Layers.Count > 0)
            {
                foreach (var layer in card.Layers)
                {
                    var path = string.IsNullOrWhiteSpace(layer.SourcePathDisplay)
                        ? ""
                        : $"  ({Truncate(layer.SourcePathDisplay, 36)})";
                    lines.Add($"  · {HumanLayer(layer.LayerName)}: {Truncate(layer.ValueDisplay, 32)}{path}");
                }
            }
            else
            {
                lines.Add("  · No per-layer observations recorded for this setting.");
            }
            lines.Add(string.Empty);

            // --- Interpretation ---
            AddSection(lines, "Interpretation");
            AddField(lines, "Effective value", DisplayOrUnknown(card.EffectiveValueDisplay));
            AddField(lines, "Effective layer", DisplayOrUnknown(HumanLayer(card.EffectiveSourceDisplay)));
            AddField(lines, "Confidence", HumanConfidence(card.Confidence));
            if (!string.IsNullOrWhiteSpace(card.ResolutionReason))
                AddWrapped(lines, card.ResolutionReason);
            if (card.HasConflict)
                lines.Add("  Note: layer values disagree — see observed layers above.");
            lines.Add(string.Empty);

            // --- Relationships (human language) ---
            if (card.Related.Count > 0)
            {
                AddSection(lines, "Related configuration");
                foreach (var group in card.Related
                             .GroupBy(r => HumanRelationshipGroup(r.Relationship))
                             .OrderBy(g => g.Key))
                {
                    lines.Add($"  {group.Key}");
                    foreach (var rel in group.Take(6))
                    {
                        var note = string.IsNullOrWhiteSpace(rel.Explanation)
                            ? ""
                            : $" — {Truncate(rel.Explanation, 48)}";
                        lines.Add($"    · {rel.Title}{note}");
                    }
                }
                lines.Add(string.Empty);
            }

            // --- Impacts / knowledge ---
            if (!string.IsNullOrWhiteSpace(card.Explanation.UserImpact))
            {
                AddSection(lines, "What changes for the user");
                AddWrapped(lines, card.Explanation.UserImpact);
                lines.Add(string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(card.Explanation.EnterpriseImpact))
            {
                AddSection(lines, "Enterprise and management");
                AddWrapped(lines, card.Explanation.EnterpriseImpact);
                lines.Add(string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(card.Explanation.PrivacyImpactText))
            {
                AddSection(lines, "Privacy impact");
                AddWrapped(lines, card.Explanation.PrivacyImpactText);
                lines.Add(string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(card.Explanation.SecurityImpactText))
            {
                AddSection(lines, "Security impact");
                AddWrapped(lines, card.Explanation.SecurityImpactText);
                lines.Add(string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(card.Explanation.SideEffects))
            {
                AddSection(lines, "Side effects");
                AddWrapped(lines, card.Explanation.SideEffects);
                lines.Add(string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(card.Explanation.Exceptions))
            {
                AddSection(lines, "When another layer wins");
                AddWrapped(lines, card.Explanation.Exceptions);
                lines.Add(string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(card.Explanation.CommonMisconceptions))
            {
                AddSection(lines, "Common misconceptions");
                AddWrapped(lines, card.Explanation.CommonMisconceptions);
                lines.Add(string.Empty);
            }

            if (card.Explanation.RelatedApplications.Count > 0)
            {
                AddSection(lines, "Applications often involved");
                foreach (var app in card.Explanation.RelatedApplications)
                    lines.Add($"  · {app}");
                lines.Add(string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(card.Explanation.TypicalUseCases))
            {
                AddSection(lines, "Typical contexts");
                AddWrapped(lines, card.Explanation.TypicalUseCases);
                lines.Add(string.Empty);
            }

            // --- Unknowns ---
            if (!string.IsNullOrWhiteSpace(card.Explanation.Unknowns))
            {
                AddSection(lines, "Unknowns and limitations");
                AddWrapped(lines, card.Explanation.Unknowns);
                lines.Add(string.Empty);
            }

            // --- Provenance ---
            AddSection(lines, "Provenance");
            AddField(lines, "Object id", mo.ObjectId);
            AddField(lines, "Discovery", Truncate(mo.DiscoveryMethod, 52));
            AddField(lines, "Interface", mo.InterfaceName.ToString());
            if (mo.LastVerified is not null)
                AddField(lines, "Observed at", mo.LastVerified.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
            lines.Add("  Values above are read-only observations; nothing was modified.");
            lines.Add(string.Empty);

            if (!string.IsNullOrWhiteSpace(card.Explanation.DecisionGuidance))
            {
                AddSection(lines, "Context");
                AddWrapped(lines, card.Explanation.DecisionGuidance);
            }

            // Window the card
            var viewHeight = Math.Max(8, Console.WindowHeight - 8);
            var maxScroll = Math.Max(0, lines.Count - viewHeight);
            scroll = Math.Min(scroll, maxScroll);
            var end = Math.Min(lines.Count, scroll + viewHeight);
            for (var i = scroll; i < end; i++)
                Console.WriteLine(lines[i]);

            if (lines.Count > viewHeight)
                Console.WriteLine($"  … lines {scroll + 1}–{end} of {lines.Count} (↑↓ to scroll)");
        }

        private static void RenderSearch(string term, List<NavigationNode> results, int cursor)
        {
            Console.WriteLine("  Search the catalog");
            Console.WriteLine($"  Filter: {term}█");
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(term))
            {
                Console.WriteLine("  Type part of a name, domain, or description.");
                return;
            }

            if (results.Count == 0)
            {
                Console.WriteLine("  No matches.");
                return;
            }

            var maxVisible = Math.Max(6, Console.WindowHeight - 12);
            var start = Math.Max(0, Math.Min(cursor - maxVisible / 2, results.Count - maxVisible));
            if (start < 0) start = 0;
            var end = Math.Min(results.Count, start + maxVisible);

            for (var i = start; i < end; i++)
            {
                var n = results[i];
                var marker = i == cursor ? "›" : " ";
                var domain = n.Domain?.ToString() ?? "";
                Console.WriteLine($"  {marker} {n.Title}  ·  {domain}");
            }

            Console.WriteLine($"\n  {results.Count} match(es)");
        }

        private static void HandleListNav(ConsoleKeyInfo key, IReadOnlyList<NavigationNode> nodes, ref int cursor, out bool open)
        {
            open = false;
            if (nodes.Count == 0) return;

            if (key.Key == ConsoleKey.UpArrow)
                cursor = (cursor - 1 + nodes.Count) % nodes.Count;
            else if (key.Key == ConsoleKey.DownArrow)
                cursor = (cursor + 1) % nodes.Count;
            else if (key.Key == ConsoleKey.Home)
                cursor = 0;
            else if (key.Key == ConsoleKey.End)
                cursor = nodes.Count - 1;
            else if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.RightArrow)
                open = true;
        }

        private static void HandleSearchInput(
            ConsoleKeyInfo key,
            ref string searchTerm,
            ref List<NavigationNode> searchResults,
            ref int cursor,
            IReadOnlyList<ManagedObject> catalog,
            SettingsQuery query,
            NavigationNode root,
            ref Screen screen,
            ref NavigationNode? currentDomain,
            ref NavigationNode? currentFeature,
            ref int detailScroll)
        {
            if (key.Key == ConsoleKey.Enter)
            {
                if (searchResults.Count == 0 || cursor < 0 || cursor >= searchResults.Count)
                    return;

                var chosen = searchResults[cursor];
                if (string.IsNullOrWhiteSpace(chosen.ObjectId))
                    return;

                foreach (var domain in root.Children)
                {
                    foreach (var feature in domain.Children)
                    {
                        var idx = feature.Children.FindIndex(c =>
                            string.Equals(c.ObjectId, chosen.ObjectId, StringComparison.OrdinalIgnoreCase));
                        if (idx >= 0)
                        {
                            currentDomain = domain;
                            currentFeature = feature;
                            cursor = idx;
                            detailScroll = 0;
                            screen = Screen.Detail;
                            return;
                        }
                    }
                }

                currentDomain = null;
                currentFeature = new NavigationNode
                {
                    Title = "Search result",
                    Children = new List<NavigationNode> { chosen }
                };
                cursor = 0;
                detailScroll = 0;
                screen = Screen.Detail;
                return;
            }

            if (key.Key == ConsoleKey.UpArrow && searchResults.Count > 0)
            {
                cursor = (cursor - 1 + searchResults.Count) % searchResults.Count;
                return;
            }
            if (key.Key == ConsoleKey.DownArrow && searchResults.Count > 0)
            {
                cursor = (cursor + 1) % searchResults.Count;
                return;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (searchTerm.Length > 0)
                    searchTerm = searchTerm[..^1];
            }
            else if (!char.IsControl(key.KeyChar) && searchTerm.Length < 80)
            {
                searchTerm += key.KeyChar;
            }
            else
            {
                return;
            }

            cursor = 0;
            searchResults = BuildSearchNodes(searchTerm, catalog, query);
        }

        private static List<NavigationNode> BuildSearchNodes(string term, IReadOnlyList<ManagedObject> catalog, SettingsQuery query)
        {
            var matches = query.Search(term).Take(80).ToList();
            return matches.Select(m => new NavigationNode
            {
                Id = $"setting:{m.ObjectId}",
                Title = m.ObjectName,
                ObjectId = m.ObjectId,
                Domain = m.ProductDomain,
                RiskLevel = m.RiskLevel,
                HasConflict = m.Observation?.Effective?.HasConflict == true ||
                              m.Observation?.Resolution?.HasConflict == true,
                Subtitle = m.CurrentState
            }).ToList();
        }

        private static int SafeIndex(IReadOnlyList<NavigationNode>? list, NavigationNode? node)
        {
            if (list is null || node is null) return 0;
            for (var i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], node) ||
                    string.Equals(list[i].Id, node.Id, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 0;
        }

        private static int ContentWidth() => Math.Min(Math.Max(Console.WindowWidth - 1, 40), 78);

        private static void AddSection(List<string> lines, string title)
        {
            lines.Add($"  {title}");
            lines.Add($"  {new string('─', Math.Min(title.Length, 40))}");
        }

        private static void AddField(List<string> lines, string label, string? value)
        {
            lines.Add($"  {label,-14}  {value ?? "(none)"}");
        }

        private static void AddWrapped(List<string> lines, string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                lines.Add("  (none)");
                return;
            }

            var width = Math.Min(ContentWidth() - 2, 72);
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = "  ";
            foreach (var word in words)
            {
                if (line.Length + word.Length + 1 > width)
                {
                    lines.Add(line);
                    line = "  " + word;
                }
                else
                {
                    line = line.Length == 2 ? line + word : line + " " + word;
                }
            }
            if (line.Trim().Length > 0)
                lines.Add(line);
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            value = value.Replace('\r', ' ').Replace('\n', ' ');
            return value.Length <= max ? value : value[..(max - 1)] + "…";
        }

        private static string DisplayOrUnknown(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "Unknown" : value;

        private static string HumanControl(ControlLevel level) => level switch
        {
            ControlLevel.AdministratorControlled => "Administrator / policy",
            ControlLevel.UserControlled => "User preference (unless overridden)",
            ControlLevel.Locked => "Platform-locked",
            _ => level.ToString()
        };

        private static string HumanConfidence(EffectiveConfidence c) => c switch
        {
            EffectiveConfidence.High => "High",
            EffectiveConfidence.Medium => "Medium",
            EffectiveConfidence.Low => "Low",
            _ => "Unknown"
        };

        private static string HumanLayer(string? layer)
        {
            if (string.IsNullOrWhiteSpace(layer)) return "Unknown";
            return layer switch
            {
                "UserPreference" => "User preference",
                "ApplicationPreference" => "Application preference",
                "AlternatePolicyStore" => "Alternate policy store",
                "MachinePolicy" => "Machine policy",
                "MDMPolicy" => "MDM policy",
                "SecurityBaseline" => "Security baseline",
                "Unknown" => "Unknown",
                _ => layer
            };
        }

        private static string HumanRelationshipGroup(string kind) => kind switch
        {
            "Overrides" => "Can override",
            "OverriddenBy" => "Controlled by",
            "ConflictsWith" => "Potential conflicts",
            "DependsOn" or "Requires" => "Depends on",
            "Affects" => "Affects",
            "SameFeatureAlternatePath" => "Alternate path for the same feature",
            "Related" => "Also related to",
            _ => "Related to"
        };
    }
}
