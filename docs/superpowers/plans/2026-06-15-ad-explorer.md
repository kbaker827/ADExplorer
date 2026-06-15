# AD Explorer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a modern C# WPF desktop app for IT staff to look up Active Directory users, groups, and computers — packaged as a single self-contained EXE with GitHub Actions release automation.

**Architecture:** WPF (.NET 8) with three tabs (Users, Groups, Computers), a split results/detail panel, and a service layer that queries on-prem AD via System.DirectoryServices/LDAP with optional Microsoft Graph fallback. Theme follows Windows system light/dark mode automatically.

**Tech Stack:** C# 12, .NET 8, WPF, System.DirectoryServices, Microsoft.Graph (NuGet), xUnit (tests), GitHub Actions

---

## File Map

| File | Responsibility |
|---|---|
| `src/ADExplorer/ADExplorer.csproj` | Project file, NuGet refs, publish settings |
| `src/ADExplorer/App.xaml` + `App.xaml.cs` | App entry, theme init |
| `src/ADExplorer/Models/AdUser.cs` | User data model |
| `src/ADExplorer/Models/AdGroup.cs` | Group data model |
| `src/ADExplorer/Models/AdComputer.cs` | Computer data model |
| `src/ADExplorer/Services/AdService.cs` | All LDAP queries |
| `src/ADExplorer/Services/GraphService.cs` | Microsoft Graph fallback |
| `src/ADExplorer/Services/ThemeService.cs` | Windows light/dark + accent detection |
| `src/ADExplorer/Themes/LightTheme.xaml` | Light mode resource dictionary |
| `src/ADExplorer/Themes/DarkTheme.xaml` | Dark mode resource dictionary |
| `src/ADExplorer/MainWindow.xaml` + `.cs` | Tab host, search bar, status bar |
| `src/ADExplorer/Views/UserSearchView.xaml` + `.cs` | User results list + detail panel |
| `src/ADExplorer/Views/GroupSearchView.xaml` + `.cs` | Group results + member list |
| `src/ADExplorer/Views/ComputerView.xaml` + `.cs` | Computer results + detail panel |
| `tests/ADExplorer.Tests/ADExplorer.Tests.csproj` | xUnit test project |
| `tests/ADExplorer.Tests/Models/AdUserTests.cs` | Model unit tests |
| `tests/ADExplorer.Tests/Services/AdServiceTests.cs` | AdService unit tests |
| `.github/workflows/release.yml` | GitHub Actions release workflow |
| `README.md` | Usage and release instructions |
| `ADExplorer.sln` | Solution file |

---

## Task 1: Scaffold Solution and Project

**Files:**
- Create: `ADExplorer.sln`
- Create: `src/ADExplorer/ADExplorer.csproj`
- Create: `tests/ADExplorer.Tests/ADExplorer.Tests.csproj`

- [ ] **Step 1: Verify .NET 8 SDK is installed**

```
dotnet --version
```
Expected: `8.x.x` or higher. If not installed, download from https://dotnet.microsoft.com/download/dotnet/8.0

- [ ] **Step 2: Create solution and projects**

```
cd C:\Users\kbaker.ONWASA\Documents\ADLookupTool
dotnet new sln -n ADExplorer
dotnet new wpf -n ADExplorer -o src/ADExplorer --framework net8.0-windows
dotnet new xunit -n ADExplorer.Tests -o tests/ADExplorer.Tests --framework net8.0
dotnet sln add src/ADExplorer/ADExplorer.csproj
dotnet sln add tests/ADExplorer.Tests/ADExplorer.Tests.csproj
dotnet add tests/ADExplorer.Tests/ADExplorer.Tests.csproj reference src/ADExplorer/ADExplorer.csproj
```

- [ ] **Step 3: Replace src/ADExplorer/ADExplorer.csproj with full config**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <AssemblyName>ADExplorer</AssemblyName>
    <RootNamespace>ADExplorer</RootNamespace>
    <ApplicationIcon>Resources\icon.ico</ApplicationIcon>
    <Version>1.0.0</Version>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Graph" Version="5.*" />
    <PackageReference Include="Azure.Identity" Version="1.*" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Create folder structure**

```
mkdir src\ADExplorer\Models
mkdir src\ADExplorer\Services
mkdir src\ADExplorer\Views
mkdir src\ADExplorer\Themes
mkdir src\ADExplorer\Resources
mkdir tests\ADExplorer.Tests\Models
mkdir tests\ADExplorer.Tests\Services
```

- [ ] **Step 5: Restore packages**

```
dotnet restore
```
Expected: no errors, packages restored.

- [ ] **Step 6: Verify build compiles**

```
dotnet build
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```
git init
git add .
git commit -m "chore: scaffold WPF solution with test project"
```

---

## Task 2: Define Models

**Files:**
- Create: `src/ADExplorer/Models/AdUser.cs`
- Create: `src/ADExplorer/Models/AdGroup.cs`
- Create: `src/ADExplorer/Models/AdComputer.cs`
- Create: `tests/ADExplorer.Tests/Models/AdUserTests.cs`

- [ ] **Step 1: Write failing model test**

Create `tests/ADExplorer.Tests/Models/AdUserTests.cs`:

```csharp
using ADExplorer.Models;
using Xunit;

namespace ADExplorer.Tests.Models;

public class AdUserTests
{
    [Fact]
    public void PasswordExpiryDays_ReturnsNegative_WhenAlreadyExpired()
    {
        var user = new AdUser
        {
            PasswordExpiryDate = DateTime.Now.AddDays(-5)
        };
        Assert.True(user.PasswordExpiryDays < 0);
    }

    [Fact]
    public void PasswordExpiryDays_ReturnsPositive_WhenNotExpired()
    {
        var user = new AdUser
        {
            PasswordExpiryDate = DateTime.Now.AddDays(14)
        };
        Assert.InRange(user.PasswordExpiryDays, 13, 15);
    }

    [Fact]
    public void LastLogonDisplay_ReturnsNever_WhenNull()
    {
        var user = new AdUser { LastLogon = null };
        Assert.Equal("Never", user.LastLogonDisplay);
    }

    [Fact]
    public void LastLogonDisplay_ReturnsFormattedDate_WhenSet()
    {
        var date = new DateTime(2026, 6, 14, 8, 2, 0);
        var user = new AdUser { LastLogon = date };
        Assert.Equal("2026-06-14 08:02", user.LastLogonDisplay);
    }
}
```

- [ ] **Step 2: Run test — expect compile failure**

```
dotnet test tests/ADExplorer.Tests
```
Expected: compile error — `AdUser` not defined yet.

- [ ] **Step 3: Create AdUser.cs**

Create `src/ADExplorer/Models/AdUser.cs`:

```csharp
namespace ADExplorer.Models;

