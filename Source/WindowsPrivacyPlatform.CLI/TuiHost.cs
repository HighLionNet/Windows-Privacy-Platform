// Source/WindowsPrivacyPlatform.CLI/TuiHost.cs
using System;
using System.Collections.Generic;
using System.Linq;
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

            Console.CursorVisible = false;
            try
            {
                while (true)
                {
                    Console.Clear();
                    WriteHeader();

                    switch (screen)
                    {
                        case Screen.Domains:
                            RenderNodeList(root.Children, cursor, "Domains", root);
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
                                    RenderDetail(NavigationBuilder.BuildDetail(mo, query));
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

                    if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Backspace)
                    {
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
                                screen = Screen.Detail;
                            break;

                        case Screen.Detail:
                            // only Back / Esc handled above; arrow keys ignored on detail
                            break;

                        case Screen.Search:
                            HandleSearchInput(key, ref searchTerm, ref searchResults, ref cursor, catalog, query, root,
                                ref screen, ref currentDomain, ref currentFeature);
                            break;
                    }
                }
            }
            finally
            {
                Console.CursorVisible = true;
                Console.Clear();
                Console.WriteLine("Exited TUI. Session was read-only.");
            }
        }

        private static void WriteHeader()
        {
            Console.WriteLine("Windows Privacy Platform  ·  v0.7  ·  Read-only explorer");
            Console.WriteLine(new string('─', Math.Min(Console.WindowWidth - 1, 72)));
            Console.WriteLine();
        }

        private static void WriteFooter(Screen screen)
        {
            Console.WriteLine();
            Console.WriteLine(new string('─', Math.Min(Console.WindowWidth - 1, 72)));
            var help = screen switch
            {
                Screen.Detail => "Esc/Back  return   ·   Q  quit",
                Screen.Search => "Type to filter  ·  Enter open  ·  Esc cancel",
                _ => "↑↓ navigate  ·  Enter open  ·  / search  ·  Esc/Back  ·  Q quit"
            };
            Console.WriteLine(help);
        }

        private static void RenderNodeList(IReadOnlyList<NavigationNode> nodes, int cursor, string title, NavigationNode parent)
        {
            Console.WriteLine($"  {title}");
            if (parent.ConflictCount > 0 || parent.HighRiskCount > 0)
            {
                Console.WriteLine($"  conflicts: {parent.ConflictCount}   high-risk: {parent.HighRiskCount}   items: {parent.ChildCount}");
            }
            Console.WriteLine();

            if (nodes.Count == 0)
            {
                Console.WriteLine("  (empty)");
                return;
            }

            var maxVisible = Math.Max(8, Console.WindowHeight - 10);
            var start = Math.Max(0, Math.Min(cursor - maxVisible / 2, nodes.Count - maxVisible));
            if (start < 0) start = 0;
            var end = Math.Min(nodes.Count, start + maxVisible);

            for (var i = start; i < end; i++)
            {
                var n = nodes[i];
                var marker = i == cursor ? ">" : " ";
                var conflict = n.HasConflict || n.ConflictCount > 0 ? " !" : "";
                var risk = n.RiskLevel == RiskLevel.High ? " [H]" : n.RiskLevel == RiskLevel.Medium ? " [M]" : "";
                var subtitle = string.IsNullOrWhiteSpace(n.Subtitle) ? "" : $"  ·  {Truncate(n.Subtitle, 28)}";
                var count = n.ChildCount > 0 ? $" ({n.ChildCount})" : "";
                Console.WriteLine($"  {marker} {n.Title}{count}{risk}{conflict}{subtitle}");
            }

            if (nodes.Count > maxVisible)
                Console.WriteLine($"  … {nodes.Count} total");
        }

        private static void RenderDetail(SettingDetailView? card)
        {
            if (card is null)
            {
                Console.WriteLine("  (no detail available)");
                return;
            }

            var width = Math.Min(Console.WindowWidth - 1, 72);
            Console.WriteLine(new string('=', width));
            Console.WriteLine(card.Title);
            Console.WriteLine(new string('=', width));
            Console.WriteLine();

            WriteSection("Overview");
            WriteField("Domain", card.DomainPath);
            WriteField("Risk", $"{card.RiskLevel} — {card.Explanation.RiskSummary}");
            WriteField("Control", card.ControlLevel.ToString());
            Console.WriteLine();

            WriteSection("What it is");
            WriteWrapped(card.Explanation.WhatIsIt);
            Console.WriteLine();

            WriteSection("Why it matters");
            WriteWrapped(card.Explanation.WhyItMatters);
            Console.WriteLine();

            WriteSection("Current observation");
            WriteField("Raw value", card.CurrentStateDisplay ?? "(none)");
            WriteField("Effective", card.EffectiveValueDisplay ?? "(unknown)");
            WriteField("Controller", card.EffectiveSourceDisplay ?? "(unknown)");
            WriteField("Confidence", card.Confidence.ToString());
            if (!string.IsNullOrWhiteSpace(card.ResolutionReason))
                WriteField("Why", card.ResolutionReason);
            if (card.HasConflict)
                Console.WriteLine("  ** Layer conflict present — review layers below.");
            Console.WriteLine();

            if (card.Layers.Count > 0)
            {
                WriteSection("Observed layers");
                foreach (var layer in card.Layers)
                    Console.WriteLine($"  · {layer.LayerName}: {Truncate(layer.ValueDisplay, 40)}");
                Console.WriteLine();
            }

            if (card.Related.Count > 0)
            {
                WriteSection("Relationships");
                foreach (var rel in card.Related.Take(12))
                {
                    var note = string.IsNullOrWhiteSpace(rel.Explanation) ? "" : $" — {Truncate(rel.Explanation, 42)}";
                    Console.WriteLine($"  · [{rel.Relationship}] {rel.Title}{note}");
                }
                Console.WriteLine();
            }

            if (!string.IsNullOrWhiteSpace(card.Explanation.UserImpact))
            {
                WriteSection("User impact");
                WriteWrapped(card.Explanation.UserImpact);
                Console.WriteLine();
            }

            if (!string.IsNullOrWhiteSpace(card.Explanation.EnterpriseImpact))
            {
                WriteSection("Enterprise / management");
                WriteWrapped(card.Explanation.EnterpriseImpact);
                Console.WriteLine();
            }

            if (!string.IsNullOrWhiteSpace(card.Explanation.TypicalUseCases))
            {
                WriteSection("Typical contexts");
                WriteWrapped(card.Explanation.TypicalUseCases);
                Console.WriteLine();
            }

            if (card.Explanation.RelatedApplications.Count > 0)
            {
                WriteSection("Often related applications");
                foreach (var app in card.Explanation.RelatedApplications)
                    Console.WriteLine($"  · {app}");
                Console.WriteLine();
            }

            if (!string.IsNullOrWhiteSpace(card.Explanation.DecisionGuidance))
            {
                WriteSection("Guidance (informational)");
                WriteWrapped(card.Explanation.DecisionGuidance);
                Console.WriteLine();
            }

            // Extra professional fields when present
            if (!string.IsNullOrWhiteSpace(card.Explanation.PrivacyImpactText))
            {
                WriteSection("Privacy impact");
                WriteWrapped(card.Explanation.PrivacyImpactText);
                Console.WriteLine();
            }
            if (!string.IsNullOrWhiteSpace(card.Explanation.SecurityImpactText))
            {
                WriteSection("Security impact");
                WriteWrapped(card.Explanation.SecurityImpactText);
                Console.WriteLine();
            }
            if (!string.IsNullOrWhiteSpace(card.Explanation.SideEffects))
            {
                WriteSection("Side effects");
                WriteWrapped(card.Explanation.SideEffects);
                Console.WriteLine();
            }
            if (!string.IsNullOrWhiteSpace(card.Explanation.Exceptions))
            {
                WriteSection("Exceptions");
                WriteWrapped(card.Explanation.Exceptions);
                Console.WriteLine();
            }
            if (!string.IsNullOrWhiteSpace(card.Explanation.CommonMisconceptions))
            {
                WriteSection("Common misconceptions");
                WriteWrapped(card.Explanation.CommonMisconceptions);
                Console.WriteLine();
            }
            if (!string.IsNullOrWhiteSpace(card.Explanation.Unknowns))
            {
                WriteSection("Unknowns / limitations");
                WriteWrapped(card.Explanation.Unknowns);
                Console.WriteLine();
            }
        }

        private static void RenderSearch(string term, List<NavigationNode> results, int cursor)
        {
            Console.WriteLine("  Search settings");
            Console.WriteLine($"  Filter: {term}_");
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(term))
            {
                Console.WriteLine("  Type to filter by name, id, domain, or description.");
                return;
            }

            if (results.Count == 0)
            {
                Console.WriteLine("  No matches.");
                return;
            }

            var maxVisible = Math.Max(8, Console.WindowHeight - 12);
            var start = Math.Max(0, Math.Min(cursor - maxVisible / 2, results.Count - maxVisible));
            if (start < 0) start = 0;
            var end = Math.Min(results.Count, start + maxVisible);

            for (var i = start; i < end; i++)
            {
                var n = results[i];
                var marker = i == cursor ? ">" : " ";
                var domain = n.Domain?.ToString() ?? "";
                Console.WriteLine($"  {marker} [{domain}] {n.Title}");
            }

            Console.WriteLine($"  ({results.Count} match(es))");
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
            ref NavigationNode? currentFeature)
        {
            if (key.Key == ConsoleKey.Enter)
            {
                if (searchResults.Count == 0 || cursor < 0 || cursor >= searchResults.Count)
                    return;

                var chosen = searchResults[cursor];
                if (string.IsNullOrWhiteSpace(chosen.ObjectId))
                    return;

                // Locate domain + feature for the chosen setting so Back works naturally
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
                            screen = Screen.Detail;
                            return;
                        }
                    }
                }

                // Fallback: open detail via query only (Back returns to domains)
                currentDomain = null;
                currentFeature = new NavigationNode
                {
                    Title = "Search result",
                    Children = new List<NavigationNode> { chosen }
                };
                cursor = 0;
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
            var idx = -1;
            for (var i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], node) ||
                    string.Equals(list[i].Id, node.Id, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }
            return idx >= 0 ? idx : 0;
        }

        private static void WriteSection(string title)
        {
            Console.WriteLine($"  {title}");
            Console.WriteLine($"  {new string('─', Math.Min(title.Length + 2, 40))}");
        }

        private static void WriteField(string label, string? value)
        {
            Console.WriteLine($"  {label,-12}: {value ?? "(none)"}");
        }

        private static void WriteWrapped(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine("  (none)");
                return;
            }

            var width = Math.Min(Console.WindowWidth - 4, 68);
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = "  ";
            foreach (var word in words)
            {
                if (line.Length + word.Length + 1 > width)
                {
                    Console.WriteLine(line);
                    line = "  " + word;
                }
                else
                {
                    line = line.Length == 2 ? line + word : line + " " + word;
                }
            }
            if (line.Trim().Length > 0)
                Console.WriteLine(line);
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            value = value.Replace('\r', ' ').Replace('\n', ' ');
            return value.Length <= max ? value : value[..(max - 1)] + "…";
        }
    }
}
