# Building the Application

The project uses Windows' built-in .NET Framework compiler (`csc.exe`). No .NET SDK, NuGet, or internet connection required for building.

## Build Command

Close any running copy of the application, then double-click `Build-PortableExe.cmd` in the project root. It creates:

```
publish\NetSecSetup.exe
publish\packages.json
```

## Requirements for Building

- Windows 10/11 with .NET Framework 4.0+ (pre-installed)
- The source file: `src\NetSecSetupClassic\Program.cs`
- The manifest: `src\NetSecSetupClassic\app.manifest`
- The package config: `src\NetSecSetupClassic\packages.json`

## What the End User Needs

- Windows 10/11 (64-bit)
- Administrator approval (UAC)
- Internet connection (to download selected applications)
- `winget` / **App Installer** (included with Windows 11; auto-prompts if missing)

Nothing else needs to be installed before running `NetSecSetup.exe`.

## Publishing

1. Run `Build-PortableExe.cmd`
2. Create a GitHub Release
3. Upload `publish\NetSecSetup.exe` and `publish\packages.json` as release assets
4. Users download and run the EXE directly

For public releases, code-signing the EXE is recommended to reduce SmartScreen warnings.