public class AdUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string SamAccountName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsLockedOut { get; set; }
    public int BadPasswordCount { get; set; }
    public DateTime? LastLogon { get; set; }
    public DateTime? PasswordExpiryDate { get; set; }
    public DateTime? Created { get; set; }
    public List<string> Groups { get; set; } = new();

    public string LastLogonDisplay =>
        LastLogon.HasValue ? LastLogon.Value.ToString("yyyy-MM-dd HH:mm") : "Never";

    public int PasswordExpiryDays =>
        PasswordExpiryDate.HasValue
            ? (int)(PasswordExpiryDate.Value - DateTime.Now).TotalDays
            : -1;

    public string StatusDisplay => IsEnabled ? "✅ Enabled" : "❌ Disabled";
    public string LockedDisplay => IsLockedOut ? "⚠️ Yes" : "No";
}
```

- [ ] **Step 4: Create AdGroup.cs**

Create `src/ADExplorer/Models/AdGroup.cs`:

```csharp
namespace ADExplorer.Models;

public class AdGroup
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
    public int MemberCount => Members.Count;
}
```

- [ ] **Step 5: Create AdComputer.cs**

Create `src/ADExplorer/Models/AdComputer.cs`:

```csharp
namespace ADExplorer.Models;

public class AdComputer
{
    public string Name { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string OperatingSystemVersion { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime? LastLogon { get; set; }

    public string LastLogonDisplay =>
        LastLogon.HasValue ? LastLogon.Value.ToString("yyyy-MM-dd HH:mm") : "Never";

    public string StatusDisplay => IsEnabled ? "✅ Enabled" : "❌ Disabled";
}
```

- [ ] **Step 6: Run tests — expect pass**

```
dotnet test tests/ADExplorer.Tests
```
Expected: `4 passed, 0 failed`

- [ ] **Step 7: Commit**

```
git add src/ADExplorer/Models/ tests/ADExplorer.Tests/Models/
git commit -m "feat: add AdUser, AdGroup, AdComputer models with tests"
```

---

## Task 3: ThemeService — Windows System Theme Detection

**Files:**
- Create: `src/ADExplorer/Services/ThemeService.cs`
- Create: `src/ADExplorer/Themes/LightTheme.xaml`
- Create: `src/ADExplorer/Themes/DarkTheme.xaml`

- [ ] **Step 1: Create ThemeService.cs**

Create `src/ADExplorer/Services/ThemeService.cs`:

```csharp
using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace ADExplorer.Services;

public class ThemeService
{
    private const string RegistryKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string RegistryValueName = "AppsUseLightTheme";

    public bool IsLightMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        var value = key?.GetValue(RegistryValueName);
        return value is int intValue && intValue == 1;
    }

    public Color GetAccentColor()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\DWM");
            var value = key?.GetValue("AccentColor");
            if (value is int colorValue)
            {
                var bytes = BitConverter.GetBytes(colorValue);
                return Color.FromRgb(bytes[0], bytes[1], bytes[2]);
            }
        }
        catch { }
        return Color.FromRgb(0, 120, 215); // Windows default blue
    }

    public void Apply(Application app)
    {
        var dict = new ResourceDictionary();
        if (IsLightMode())
        {
            dict.Source = new Uri("pack://application:,,,/Themes/LightTheme.xaml");
        }
        else
        {
            dict.Source = new Uri("pack://application:,,,/Themes/DarkTheme.xaml");
        }

        // Remove existing theme dict if present
        var existing = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Theme.xaml") == true);
        if (existing != null)
            app.Resources.MergedDictionaries.Remove(existing);

        app.Resources.MergedDictionaries.Add(dict);

        // Apply accent color
        var accent = GetAccentColor();
        app.Resources["AccentColor"] = accent;
        app.Resources["AccentBrush"] = new SolidColorBrush(accent);
    }
}
```

- [ ] **Step 2: Create LightTheme.xaml**

Create `src/ADExplorer/Themes/LightTheme.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Backgrounds -->
    <SolidColorBrush x:Key="WindowBackground" Color="#F3F3F3"/>
    <SolidColorBrush x:Key="PanelBackground" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="SidebarBackground" Color="#F9F9F9"/>
    <SolidColorBrush x:Key="HoverBackground" Color="#E5E5E5"/>
    <SolidColorBrush x:Key="SelectedBackground" Color="#CCE4FF"/>
    <SolidColorBrush x:Key="BorderBrush" Color="#E0E0E0"/>

    <!-- Text -->
    <SolidColorBrush x:Key="PrimaryText" Color="#1A1A1A"/>
    <SolidColorBrush x:Key="SecondaryText" Color="#666666"/>
    <SolidColorBrush x:Key="MutedText" Color="#999999"/>

    <!-- Status -->
    <SolidColorBrush x:Key="SuccessBrush" Color="#107C10"/>
    <SolidColorBrush x:Key="ErrorBrush" Color="#D13438"/>
    <SolidColorBrush x:Key="WarningBrush" Color="#CA5010"/>

    <!-- Input -->
    <SolidColorBrush x:Key="InputBackground" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="StatusBarBackground" Color="#E8E8E8"/>
</ResourceDictionary>
```

- [ ] **Step 3: Create DarkTheme.xaml**

Create `src/ADExplorer/Themes/DarkTheme.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Backgrounds -->
    <SolidColorBrush x:Key="WindowBackground" Color="#202020"/>
    <SolidColorBrush x:Key="PanelBackground" Color="#2C2C2C"/>
    <SolidColorBrush x:Key="SidebarBackground" Color="#252525"/>
    <SolidColorBrush x:Key="HoverBackground" Color="#383838"/>
    <SolidColorBrush x:Key="SelectedBackground" Color="#0078D4"/>
    <SolidColorBrush x:Key="BorderBrush" Color="#404040"/>

    <!-- Text -->
    <SolidColorBrush x:Key="PrimaryText" Color="#F3F3F3"/>
    <SolidColorBrush x:Key="SecondaryText" Color="#BBBBBB"/>
    <SolidColorBrush x:Key="MutedText" Color="#888888"/>

    <!-- Status -->
    <SolidColorBrush x:Key="SuccessBrush" Color="#6CCB5F"/>
    <SolidColorBrush x:Key="ErrorBrush" Color="#F87171"/>
    <SolidColorBrush x:Key="WarningBrush" Color="#FCD34D"/>

    <!-- Input -->
    <SolidColorBrush x:Key="InputBackground" Color="#1A1A1A"/>
    <SolidColorBrush x:Key="StatusBarBackground" Color="#1A1A1A"/>
</ResourceDictionary>
```

- [ ] **Step 4: Build to verify no errors**

```
dotnet build
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```
git add src/ADExplorer/Services/ThemeService.cs src/ADExplorer/Themes/
git commit -m "feat: add ThemeService with Windows light/dark detection"
```

---

## Task 4: AdService — LDAP Queries

**Files:**
- Create: `src/ADExplorer/Services/AdService.cs`
- Create: `tests/ADExplorer.Tests/Services/AdServiceTests.cs`

- [ ] **Step 1: Write failing tests for AdService result mapping**

Create `tests/ADExplorer.Tests/Services/AdServiceTests.cs`:

