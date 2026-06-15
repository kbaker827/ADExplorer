# AD Explorer

A modern Active Directory lookup tool for IT professionals. Search users, groups, and computers in your on-prem AD — all from a clean, system-themed Windows desktop app.

## Features

- **User Search** — Look up by name, username, or email. See account status, group memberships, password expiry, and last logon.
- **Group Search** — Find groups and browse their members.
- **Computer Search** — Look up machines by hostname with OS info and last logon.
- **System Theme** — Automatically follows Windows light/dark mode and accent color.
- **Copy to Clipboard** — One-click copy for email and username.

## Requirements

- Windows 10/11, domain-joined machine
- No installation required — just run the EXE

## Download

Download the latest `ADExplorer.exe` from [Releases](../../releases).

## Build from Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
git clone https://github.com/<your-username>/ADExplorer.git
cd ADExplorer
dotnet publish src/ADExplorer/ADExplorer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
# EXE is at publish/ADExplorer.exe
```

## Release a New Version

Tag a commit and push — GitHub Actions builds and publishes the EXE automatically:

```powershell
git tag v1.0.1
git push --tags
```

## How It Works

- Queries your on-premises Active Directory via LDAP (no extra configuration needed on domain-joined machines)
- Optional Microsoft Graph API fallback for hybrid Azure AD environments
- Theme follows your Windows personalization settings automatically
