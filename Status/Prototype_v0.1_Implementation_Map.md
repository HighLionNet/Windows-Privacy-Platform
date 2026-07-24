# Windows Privacy Platform
## Prototype v0.2 Implementation Map (Updated)

**Purpose**

This document records the exact implementation state after Prototype v0.2 closure and the introduction of the first real collector.

It is authoritative for the current codebase.

---

# Current Prototype Status

Prototype v0.2 is complete.

Architecture, build and runtime have all been verified.

The first real Windows discovery component (WindowsIdentityCollector) has been implemented and is strictly read-only.

No remediation exists. No elevation exists. No Windows modification occurs.

---

# Overall Runtime Pipeline

CLI  
↓ Create AuditLogger  
↓ Create InMemoryKnowledgeBaseRepository  
↓ Create Inventory Collector List  
↓ Create InventoryScanner  
↓ Create SchemaValidator  
↓ Scanner.Scan()  
↓ InventoryScanner iterates collectors  
↓ Collectors populate InventorySnapshot  
↓ InventorySnapshot returned inside ScanResult  
↓ CLI creates one test ManagedObject  
↓ CLI creates KnowledgeBaseEntry  
↓ KnowledgeBase stores entry  
↓ SchemaValidator validates entry  
↓ ValidationResult returned  
↓ CLI prints results  
↓ CLI prints explicit safety confirmation

---

# Scanner Architecture

InventoryScanner is responsible only for orchestration, collector execution order, logging and exception handling.

It no longer contains collection logic.

Future collectors can be added without modifying InventoryScanner itself.

---

# Collector Interface

IInventoryCollector  
• Name property  
• Collect(InventorySnapshot)

Collectors receive the snapshot and populate only their own section. They never report, never validate, never write to Windows.

---

# Current Collectors

WindowsIdentityCollector — **real, read-only**  
  Primary: Registry.LocalMachine\SOFTWARE\Microsoft\Windows NT\CurrentVersion (non-elevated)  
  Fallback: Environment.OSVersion  
  Populates: WindowsVersion, Edition, BuildNumber, CaptureTimestamp

CapabilityCollector — placeholder  
PackageCollector — placeholder  
ServiceCollector — placeholder  
ScheduledTaskCollector — placeholder  
PrivacyCollector — placeholder

---

# Dependency Injection

InventoryScanner constructor receives IAuditLogger and IEnumerable<IInventoryCollector>.  
Collectors are created by CLI.  
No DI container. Construction remains explicit.

---

# Logging Architecture

AuditEventType, IAuditLogger, AuditLogger.  
Console only, thread-safe, UTC timestamps.  
No file, network or third-party dependencies.

---

# Validator Architecture

SchemaValidator receives IAuditLogger.  
Structural validation only via RequiredFieldRule (ObjectId, ObjectName).  
No policy or compliance engine.

---

# Knowledge Base

InMemoryKnowledgeBaseRepository.  
Stores KnowledgeBaseEntry objects.  
No persistence, no serialization, no database.

---

# Models / Core

Unchanged in structure.  
Models contain pure data.  
Core contains OperationResult, PlatformException, PathConstants.  
No ElevationHelper exists.

---

# CLI

Creates logger, repository, collector list, scanner, validator.  
Runs scan → stores one ManagedObject → validates → prints results + safety confirmation.  
No command parsing yet.

---

# Current Dependency Graph

Models ← Core ← Logging ← KnowledgeBase ← Validator ← Scanner ← CLI

---

# Current Read-Only Guarantees

InventoryScanner cannot modify Windows.  
Collectors contain no write paths (WindowsIdentityCollector is read-only).  
Logging, Validator, KnowledgeBase and CLI cannot modify Windows.  
No remediation code exists anywhere.

---

# Files Added / Modified for Real Identity Collector

Modified:  
Source/WindowsPrivacyPlatform.Scanner/WindowsIdentityCollector.cs

No new projects or files required.

---

# Build Expectations

```
dotnet build -c Release
```
Expected: success, zero errors, zero warnings preferred.

---

# Runtime Expectations

CLI starts. Logger initializes. Scanner executes.  
WindowsIdentityCollector returns real version/edition/build (or safe fallback).  
Remaining collectors remain placeholders.  
KnowledgeBase stores object. Validator validates.  
Safety confirmation is printed.  
No Windows changes, no elevation, no UAC.

---

# Repository Layout

Archive/  
KnowledgeBase/  
Source/  
Status/

Development continues only against the active Source directory.

---

# Next Steps

1. Confirm real WindowsIdentityCollector output on a local machine.
2. Implement next real collector (CapabilityCollector recommended).
3. Continue one collector at a time while preserving build and runtime success after every change.