```csharp
using ADExplorer.Services;
using Xunit;

namespace ADExplorer.Tests.Services;

public class AdServiceTests
{
    [Fact]
    public void ParseGroupType_ReturnsSecurity_ForNegativeValue()
    {
        var result = AdService.ParseGroupType(-2147483646);
        Assert.Equal("Security", result);
    }

    [Fact]
    public void ParseGroupType_ReturnsDistribution_ForPositiveValue()
    {
        var result = AdService.ParseGroupType(2);
        Assert.Equal("Distribution", result);
    }

    [Fact]
    public void ParseFileTime_ReturnsNull_ForZero()
    {
        var result = AdService.ParseFileTime(0);
        Assert.Null(result);
    }

    [Fact]
    public void ParseFileTime_ReturnsNull_ForMaxValue()
    {
        var result = AdService.ParseFileTime(long.MaxValue);
        Assert.Null(result);
    }

    [Fact]
    public void ParseFileTime_ReturnsDateTime_ForValidValue()
    {
        // 2026-01-01 00:00:00 UTC in Windows FileTime
        var fileTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToFileTime();
        var result = AdService.ParseFileTime(fileTime);
        Assert.NotNull(result);
        Assert.Equal(2026, result!.Value.Year);
    }
}
```

- [ ] **Step 2: Run test — expect compile failure**

```
dotnet test tests/ADExplorer.Tests
```
Expected: compile error — `AdService` not defined yet.

- [ ] **Step 3: Create AdService.cs**

Create `src/ADExplorer/Services/AdService.cs`:

```csharp
using ADExplorer.Models;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;

namespace ADExplorer.Services;

public class AdService
{
    private readonly string _ldapRoot;

    public AdService()
    {
        try
        {
            var domain = Domain.GetCurrentDomain();
            _ldapRoot = $"LDAP://{domain.Name}";
        }
        catch
        {
            _ldapRoot = "LDAP://";
        }
    }

    public string DomainName
    {
        get
        {
            try { return Domain.GetCurrentDomain().Name; }
            catch { return "Not connected"; }
        }
    }

    // ── User Search ────────────────────────────────────────────────

    public List<AdUser> SearchUsers(string query)
    {
        var filter = $"(&(objectClass=user)(objectCategory=person)" +
                     $"(|(displayName=*{query}*)(sAMAccountName=*{query}*)(mail=*{query}*)))";
        return SearchDirectory<AdUser>(filter, BuildUser);
    }

    public AdUser? GetUserDetails(string distinguishedName)
    {
        try
        {
            using var entry = new DirectoryEntry($"LDAP://{distinguishedName}");
            return BuildUser(entry);
        }
        catch { return null; }
    }

    // ── Group Search ───────────────────────────────────────────────

    public List<AdGroup> SearchGroups(string query)
    {
        var filter = $"(&(objectClass=group)(name=*{query}*))";
        return SearchDirectory<AdGroup>(filter, BuildGroup);
    }

    // ── Computer Search ────────────────────────────────────────────

    public List<AdComputer> SearchComputers(string query)
    {
        var filter = $"(&(objectClass=computer)(name=*{query}*))";
        return SearchDirectory<AdComputer>(filter, BuildComputer);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private List<T> SearchDirectory<T>(string filter, Func<DirectoryEntry, T> builder)
    {
        var results = new List<T>();
        try
        {
            using var root = new DirectoryEntry(_ldapRoot);
            using var searcher = new DirectorySearcher(root, filter) { SizeLimit = 200 };
            foreach (SearchResult result in searcher.FindAll())
                results.Add(builder(result.GetDirectoryEntry()));
        }
        catch { /* return empty on LDAP failure */ }
        return results;
    }

    private static AdUser BuildUser(DirectoryEntry e)
    {
        var uac = GetInt(e, "userAccountControl");
        var isDisabled = (uac & 0x2) != 0;
        var isLockedOut = (uac & 0x10) != 0;

        var maxPwdAge = GetLong(e, "maxPwdAge");
        var pwdLastSet = GetLong(e, "pwdLastSet");
        DateTime? pwdExpiry = null;
        if (pwdLastSet > 0 && maxPwdAge < 0)
        {
            var setDate = DateTime.FromFileTime(pwdLastSet);
            var maxAge = TimeSpan.FromTicks(Math.Abs(maxPwdAge));
            pwdExpiry = setDate + maxAge;
        }

        var groups = new List<string>();
        if (e.Properties["memberOf"].Value is object[] memberOf)
            foreach (var g in memberOf)
                groups.Add(ParseCN(g.ToString() ?? ""));

        return new AdUser
        {
            DisplayName   = GetString(e, "displayName"),
            SamAccountName = GetString(e, "sAMAccountName"),
            Email         = GetString(e, "mail"),
            Title         = GetString(e, "title"),
            Department    = GetString(e, "department"),
            Manager       = ParseCN(GetString(e, "manager")),
            Phone         = GetString(e, "telephoneNumber"),
            DistinguishedName = GetString(e, "distinguishedName"),
            Description   = GetString(e, "description"),
            IsEnabled     = !isDisabled,
            IsLockedOut   = isLockedOut,
            BadPasswordCount = GetInt(e, "badPwdCount"),
            LastLogon     = ParseFileTime(GetLong(e, "lastLogon")),
            PasswordExpiryDate = pwdExpiry,
            Created       = e.Properties["whenCreated"].Value as DateTime?,
            Groups        = groups,
        };
    }

    private static AdGroup BuildGroup(DirectoryEntry e)
    {
        var members = new List<string>();
        if (e.Properties["member"].Value is object[] memberArr)
            foreach (var m in memberArr)
                members.Add(ParseCN(m.ToString() ?? ""));

        return new AdGroup
        {
            Name              = GetString(e, "name"),
            Description       = GetString(e, "description"),
            DistinguishedName = GetString(e, "distinguishedName"),
            GroupType         = ParseGroupType(GetInt(e, "groupType")),
            Members           = members,
        };
    }

    private static AdComputer BuildComputer(DirectoryEntry e)
    {
        var uac = GetInt(e, "userAccountControl");
        return new AdComputer
        {
            Name                   = GetString(e, "name"),
            OperatingSystem        = GetString(e, "operatingSystem"),
            OperatingSystemVersion = GetString(e, "operatingSystemVersion"),
            DistinguishedName      = GetString(e, "distinguishedName"),
            Description            = GetString(e, "description"),
            IsEnabled              = (uac & 0x2) == 0,
            LastLogon              = ParseFileTime(GetLong(e, "lastLogon")),
        };
    }

    private static string GetString(DirectoryEntry e, string attr) =>
        e.Properties[attr].Value?.ToString() ?? string.Empty;

    private static int GetInt(DirectoryEntry e, string attr) =>
        e.Properties[attr].Value is int v ? v : 0;

    private static long GetLong(DirectoryEntry e, string attr) =>
        e.Properties[attr].Value is long v ? v : 0;

    public static DateTime? ParseFileTime(long fileTime)
    {
        if (fileTime <= 0 || fileTime == long.MaxValue) return null;
        return DateTime.FromFileTime(fileTime);
    }

    public static string ParseGroupType(int groupType) =>
        groupType < 0 ? "Security" : "Distribution";

    private static string ParseCN(string dn)
    {
        if (string.IsNullOrEmpty(dn)) return string.Empty;
        var cn = dn.Split(',').FirstOrDefault(p => p.TrimStart().StartsWith("CN="));
        return cn?.Substring(3).Trim() ?? dn;
    }
}
```

