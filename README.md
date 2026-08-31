# NetSec Windows Setup

A standalone Windows application that automates the installation of essential development, networking, and security tools on a fresh Windows installation.

## Purpose

Moving to a new computer, formatting a machine, setting up a fresh Windows install, or provisioning a VM/VPS usually means manually downloading and installing dozens of tools one by one. This application eliminates that repetitive work — select the tools you need, click **Install**, and let Windows handle the rest.

The goal is to significantly improve productivity and reduce the time wasted on repetitive software setup.

## Features

- **Organized categories**: Essentials, Development, Web Browsers, Networking & Security, Reverse Engineering, System Utilities
- **21 tools** covering development, networking, security, and system maintenance
- **Smart dependencies**: Docker Desktop auto-installs WSL+Ubuntu; Ghidra auto-installs Java JDK 21
- **Default selection**: Python, Git, .NET SDK, Visual Studio Community, WinRAR (recommended baseline)
- **Per-package progress bars** during installation
- **Retry failed packages** with one click
- **Select All / Deselect All** buttons for quick selection
- **Dark/Light mode** with persistence
- **Silent/CLI mode** for automation: `NetSecSetup.exe --install "Python" "Git" --yes`
- **Configurable packages**: Edit `packages.json` to add/remove tools without recompiling

## Included Tools

| Category | Tools |
|----------|-------|
| **Essentials** | WinRAR (default), 7-Zip |
| **Development** | Python, Git, .NET SDK, Visual Studio Community (default), VS Code, Docker Desktop, WSL+Ubuntu |
| **Web Browsers** | Google Chrome, Brave, Mozilla Firefox, DuckDuckGo Browser |
| **Networking & Security** | Wireshark, Nmap, GNS3, Proton VPN, Maltego CE |
| **System Utilities** | Win11Debloat (removes bloatware/telemetry — recommended for advanced users) |
| **Reverse Engineering** | Ghidra (auto-installs Java JDK 21) |

## Requirements

- Windows 10 / 11 (64-bit)
- Administrator rights (UAC prompt)
- Internet connection
- `winget` / **App Installer** (built into Windows 11; auto-prompts to install if missing)

## Usage

### GUI Mode
1. Download `NetSecSetup.exe` from the [Releases](../../releases) page
2. Run as Administrator
3. Select desired tools (or click **Select recommended** for defaults)
4. Click **Install selected apps**

### Silent/CLI Mode
```cmd
# Install specific packages without prompts
NetSecSetup.exe --install "Python" "Git" ".NET SDK" "Visual Studio Community" "WinRAR" --yes

# Install with confirmation prompt
NetSecSetup.exe --install "Wireshark" "Nmap" "Ghidra"
```

### Customizing Packages
Edit `packages.json` (next to the EXE) to add, remove, or reorder tools. No rebuild needed.

## Building from Source

```cmd
Build-PortableExe.cmd
```
Output: `publish\NetSecSetup.exe` + `publish\packages.json`

Uses Windows' built-in .NET Framework compiler — no SDK, NuGet, or internet required for the build.

## License

MIT License — see [LICENSE](LICENSE).