# AD Explorer — Design Spec
**Date:** 2026-06-15  
**Status:** Approved

## Overview

A modern C# WPF desktop application for IT staff to instantly look up users, groups, and computers in Active Directory. Packaged as a single self-contained EXE, distributed via GitHub Releases with automated builds on tag push.

## Technology Stack

| Concern | Choice |
|---|---|
| Language | C# (.NET 8) |
| UI Framework | WPF (Windows Presentation Foundation) |
| AD Connectivity | System.DirectoryServices (LDAP) primary, Microsoft Graph API fallback |
| Theme | Windows system theme (light/dark + accent color, auto-detected) |
| Packaging | Single self-contained EXE (win-x64, PublishSingleFile) |
| CI/CD | GitHub Actions, triggers on `v*` tag push |

## Architecture

```
ADExplorer.exe
├── UI Layer (WPF / XAML)
│   ├── MainWindow.xaml              ← tab host, search bar, status bar
│   ├── Views/UserSearchView.xaml    ← results list + detail panel
│   ├── Views/GroupSearchView.xaml   ← group results + member list
│   └── Views/ComputerView.xaml      ← computer results + detail panel
├── Services
│   ├── AdService.cs                 ← DirectoryServices/LDAP queries
│   ├── GraphService.cs              ← optional Microsoft Graph fallback
│   └── ThemeService.cs              ← detects Windows light/dark mode
└── Models
    ├── AdUser.cs
    ├── AdGroup.cs
    └── AdComputer.cs
```

**Data flow:** Search input → `AdService` queries LDAP → results populate list view → user clicks result → detail panel populates. Graph API is only invoked as a fallback if LDAP returns no results (supports hybrid Azure AD environments).

## UI Layout

### Main Window
- Title bar: "AD Explorer" with minimize/maximize/close
- Tab bar: **Users | Groups | Computers**
- Search bar: single text input, searches by name, username, or email
- Split panel: results list (left) + detail panel (right)
- Status bar: domain name + result count

### User Detail Panel
- **Identity:** Display name, username (sAMAccountName), email, title, department, manager, phone
- **Account Status:** Enabled/disabled, locked out (yes/no), password expiry (days remaining), last logon timestamp, bad password count
- **Group Memberships:** Scrollable list of all groups the user belongs to
- **Org info:** OU path, account creation date, description
- **Actions:** Copy Email button, Copy Username button

### Groups Tab
- Search groups by name
- Detail panel: group description, group type (security/distribution), member count, full member list
- Clicking a member navigates to their user detail

### Computers Tab
- Search computers by hostname
- Detail panel: OS name + version, last logon, enabled status, OU path, description

## Theme

Follows Windows system settings automatically:
- Light/dark mode detected via `SystemParameters` and registry
- Accent color pulled from Windows personalization settings
- No manual theme toggle needed — updates if the user changes Windows theme while the app is open

## Packaging

```
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Output: single `ADExplorer.exe` (~50-80MB), no installer, no runtime dependency.

## GitHub Actions Release Workflow

**Trigger:** Push a tag matching `v*` (e.g., `v1.0.0`)

**Steps:**
1. Checkout on `windows-latest` runner
2. Setup .NET 8
3. `dotnet publish` with self-contained single-file flags
4. Create GitHub Release with tag name as title
5. Upload `ADExplorer.exe` as release asset

**Releasing a new version:**
```
git tag v1.0.0
git push --tags
```
GitHub Actions handles the rest — EXE attached to the release automatically.

## Repository Structure

```
ADExplorer/
├── .github/
│   └── workflows/
│       └── release.yml
├── src/
│   └── ADExplorer/
│       ├── ADExplorer.csproj
│       ├── App.xaml
│       ├── MainWindow.xaml
│       ├── Views/
│       │   ├── UserSearchView.xaml
│       │   ├── GroupSearchView.xaml
│       │   └── ComputerView.xaml
│       ├── Services/
│       │   ├── AdService.cs
│       │   ├── GraphService.cs
│       │   └── ThemeService.cs
│       └── Models/
│           ├── AdUser.cs
│           ├── AdGroup.cs
│           └── AdComputer.cs
├── README.md
└── ADExplorer.sln
```

## Error Handling

- If LDAP connection fails: show inline error in status bar, offer Graph fallback if configured
- If search returns no results: empty state message in results panel
- If a user has no last logon (never logged in): display "Never"
- If Graph credentials not configured: Graph tab silently disabled, LDAP-only mode

## Out of Scope (v1)

- Editing AD attributes
- Password resets
- Creating/deleting AD objects
- Remote computer management