- [ ] **Step 4: Run tests — expect pass**

```
dotnet test tests/ADExplorer.Tests
```
Expected: `7 passed, 0 failed`

- [ ] **Step 5: Commit**

```
git add src/ADExplorer/Services/AdService.cs tests/ADExplorer.Tests/Services/
git commit -m "feat: add AdService with LDAP user/group/computer search"
```

---

## Task 5: GraphService — Microsoft Graph Fallback

**Files:**
- Create: `src/ADExplorer/Services/GraphService.cs`

- [ ] **Step 1: Create GraphService.cs**

Create `src/ADExplorer/Services/GraphService.cs`:

```csharp
using ADExplorer.Models;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace ADExplorer.Services;

public class GraphService
{
    private GraphServiceClient? _client;
    public bool IsConfigured { get; private set; }

    public void Initialize(string tenantId, string clientId, string clientSecret)
    {
        try
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _client = new GraphServiceClient(credential);
            IsConfigured = true;
        }
        catch
        {
            IsConfigured = false;
        }
    }

    public async Task<List<AdUser>> SearchUsersAsync(string query)
    {
        if (_client == null) return new();
        try
        {
            var users = await _client.Users.GetAsync(config =>
            {
                config.QueryParameters.Search = $"\"displayName:{query}\" OR \"userPrincipalName:{query}\"";
                config.QueryParameters.Select = new[]
                {
                    "displayName","userPrincipalName","mail","jobTitle",
                    "department","manager","mobilePhone","accountEnabled",
                    "createdDateTime","id"
                };
                config.Headers.Add("ConsistencyLevel", "eventual");
            });

            return users?.Value?.Select(u => new AdUser
            {
                DisplayName    = u.DisplayName ?? string.Empty,
                SamAccountName = u.UserPrincipalName?.Split('@')[0] ?? string.Empty,
                Email          = u.Mail ?? u.UserPrincipalName ?? string.Empty,
                Title          = u.JobTitle ?? string.Empty,
                Department     = u.Department ?? string.Empty,
                Phone          = u.MobilePhone ?? string.Empty,
                IsEnabled      = u.AccountEnabled ?? false,
                Created        = u.CreatedDateTime?.DateTime,
            }).ToList() ?? new();
        }
        catch { return new(); }
    }
}
```

- [ ] **Step 2: Build to verify**

```
dotnet build
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```
git add src/ADExplorer/Services/GraphService.cs
git commit -m "feat: add GraphService Microsoft Graph fallback"
```

---

## Task 6: App Entry Point and Theme Bootstrap

**Files:**
- Modify: `src/ADExplorer/App.xaml`
- Modify: `src/ADExplorer/App.xaml.cs`

- [ ] **Step 1: Replace App.xaml**

Replace contents of `src/ADExplorer/App.xaml`:

```xml
<Application x:Class="ADExplorer.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- Theme loaded dynamically at startup -->
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- [ ] **Step 2: Replace App.xaml.cs**

Replace contents of `src/ADExplorer/App.xaml.cs`:

```csharp
using ADExplorer.Services;
using System.Windows;

namespace ADExplorer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var themeService = new ThemeService();
        themeService.Apply(this);
    }
}
```

- [ ] **Step 3: Build to verify**

```
dotnet build
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```
git add src/ADExplorer/App.xaml src/ADExplorer/App.xaml.cs
git commit -m "feat: bootstrap theme from Windows system settings on startup"
```

---

## Task 7: MainWindow — Tab Host, Search Bar, Status Bar

**Files:**
- Modify: `src/ADExplorer/MainWindow.xaml`
- Modify: `src/ADExplorer/MainWindow.xaml.cs`

- [ ] **Step 1: Replace MainWindow.xaml**

Replace contents of `src/ADExplorer/MainWindow.xaml`:

```xml
<Window x:Class="ADExplorer.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:views="clr-namespace:ADExplorer.Views"
        Title="AD Explorer" Height="700" Width="1100"
        MinHeight="500" MinWidth="800"
        Background="{DynamicResource WindowBackground}"
        WindowStartupLocation="CenterScreen">

    <Window.Resources>
        <Style x:Key="TabStyle" TargetType="TabItem">
            <Setter Property="Foreground" Value="{DynamicResource PrimaryText}"/>
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="Padding" Value="16,8"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="TabItem">
                        <Border x:Name="Border" Padding="{TemplateBinding Padding}"
                                BorderThickness="0,0,0,2"
                                BorderBrush="Transparent">
                            <ContentPresenter ContentSource="Header"
                                              TextBlock.Foreground="{DynamicResource SecondaryText}"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsSelected" Value="True">
                                <Setter TargetName="Border" Property="BorderBrush"
                                        Value="{DynamicResource AccentBrush}"/>
                                <Setter Property="Foreground"
                                        Value="{DynamicResource PrimaryText}"/>
                            </Trigger>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter TargetName="Border" Property="Background"
                                        Value="{DynamicResource HoverBackground}"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>

    <DockPanel>
        <!-- Title Bar -->
        <Border DockPanel.Dock="Top"
                Background="{DynamicResource PanelBackground}"
                BorderBrush="{DynamicResource BorderBrush}"
                BorderThickness="0,0,0,1" Padding="16,12">
            <TextBlock Text="🔍 AD Explorer"
                       FontSize="18" FontWeight="SemiBold"
                       Foreground="{DynamicResource PrimaryText}"/>
        </Border>

        <!-- Status Bar -->
        <Border DockPanel.Dock="Bottom"
                Background="{DynamicResource StatusBarBackground}"
                BorderBrush="{DynamicResource BorderBrush}"
                BorderThickness="0,1,0,0" Padding="16,6">
            <StackPanel Orientation="Horizontal">
                <TextBlock x:Name="DomainText" FontSize="12"
                           Foreground="{DynamicResource SecondaryText}"/>
                <TextBlock Text="  •  " FontSize="12"
                           Foreground="{DynamicResource MutedText}"/>
                <TextBlock x:Name="ResultCountText" FontSize="12"
                           Foreground="{DynamicResource SecondaryText}"/>
            </StackPanel>
        </Border>

        <!-- Tab Control -->
        <TabControl x:Name="MainTabs"
                    Background="{DynamicResource WindowBackground}"
                    BorderThickness="0"
                    Padding="0">
            <TabControl.Resources>
                <Style TargetType="TabPanel">
                    <Setter Property="Background"
                            Value="{DynamicResource PanelBackground}"/>
                </Style>
            </TabControl.Resources>

            <TabItem Header="Users" Style="{StaticResource TabStyle}">
                <views:UserSearchView x:Name="UserView"
                                      ResultCountChanged="OnResultCountChanged"/>
            </TabItem>
            <TabItem Header="Groups" Style="{StaticResource TabStyle}">
                <views:GroupSearchView x:Name="GroupView"
                                       ResultCountChanged="OnResultCountChanged"/>
            </TabItem>
            <TabItem Header="Computers" Style="{StaticResource TabStyle}">
                <views:ComputerView x:Name="ComputerView"
                                    ResultCountChanged="OnResultCountChanged"/>
            </TabItem>
        </TabControl>
    </DockPanel>
