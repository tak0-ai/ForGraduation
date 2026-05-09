# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade RuralTourism\RuralTourism.csproj
4. Upgrade RuralTourism.Api\RuralTourism.Api.csproj

## Settings

### Excluded projects

| Project name                                   | Description                 |
|:----------------------------------------------|:---------------------------:|
| *None*                                        | *No projects excluded*      |

### Aggregate NuGet packages modifications across all projects

| Package Name                        | Current Version | New Version | Description                                   |
|:------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.AspNetCore.Authentication.JwtBearer |     9.0.11      |   10.0.1    | Recommended for .NET 10.0 upgrade             |
| Microsoft.AspNetCore.OpenApi        |     9.0.11      |   10.0.1    | Recommended for .NET 10.0 upgrade             |
| Microsoft.EntityFrameworkCore      |     9.0.11      |   10.0.1    | Recommended for .NET 10.0 upgrade             |
| Microsoft.EntityFrameworkCore.Design |     9.0.11      |   10.0.1    | Recommended for .NET 10.0 upgrade             |
| Microsoft.EntityFrameworkCore.Sqlite |     9.0.11      |   10.0.1    | Recommended for .NET 10.0 upgrade             |
| Microsoft.Extensions.Http          |     9.0.11      |   10.0.1    | Recommended for .NET 10.0 upgrade             |
| Microsoft.Extensions.Logging.Debug |     9.0.8       |   10.0.1    | Recommended for .NET 10.0 upgrade             |

### Project upgrade details

#### RuralTourism\RuralTourism.csproj modifications

Project properties changes:
  - Target frameworks should be changed from `net9.0-android;net9.0-ios;net9.0-maccatalyst;net9.0-windows10.0.19041.0` to `net9.0-android;net9.0-ios;net9.0-maccatalyst;net9.0-windows10.0.19041.0;net10.0-windows`.

NuGet packages changes:
  - `Microsoft.Extensions.Http` should be updated from `9.0.11` to `10.0.1`.
  - `Microsoft.Extensions.Logging.Debug` should be updated from `9.0.8` to `10.0.1`.

Other changes:
  - Ensure new `net10.0-windows` target is wired into MAUI multi-target build and any platform-specific assets or runtimeconfig entries are aligned.

#### RuralTourism.Api\RuralTourism.Api.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`.

NuGet packages changes:
  - `Microsoft.AspNetCore.Authentication.JwtBearer` should be updated from `9.0.11` to `10.0.1`.
  - `Microsoft.AspNetCore.OpenApi` should be updated from `9.0.11` to `10.0.1`.
  - `Microsoft.EntityFrameworkCore` should be updated from `9.0.11` to `10.0.1`.
  - `Microsoft.EntityFrameworkCore.Design` should be updated from `9.0.11` to `10.0.1`.
  - `Microsoft.EntityFrameworkCore.Sqlite` should be updated from `9.0.11` to `10.0.1`.

Other changes:
  - Verify EF Core tooling and migrations remain compatible with the updated runtime and adjust any `dotnet ef` CLI usage if needed.
