<p align="center">
  <img src="assets/netsec-forge-logo.png" alt="NetSec Forge logo" width="200">
</p>

# NetSec Forge

A one-click Windows setup tool for programming, networking, and cybersecurity work. Open a single file, `NetSecSetup.exe`, pick the apps you need, and hit install — no PowerShell commands, no pre-installed .NET, VS Code, or Git required.

## Features

- Clean graphical interface with light and dark mode.
- Apps grouped by category: Essentials, Development, Browsers, Networking & Security, and Reverse Engineering.
- Automatic handling of dependencies: Docker Desktop enables WSL, Ghidra pulls in Java JDK 21 when needed.
- Clear install log for every operation.
- **Automatic updates**: The app checks for newer versions on startup and updates itself silently from GitHub releases.

## Included Packages (v1)

| Category | Apps |
| --- | --- |
| Essentials | WinRAR or 7-Zip |
| Development | WSL + Ubuntu, .NET SDK, VS Code, Python, Git, Docker Desktop |
| Browsers | Brave, Google Chrome, DuckDuckGo Browser |
| Networking & Security | GNS3, Nmap, Wireshark, Proton VPN |
| Reverse Engineering | Ghidra (with Java JDK 21 auto-installed) |

WinRAR and 7-Zip are alternatives — picking one deselects the other. Proton VPN requires signing in to your account after install.

## Requirements

- Windows 10 or Windows 11.
- An internet connection.
- Administrator approval via the standard Windows UAC prompt (required for WSL installation).
- `winget` / **App Installer** — included by default on most Windows 11 systems. If missing, the app opens its Microsoft Store page automatically.

These are Windows system requirements, not something you need to install manually before running the app.

## Building From Source

Build once, then distribute the generated `NetSecSetup.exe` — no need for the .NET SDK or NuGet packages, since it targets the .NET Framework already included in Windows 10/11.

See [BUILD.md](BUILD.md) for details. In short: double-click `Build-PortableExe.cmd`, and the compiled app will appear at:

```
publish\NetSecSetup.exe
```

## Notes

- After installing WSL or Docker Desktop, a restart may be required, followed by opening Ubuntu once to set a Linux username and password.
- Docker Desktop requires virtualization enabled in BIOS/UEFI on some machines.
- GNS3 installs the core application only — device images, the GNS3 VM, and Cisco Packet Tracer are not part of the automated install since they require separate downloads or licenses.
- Package options and UI logic live in [Program.cs](src/NetSecSetupClassic/Program.cs).

## License

MIT License — see [LICENSE](LICENSE).