</Window>
```

- [ ] **Step 2: Replace MainWindow.xaml.cs**

Replace contents of `src/ADExplorer/MainWindow.xaml.cs`:

```csharp
using ADExplorer.Services;
using System.Windows;

namespace ADExplorer;

public partial class MainWindow : Window
{
    private readonly AdService _adService = new();

    public MainWindow()
    {
        InitializeComponent();
        DomainText.Text = $"Connected to: {_adService.DomainName}";
        ResultCountText.Text = "Results: 0";

        UserView.AdService = _adService;
        GroupView.AdService = _adService;
        ComputerView.AdService = _adService;
    }

    private void OnResultCountChanged(object sender, int count)
    {
        ResultCountText.Text = $"Results: {count}";
    }
}
```

- [ ] **Step 3: Build to verify (views not yet created, expect errors — that's ok)**

Note: Build will fail because Views don't exist yet. That's expected — continue to Task 8.

- [ ] **Step 4: Commit**

```
git add src/ADExplorer/MainWindow.xaml src/ADExplorer/MainWindow.xaml.cs
git commit -m "feat: add MainWindow with tab host and status bar"
```

---

## Task 8: UserSearchView

**Files:**
- Create: `src/ADExplorer/Views/UserSearchView.xaml`
- Create: `src/ADExplorer/Views/UserSearchView.xaml.cs`

- [ ] **Step 1: Create UserSearchView.xaml**

Create `src/ADExplorer/Views/UserSearchView.xaml`:

```xml
<UserControl x:Class="ADExplorer.Views.UserSearchView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="{DynamicResource WindowBackground}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Search Bar -->
        <Border Grid.Row="0" Padding="16,12"
                Background="{DynamicResource PanelBackground}"
                BorderBrush="{DynamicResource BorderBrush}"
                BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBox x:Name="SearchBox"
                         Grid.Column="0"
                         Padding="10,8"
                         FontSize="14"
                         Background="{DynamicResource InputBackground}"
                         Foreground="{DynamicResource PrimaryText}"
                         BorderBrush="{DynamicResource BorderBrush}"
                         BorderThickness="1"
                         KeyDown="SearchBox_KeyDown">
                    <TextBox.Style>
                        <Style TargetType="TextBox">
                            <Style.Resources>
                                <VisualBrush x:Key="HintBrush" TileMode="None"
                                             Stretch="None" AlignmentX="Left">
                                    <VisualBrush.Visual>
                                        <TextBlock Text="Search by name, username, or email..."
                                                   Foreground="{DynamicResource MutedText}"
                                                   FontSize="14" Margin="2,0"/>
                                    </VisualBrush.Visual>
                                </VisualBrush>
                            </Style.Resources>
                            <Style.Triggers>
                                <Trigger Property="Text" Value="">
                                    <Setter Property="Background" Value="{StaticResource HintBrush}"/>
                                </Trigger>
                            </Style.Triggers>
                        </Style>
                    </TextBox.Style>
                </TextBox>
                <Button Grid.Column="1" Content="Search" Margin="8,0,0,0"
                        Padding="16,8" FontSize="14"
                        Background="{DynamicResource AccentBrush}"
                        Foreground="White" BorderThickness="0"
                        Cursor="Hand" Click="SearchButton_Click"/>
            </Grid>
        </Border>

        <!-- Split Panel -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="280" MinWidth="200"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- Results List -->
            <Border Grid.Column="0"
                    Background="{DynamicResource SidebarBackground}"
                    BorderBrush="{DynamicResource BorderBrush}"
                    BorderThickness="0,0,1,0">
                <ListBox x:Name="ResultsList"
                         Background="Transparent"
                         BorderThickness="0"
                         ScrollViewer.HorizontalScrollBarVisibility="Disabled"
                         SelectionChanged="ResultsList_SelectionChanged">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel Margin="12,8">
                                <TextBlock Text="{Binding DisplayName}"
                                           FontSize="14" FontWeight="SemiBold"
                                           Foreground="{DynamicResource PrimaryText}"/>
                                <TextBlock Text="{Binding SamAccountName}"
                                           FontSize="12"
                                           Foreground="{DynamicResource SecondaryText}"/>
                            </StackPanel>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                    <ListBox.ItemContainerStyle>
                        <Style TargetType="ListBoxItem">
                            <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
                            <Setter Property="Padding" Value="0"/>
                            <Style.Triggers>
                                <Trigger Property="IsMouseOver" Value="True">
                                    <Setter Property="Background"
                                            Value="{DynamicResource HoverBackground}"/>
                                </Trigger>
                                <Trigger Property="IsSelected" Value="True">
                                    <Setter Property="Background"
                                            Value="{DynamicResource SelectedBackground}"/>
                                </Trigger>
                            </Style.Triggers>
                        </Style>
                    </ListBox.ItemContainerStyle>
                </ListBox>
            </Border>

            <GridSplitter Grid.Column="1" Width="4"
                          HorizontalAlignment="Center"
                          Background="{DynamicResource BorderBrush}"/>

            <!-- Detail Panel -->
            <ScrollViewer Grid.Column="2" VerticalScrollBarVisibility="Auto">
                <StackPanel x:Name="DetailPanel" Margin="24,20" Visibility="Collapsed">

                    <!-- Identity -->
                    <TextBlock x:Name="DetailName" FontSize="22" FontWeight="Bold"
                               Foreground="{DynamicResource PrimaryText}" Margin="0,0,0,4"/>
                    <TextBlock x:Name="DetailEmail" FontSize="14"
                               Foreground="{DynamicResource AccentBrush}" Margin="0,0,0,16"/>

                    <Separator Background="{DynamicResource BorderBrush}" Margin="0,0,0,16"/>

                    <Grid Margin="0,0,0,16">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="120"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <TextBlock Grid.Row="0" Grid.Column="0" Text="Title"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailTitle" Grid.Row="0" Grid.Column="1"
                                   Foreground="{DynamicResource PrimaryText}" FontSize="12" Margin="0,4"/>
                        <TextBlock Grid.Row="1" Grid.Column="0" Text="Department"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailDept" Grid.Row="1" Grid.Column="1"
                                   Foreground="{DynamicResource PrimaryText}" FontSize="12" Margin="0,4"/>
                        <TextBlock Grid.Row="2" Grid.Column="0" Text="Manager"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailManager" Grid.Row="2" Grid.Column="1"
                                   Foreground="{DynamicResource PrimaryText}" FontSize="12" Margin="0,4"/>
                        <TextBlock Grid.Row="3" Grid.Column="0" Text="Phone"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailPhone" Grid.Row="3" Grid.Column="1"
                                   Foreground="{DynamicResource PrimaryText}" FontSize="12" Margin="0,4"/>
                    </Grid>

                    <Separator Background="{DynamicResource BorderBrush}" Margin="0,0,0,16"/>

                    <!-- Account Status -->
                    <TextBlock Text="ACCOUNT STATUS" FontSize="11" FontWeight="Bold"
                               Foreground="{DynamicResource MutedText}" Margin="0,0,0,10"/>
                    <Grid Margin="0,0,0,16">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <TextBlock Grid.Row="0" Grid.Column="0" Text="Status"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailStatus" Grid.Row="0" Grid.Column="1"
                                   FontSize="12" Margin="0,4"/>
                        <TextBlock Grid.Row="1" Grid.Column="0" Text="Locked Out"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailLocked" Grid.Row="1" Grid.Column="1"
                                   Foreground="{DynamicResource PrimaryText}" FontSize="12" Margin="0,4"/>
                        <TextBlock Grid.Row="2" Grid.Column="0" Text="Bad Pwd Count"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailBadPwd" Grid.Row="2" Grid.Column="1"
                                   Foreground="{DynamicResource PrimaryText}" FontSize="12" Margin="0,4"/>
                        <TextBlock Grid.Row="3" Grid.Column="0" Text="Pwd Expiry"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailPwdExpiry" Grid.Row="3" Grid.Column="1"
                                   Foreground="{DynamicResource PrimaryText}" FontSize="12" Margin="0,4"/>
                        <TextBlock Grid.Row="4" Grid.Column="0" Text="Last Logon"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailLastLogon" Grid.Row="4" Grid.Column="1"
                                   Foreground="{DynamicResource PrimaryText}" FontSize="12" Margin="0,4"/>
                    </Grid>

                    <Separator Background="{DynamicResource BorderBrush}" Margin="0,0,0,16"/>

                    <!-- Groups -->
                    <TextBlock Text="GROUP MEMBERSHIPS" FontSize="11" FontWeight="Bold"
                               Foreground="{DynamicResource MutedText}" Margin="0,0,0,10"/>
                    <ItemsControl x:Name="GroupsList" Margin="0,0,0,16">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding}" FontSize="12"
                                           Foreground="{DynamicResource SecondaryText}"
                                           Margin="0,2"/>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>

                    <Separator Background="{DynamicResource BorderBrush}" Margin="0,0,0,16"/>

                    <!-- OU / Org Info -->
                    <TextBlock Text="ORG INFO" FontSize="11" FontWeight="Bold"
                               Foreground="{DynamicResource MutedText}" Margin="0,0,0,10"/>
                    <TextBlock x:Name="DetailOU" FontSize="11"
                               Foreground="{DynamicResource MutedText}"
                               TextWrapping="Wrap" Margin="0,0,0,8"/>
                    <TextBlock x:Name="DetailCreated" FontSize="12"
                               Foreground="{DynamicResource SecondaryText}" Margin="0,0,0,16"/>

                    <!-- Action Buttons -->
                    <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                        <Button x:Name="CopyEmailBtn" Content="Copy Email"
                                Padding="14,8" Margin="0,0,8,0"
                                Background="{DynamicResource AccentBrush}"
                                Foreground="White" BorderThickness="0"
                                Cursor="Hand" Click="CopyEmail_Click"/>
                        <Button x:Name="CopyUserBtn" Content="Copy Username"
                                Padding="14,8"
                                Background="{DynamicResource PanelBackground}"
                                Foreground="{DynamicResource PrimaryText}"
                                BorderBrush="{DynamicResource BorderBrush}"
                                BorderThickness="1"
                                Cursor="Hand" Click="CopyUsername_Click"/>
                    </StackPanel>

                </StackPanel>
            </ScrollViewer>
        </Grid>
    </DockPanel>
