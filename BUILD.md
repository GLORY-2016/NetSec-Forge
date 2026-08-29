# Building the one-click application

The project uses Windows' built-in .NET Framework compiler. Build it once, then distribute the generated `NetSecSetup.exe` file; people who run that file do **not** need .NET SDK, VS Code, Git, or PowerShell.

## Build command

Close any running copy of the application, then double-click `Build-PortableExe.cmd` in the project root. It creates:

```text
publish\NetSecSetup.exe
```

The build itself needs no package restore and no internet connection.

## What the end user needs

- Windows 10/11 (64-bit).
- An Internet connection, to download the selected applications.
- Administrator approval through the normal Windows UAC prompt.
- `winget` / **App Installer**, which is included with most current Windows 11 installations. If it is absent, the application opens its official Microsoft Store page.

Nothing else needs to be installed before opening `NetSecSetup.exe`.

## Publishing

Create a GitHub Release and upload `publish\NetSecSetup.exe` as its downloadable asset. Put the source project in the repository and direct visitors to the release download.

For a public release, code-signing the EXE is recommended. This reduces Windows SmartScreen warnings and shows users that the file is genuinely yours.
