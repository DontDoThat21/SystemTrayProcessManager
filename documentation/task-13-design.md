# Task 13: Error Handling & Stability - Design Document

## Document Overview
**Task**: Task 13: Error Handling & Stability (Phase 5.2)  
**Priority**: Critical  
**Estimated Time**: 3-4 hours  
**Dependencies**: All previous tasks  
**Created**: 2026-01-29  
**Author**: WPF Engineer Agent

---

## 1. Task Overview

### 1.1 Objective
Implement a centralized error handling and stability system that:
- Provides a unified error handling service with user-friendly error messages
- Detects administrator elevation and wraps UAC-aware operations
- Generates crash reports with diagnostic information on unhandled exceptions
- Provides automated recovery for corrupted settings
- Handles edge cases: terminated processes, invalid handles, missing audio devices

### 1.2 Scope
**In Scope**:
- `ErrorInfo` model for structured error data
- `DiagnosticInfo` model for system diagnostic snapshots
- `CrashReport` model combining error + diagnostics
- `IErrorHandlingService` for centralized error management
- `ICrashReporterService` for crash report generation and persistence
- `IElevationService` for admin detection and UAC-aware operations
- Service implementations in Infrastructure
- Enhanced App.xaml.cs exception handlers using crash reporter
- Unit tests for all new components
- DI registration

**Out of Scope**:
- Recovery dialog UI (basic MessageBox used instead)
- Automated restart mechanism
- Remote crash report submission

### 1.3 Success Criteria
- [x] Centralized error handling with severity levels and throttling
- [x] Crash reports persisted to AppData on unhandled exceptions
- [x] Admin elevation detection using WindowsIdentity
- [x] Diagnostic info collection (OS, .NET, memory, uptime)
- [x] All new code has comprehensive try-catch and logging
- [x] All unit tests pass (100%)
- [x] No compiler warnings for task files
- [x] Integrated with existing App.xaml.cs exception handlers

---

## 2. Architecture

### 2.1 New Components

```
Core/Models/
+-- ErrorInfo.cs                    Structured error data with severity
+-- DiagnosticInfo.cs               System diagnostic snapshot
+-- CrashReport.cs                  Combines ErrorInfo + DiagnosticInfo

Core/Enums/
+-- ErrorSeverity.cs                None, Low, Medium, High, Critical

Core/Services/
+-- IErrorHandlingService.cs        Centralized error handling
+-- ICrashReporterService.cs        Crash report generation + persistence
+-- IElevationService.cs            Admin detection + UAC wrapper

Infrastructure/Services/
+-- ErrorHandlingService.cs         Implementation with throttling
+-- CrashReporterService.cs         JSON crash reports to AppData
+-- ElevationService.cs             WindowsIdentity-based detection
```

### 2.2 Data Flow

```
Exception occurs --> ErrorHandlingService.HandleError()
                         |
                         +-- Logs error via ILogger
                         +-- Tracks error history (throttling)
                         +-- Raises ErrorOccurred event
                         +-- If Critical: triggers ICrashReporterService

CrashReporterService.GenerateReportAsync()
    +-- Captures ErrorInfo from exception
    +-- Collects DiagnosticInfo (OS, memory, .NET)
    +-- Writes JSON to %LOCALAPPDATA%/.../crash-reports/
    +-- Returns CrashReport for caller
```

---

## 3. Model Design

### 3.1 ErrorSeverity Enum
```
None = 0, Low = 1, Medium = 2, High = 3, Critical = 4
```

### 3.2 ErrorInfo
- ExceptionType, Message, StackTrace, InnerExceptionMessage
- Source (service name), Severity, Timestamp
- AdditionalContext dictionary

### 3.3 DiagnosticInfo
- OSVersion, DotNetVersion, MachineName, UserName
- ProcessMemoryMB, AvailableMemoryMB, ProcessorCount
- ApplicationVersion, Uptime
- IsAdministrator flag

### 3.4 CrashReport
- Id (GUID), Timestamp, ErrorInfo, DiagnosticInfo
- ApplicationState (running services summary)
- Serializable to JSON

---

## 4. Service Design

### 4.1 IErrorHandlingService
- `HandleError(Exception, string source, ErrorSeverity)` - Main entry point
- `HandleError(string message, string source, ErrorSeverity)` - Message-only variant
- `GetUserFriendlyMessage(Exception)` - Maps exceptions to user-facing text
- `RecentErrors` - Read-only collection of recent errors
- `ErrorOccurred` event - For UI notification
- `ClearErrors()` - Resets error history

### 4.2 ICrashReporterService
- `GenerateReportAsync(Exception)` - Creates and persists a crash report
- `GetRecentReportsAsync()` - Lists recent crash reports
- `GetReportAsync(string id)` - Loads a specific report
- `CleanupOldReportsAsync(int daysToKeep)` - Purges old reports
- `CrashReportDirectory` - Path to crash reports folder

### 4.3 IElevationService
- `IsRunningAsAdministrator` - Cached bool property
- `IsElevationRequired(int processId)` - Checks if process requires admin
- `RequestElevation()` - Relaunches app as admin (returns bool)

---

## 5. Integration Points

| Component | Integration |
|---|---|
| App.xaml.cs | Register services in DI; use crash reporter in exception handlers |
| OnDispatcherUnhandledException | Generate crash report, show enhanced dialog |
| OnUnhandledException | Generate crash report |
| OnUnobservedTaskException | Log via error handling service |
| Existing services | Can optionally inject IErrorHandlingService |

---

## 6. Test Strategy

| Test File | Tests | Coverage |
|---|---|---|
| ErrorInfoTests.cs | Model construction, FromException, properties | ~20 |
| DiagnosticInfoTests.cs | Model construction, Capture, properties | ~15 |
| CrashReportTests.cs | Model construction, CreateFromException, JSON | ~15 |
| ErrorSeverityTests.cs | Enum values | ~6 |
| ErrorHandlingServiceTests.cs | HandleError, friendly messages, throttling | ~25 |
| CrashReporterServiceTests.cs | Generate, list, load, cleanup reports | ~20 |
| ElevationServiceTests.cs | Admin detection, elevation required | ~10 |
| **Total** | | **~111** |

---

**Design Status**: Complete  
**Date**: 2026-01-29