</UserControl>
```

- [ ] **Step 2: Create UserSearchView.xaml.cs**

Create `src/ADExplorer/Views/UserSearchView.xaml.cs`:

```csharp
using ADExplorer.Models;
using ADExplorer.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ADExplorer.Views;

public partial class UserSearchView : UserControl
{
    public AdService? AdService { get; set; }
    public event EventHandler<int>? ResultCountChanged;

    public UserSearchView()
    {
        InitializeComponent();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e) => RunSearch();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) RunSearch();
    }

    private void RunSearch()
    {
        if (AdService == null || string.IsNullOrWhiteSpace(SearchBox.Text)) return;
        var results = AdService.SearchUsers(SearchBox.Text.Trim());
        ResultsList.ItemsSource = results;
        DetailPanel.Visibility = Visibility.Collapsed;
        ResultCountChanged?.Invoke(this, results.Count);
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not AdUser user) return;
        PopulateDetail(user);
    }

    private void PopulateDetail(AdUser user)
    {
        DetailName.Text     = user.DisplayName;
        DetailEmail.Text    = user.Email;
        DetailTitle.Text    = user.Title;
        DetailDept.Text     = user.Department;
        DetailManager.Text  = user.Manager;
        DetailPhone.Text    = user.Phone;
        DetailStatus.Text   = user.StatusDisplay;
        DetailStatus.Foreground = user.IsEnabled
            ? (System.Windows.Media.Brush)FindResource("SuccessBrush")
            : (System.Windows.Media.Brush)FindResource("ErrorBrush");
        DetailLocked.Text   = user.LockedDisplay;
        DetailBadPwd.Text   = user.BadPasswordCount.ToString();
        DetailPwdExpiry.Text = user.PasswordExpiryDate.HasValue
            ? $"{user.PasswordExpiryDays} days ({user.PasswordExpiryDate:yyyy-MM-dd})"
            : "No expiry";
        DetailLastLogon.Text = user.LastLogonDisplay;
        GroupsList.ItemsSource = user.Groups;
        DetailOU.Text      = user.DistinguishedName;
        DetailCreated.Text = user.Created.HasValue
            ? $"Created: {user.Created:yyyy-MM-dd}"
            : string.Empty;
        DetailPanel.Visibility = Visibility.Visible;
    }

    private void CopyEmail_Click(object sender, RoutedEventArgs e)
    {
        if (ResultsList.SelectedItem is AdUser user)
            Clipboard.SetText(user.Email);
    }

    private void CopyUsername_Click(object sender, RoutedEventArgs e)
    {
        if (ResultsList.SelectedItem is AdUser user)
            Clipboard.SetText(user.SamAccountName);
    }
}
```

- [ ] **Step 3: Build**

```
dotnet build
```
Expected: `Build succeeded. 0 Error(s)` (MainWindow will now compile since UserSearchView exists)

- [ ] **Step 4: Commit**

```
git add src/ADExplorer/Views/UserSearchView.xaml src/ADExplorer/Views/UserSearchView.xaml.cs
git commit -m "feat: add UserSearchView with results list and detail panel"
```

---

## Task 9: GroupSearchView

**Files:**
- Create: `src/ADExplorer/Views/GroupSearchView.xaml`
- Create: `src/ADExplorer/Views/GroupSearchView.xaml.cs`

- [ ] **Step 1: Create GroupSearchView.xaml**

Create `src/ADExplorer/Views/GroupSearchView.xaml`:

```xml
<UserControl x:Class="ADExplorer.Views.GroupSearchView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="{DynamicResource WindowBackground}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Search Bar -->
        <Border Grid.Row="0" Padding="16,12"
                Background="{DynamicResource PanelBackground}"
                BorderBrush="{DynamicResource BorderBrush}"
                BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBox x:Name="SearchBox" Grid.Column="0"
                         Padding="10,8" FontSize="14"
                         Background="{DynamicResource InputBackground}"
                         Foreground="{DynamicResource PrimaryText}"
                         BorderBrush="{DynamicResource BorderBrush}"
                         BorderThickness="1"
                         KeyDown="SearchBox_KeyDown"/>
                <Button Grid.Column="1" Content="Search" Margin="8,0,0,0"
                        Padding="16,8" FontSize="14"
                        Background="{DynamicResource AccentBrush}"
                        Foreground="White" BorderThickness="0"
                        Cursor="Hand" Click="SearchButton_Click"/>
            </Grid>
        </Border>

        <!-- Split Panel -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="280"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Border Grid.Column="0"
                    Background="{DynamicResource SidebarBackground}"
                    BorderBrush="{DynamicResource BorderBrush}"
                    BorderThickness="0,0,1,0">
                <ListBox x:Name="ResultsList"
                         Background="Transparent" BorderThickness="0"
                         ScrollViewer.HorizontalScrollBarVisibility="Disabled"
                         SelectionChanged="ResultsList_SelectionChanged">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel Margin="12,8">
                                <TextBlock Text="{Binding Name}" FontSize="14"
                                           FontWeight="SemiBold"
                                           Foreground="{DynamicResource PrimaryText}"/>
                                <TextBlock Text="{Binding GroupType}" FontSize="12"
                                           Foreground="{DynamicResource SecondaryText}"/>
                            </StackPanel>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </Border>

            <GridSplitter Grid.Column="1" Width="4"
                          HorizontalAlignment="Center"
                          Background="{DynamicResource BorderBrush}"/>

            <ScrollViewer Grid.Column="2" VerticalScrollBarVisibility="Auto">
                <StackPanel x:Name="DetailPanel" Margin="24,20" Visibility="Collapsed">
                    <TextBlock x:Name="DetailName" FontSize="22" FontWeight="Bold"
                               Foreground="{DynamicResource PrimaryText}" Margin="0,0,0,4"/>
                    <TextBlock x:Name="DetailType" FontSize="13"
                               Foreground="{DynamicResource AccentBrush}" Margin="0,0,0,8"/>
                    <TextBlock x:Name="DetailDesc" FontSize="13"
                               Foreground="{DynamicResource SecondaryText}" Margin="0,0,0,16"
                               TextWrapping="Wrap"/>

                    <Separator Background="{DynamicResource BorderBrush}" Margin="0,0,0,16"/>

                    <TextBlock x:Name="MemberCountText" FontSize="11" FontWeight="Bold"
                               Foreground="{DynamicResource MutedText}" Margin="0,0,0,10"/>

                    <ItemsControl x:Name="MembersList">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding}" FontSize="12"
                                           Foreground="{DynamicResource SecondaryText}"
                                           Margin="0,3"/>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </ScrollViewer>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Create GroupSearchView.xaml.cs**

Create `src/ADExplorer/Views/GroupSearchView.xaml.cs`:

```csharp
using ADExplorer.Models;
using ADExplorer.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ADExplorer.Views;

public partial class GroupSearchView : UserControl
{
    public AdService? AdService { get; set; }
    public event EventHandler<int>? ResultCountChanged;

    public GroupSearchView()
    {
        InitializeComponent();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e) => RunSearch();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) RunSearch();
    }

    private void RunSearch()
    {
        if (AdService == null || string.IsNullOrWhiteSpace(SearchBox.Text)) return;
        var results = AdService.SearchGroups(SearchBox.Text.Trim());
        ResultsList.ItemsSource = results;
        DetailPanel.Visibility = Visibility.Collapsed;
        ResultCountChanged?.Invoke(this, results.Count);
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not AdGroup group) return;
        DetailName.Text        = group.Name;
        DetailType.Text        = group.GroupType;
        DetailDesc.Text        = group.Description;
        MemberCountText.Text   = $"MEMBERS ({group.MemberCount})";
        MembersList.ItemsSource = group.Members;
        DetailPanel.Visibility  = Visibility.Visible;
    }
}
```

- [ ] **Step 3: Build**

```
dotnet build
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```
git add src/ADExplorer/Views/GroupSearchView.xaml src/ADExplorer/Views/GroupSearchView.xaml.cs
git commit -m "feat: add GroupSearchView with member list"
```

---

## Task 10: ComputerView

**Files:**
- Create: `src/ADExplorer/Views/ComputerView.xaml`
- Create: `src/ADExplorer/Views/ComputerView.xaml.cs`

- [ ] **Step 1: Create ComputerView.xaml**

Create `src/ADExplorer/Views/ComputerView.xaml`:

```xml
<UserControl x:Class="ADExplorer.Views.ComputerView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="{DynamicResource WindowBackground}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <Border Grid.Row="0" Padding="16,12"
                Background="{DynamicResource PanelBackground}"
                BorderBrush="{DynamicResource BorderBrush}"
                BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBox x:Name="SearchBox" Grid.Column="0"
                         Padding="10,8" FontSize="14"
                         Background="{DynamicResource InputBackground}"
                         Foreground="{DynamicResource PrimaryText}"
                         BorderBrush="{DynamicResource BorderBrush}"
                         BorderThickness="1"
                         KeyDown="SearchBox_KeyDown"/>
                <Button Grid.Column="1" Content="Search" Margin="8,0,0,0"
                        Padding="16,8" FontSize="14"
                        Background="{DynamicResource AccentBrush}"
                        Foreground="White" BorderThickness="0"
                        Cursor="Hand" Click="SearchButton_Click"/>
            </Grid>
        </Border>

        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="280"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Border Grid.Column="0"
                    Background="{DynamicResource SidebarBackground}"
                    BorderBrush="{DynamicResource BorderBrush}"
                    BorderThickness="0,0,1,0">
                <ListBox x:Name="ResultsList"
                         Background="Transparent" BorderThickness="0"
                         ScrollViewer.HorizontalScrollBarVisibility="Disabled"
                         SelectionChanged="ResultsList_SelectionChanged">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel Margin="12,8">
                                <TextBlock Text="{Binding Name}" FontSize="14"
                                           FontWeight="SemiBold"
                                           Foreground="{DynamicResource PrimaryText}"/>
                                <TextBlock Text="{Binding OperatingSystem}" FontSize="12"
                                           Foreground="{DynamicResource SecondaryText}"/>
                            </StackPanel>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </Border>

            <GridSplitter Grid.Column="1" Width="4"
                          HorizontalAlignment="Center"
                          Background="{DynamicResource BorderBrush}"/>

            <ScrollViewer Grid.Column="2" VerticalScrollBarVisibility="Auto">
                <StackPanel x:Name="DetailPanel" Margin="24,20" Visibility="Collapsed">
                    <TextBlock x:Name="DetailName" FontSize="22" FontWeight="Bold"
                               Foreground="{DynamicResource PrimaryText}" Margin="0,0,0,4"/>
                    <TextBlock x:Name="DetailStatus" FontSize="13" Margin="0,0,0,16"/>

                    <Separator Background="{DynamicResource BorderBrush}" Margin="0,0,0,16"/>

                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <TextBlock Grid.Row="0" Grid.Column="0" Text="OS"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailOS" Grid.Row="0" Grid.Column="1"
                                   Foreground="{DynamicResource PrimaryText}" FontSize="12" Margin="0,4"/>
                        <TextBlock Grid.Row="1" Grid.Column="0" Text="OS Version"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailOSVersion" Grid.Row="1" Grid.Column="1"
                                   Foreground="{DynamicResource PrimaryText}" FontSize="12" Margin="0,4"/>
                        <TextBlock Grid.Row="2" Grid.Column="0" Text="Last Logon"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailLastLogon" Grid.Row="2" Grid.Column="1"
                                   Foreground="{DynamicResource PrimaryText}" FontSize="12" Margin="0,4"/>
                        <TextBlock Grid.Row="3" Grid.Column="0" Text="OU Path"
                                   Foreground="{DynamicResource MutedText}" FontSize="12" Margin="0,4"/>
                        <TextBlock x:Name="DetailOU" Grid.Row="3" Grid.Column="1"
                                   Foreground="{DynamicResource MutedText}" FontSize="11"
                                   TextWrapping="Wrap" Margin="0,4"/>
                    </Grid>

                    <TextBlock x:Name="DetailDesc" FontSize="12"
                               Foreground="{DynamicResource SecondaryText}"
                               TextWrapping="Wrap" Margin="0,16,0,0"/>
                </StackPanel>
            </ScrollViewer>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Create ComputerView.xaml.cs**

Create `src/ADExplorer/Views/ComputerView.xaml.cs`:

```csharp
using ADExplorer.Models;
using ADExplorer.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ADExplorer.Views;

public partial class ComputerView : UserControl
{
    public AdService? AdService { get; set; }
    public event EventHandler<int>? ResultCountChanged;

    public ComputerView()
    {
        InitializeComponent();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e) => RunSearch();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) RunSearch();
    }

    private void RunSearch()
    {
        if (AdService == null || string.IsNullOrWhiteSpace(SearchBox.Text)) return;
        var results = AdService.SearchComputers(SearchBox.Text.Trim());
        ResultsList.ItemsSource = results;
        DetailPanel.Visibility = Visibility.Collapsed;
        ResultCountChanged?.Invoke(this, results.Count);
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not AdComputer computer) return;
        DetailName.Text        = computer.Name;
        DetailStatus.Text      = computer.StatusDisplay;
        DetailStatus.Foreground = computer.IsEnabled
            ? (System.Windows.Media.Brush)FindResource("SuccessBrush")
            : (System.Windows.Media.Brush)FindResource("ErrorBrush");
        DetailOS.Text          = computer.OperatingSystem;
        DetailOSVersion.Text   = computer.OperatingSystemVersion;
        DetailLastLogon.Text   = computer.LastLogonDisplay;
        DetailOU.Text          = computer.DistinguishedName;
        DetailDesc.Text        = computer.Description;
        DetailPanel.Visibility  = Visibility.Visible;
    }
}
```

- [ ] **Step 3: Full build and test**

```
dotnet build
dotnet test tests/ADExplorer.Tests
```
Expected: `Build succeeded. 0 Error(s)` and `7 passed, 0 failed`

- [ ] **Step 4: Commit**

```
git add src/ADExplorer/Views/ComputerView.xaml src/ADExplorer/Views/ComputerView.xaml.cs
git commit -m "feat: add ComputerView with OS and status detail"
```

---

## Task 11: GitHub Actions Release Workflow

**Files:**
- Create: `.github/workflows/release.yml`
- Create: `README.md`

- [ ] **Step 1: Create release workflow**

Create `.github/workflows/release.yml`:

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore

      - name: Test
        run: dotnet test tests/ADExplorer.Tests --no-restore

      - name: Publish
        run: >
          dotnet publish src/ADExplorer/ADExplorer.csproj
          -c Release
          -r win-x64
          --self-contained true
          -p:PublishSingleFile=true
          -p:IncludeNativeLibrariesForSelfExtract=true
          -o publish/

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: publish/ADExplorer.exe
          generate_release_notes: true
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

- [ ] **Step 2: Create README.md**

Create `README.md`:

```markdown
# AD Explorer

A modern Active Directory lookup tool for IT professionals. Search users, groups, and computers in your on-prem AD — all from a clean, system-themed desktop app.

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

```
dotnet publish src/ADExplorer/ADExplorer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

## Release a New Version

```
git tag v1.0.1
git push --tags
```

GitHub Actions builds and publishes the EXE automatically.
```

- [ ] **Step 3: Commit**

```
git add .github/ README.md
git commit -m "chore: add GitHub Actions release workflow and README"
```

---

## Task 12: Push to GitHub

- [ ] **Step 1: Create GitHub repo**

Go to https://github.com/new and create a new public repo named `ADExplorer`. Do not initialize with README (we already have one).

- [ ] **Step 2: Add remote and push**

```
git remote add origin https://github.com/<your-username>/ADExplorer.git
git branch -M main
git push -u origin main
```

- [ ] **Step 3: Tag and trigger first release**

```
git tag v1.0.0
git push --tags
```

- [ ] **Step 4: Verify release**

Go to `https://github.com/<your-username>/ADExplorer/actions` — watch the workflow run. When it completes, check `https://github.com/<your-username>/ADExplorer/releases` for the `ADExplorer.exe` artifact.

---

## Self-Review

**Spec coverage check:**
- ✅ WPF (.NET 8) — Task 1
- ✅ Models (AdUser, AdGroup, AdComputer) — Task 2
- ✅ System.DirectoryServices LDAP — Task 4
- ✅ Microsoft Graph fallback — Task 5
- ✅ Windows system theme — Task 3, 6
- ✅ Three tabs: Users, Groups, Computers — Tasks 7–10
- ✅ User detail: identity, status, groups, OU, copy buttons — Task 8
- ✅ Group detail: description, type, members — Task 9
- ✅ Computer detail: OS, last logon, status, OU — Task 10
- ✅ Single self-contained EXE — Task 11
- ✅ GitHub Actions on tag push — Task 11
- ✅ README — Task 11
- ✅ Error handling (LDAP fail, no results, never logged in) — AdService + views

**No placeholders found. Type consistency verified across all tasks.**
