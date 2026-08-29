using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetSecSetup
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Check for updates before launching the UI
            if (CheckAndApplyUpdates())
            {
                return; // Application was updated and restarted
            }
            
            Application.Run(new SetupForm());
        }

        private static bool CheckAndApplyUpdates()
        {
            try
            {
                var currentVersion = GetLocalVersion();
                var latestVersion = GetLatestVersionFromGitHub();
                
                if (latestVersion == null || currentVersion >= latestVersion)
                {
                    return false; // No update needed
                }

                // Update is available
                var localExePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var downloadUrl = GetDownloadUrlFromGitHub(latestVersion);
                
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    return false;
                }

                var backupPath = localExePath + ".backup";
                var tempPath = Path.Combine(Path.GetTempPath(), "NetSecSetup_Update.exe");

                // Download the new version
                using (var client = new WebClient())
                {
                    client.DownloadFile(downloadUrl, tempPath);
                }

                if (!File.Exists(tempPath) || new FileInfo(tempPath).Length == 0)
                {
                    return false; // Download failed
                }

                // Backup current version and replace
                if (File.Exists(localExePath))
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                    File.Move(localExePath, backupPath);
                }

                File.Move(tempPath, localExePath);

                // Restart the application
                Process.Start(localExePath);
                return true;
            }
            catch
            {
                // Silent fail - allow app to run with current version
                return false;
            }
        }

        private static Version GetLocalVersion()
        {
            try
            {
                var versionFile = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    ".version");
                
                if (File.Exists(versionFile))
                {
                    var versionString = File.ReadAllText(versionFile).Trim();
                    return new Version(versionString);
                }

                // Fallback: read from assembly
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                return assembly.GetName().Version ?? new Version("1.0.0.0");
            }
            catch
            {
                return new Version("1.0.0.0");
            }
        }

        private static Version GetLatestVersionFromGitHub()
        {
            try
            {
                var url = "https://api.github.com/repos/GLORY-2016/NetSec-Forge/releases/latest";
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "NetSecSetup");
                    var json = client.DownloadString(url);
                    
                    // Extract version from tag_name (e.g., "v1.0.1" -> "1.0.1")
                    var match = System.Text.RegularExpressions.Regex.Match(json, "\"tag_name\":\"v?([0-9.]+)\"");
                    if (match.Success)
                    {
                        return new Version(match.Groups[1].Value);
                    }
                }
            }
            catch
            {
                // Silent fail - no internet or API error
            }
            return null;
        }

        private static string GetDownloadUrlFromGitHub(Version version)
        {
            try
            {
                var url = "https://api.github.com/repos/GLORY-2016/NetSec-Forge/releases/latest";
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "NetSecSetup");
                    var json = client.DownloadString(url);
                    
                    // Extract download URL for NetSecSetup.exe
                    var match = System.Text.RegularExpressions.Regex.Match(
                        json, 
                        "\"browser_download_url\":\"([^\"]*NetSecSetup\\.exe)\"");
                    
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            catch
            {
                // Silent fail
            }
            return null;
        }
    }

    internal sealed class SetupForm : Form
    {
        private readonly List<PackageEntry> packages = new List<PackageEntry>
        {
            new PackageEntry("WinRAR", "RARLab.WinRAR", "Archive manager (commercial, trial available).", "Essentials", InstallerKind.Winget, false),
            new PackageEntry("7-Zip", "7zip.7zip", "Free, open-source archive manager.", "Essentials", InstallerKind.Winget, true),
            new PackageEntry("WSL + Ubuntu", "Ubuntu", "Linux environment for Windows.", "Development", InstallerKind.Wsl, true),
            new PackageEntry(".NET SDK", "Microsoft.DotNet.SDK.10", ".NET 10 SDK (LTS).", "Development", InstallerKind.Winget, true),
            new PackageEntry("Visual Studio Code", "Microsoft.VisualStudioCode", "Code editor.", "Development", InstallerKind.Winget, true),
            new PackageEntry("Python", "Python.Python.3.13", "Python programming language.", "Development", InstallerKind.Winget, true),
            new PackageEntry("Git", "Git.Git", "Version-control system.", "Development", InstallerKind.Winget, true),
            new PackageEntry("Docker Desktop", "Docker.DockerDesktop", "Containers and local development environments.", "Development", InstallerKind.Winget, true),
            new PackageEntry("Brave", "Brave.Brave", "Privacy-focused browser.", "Browsers", InstallerKind.Winget, true),
            new PackageEntry("Google Chrome", "Google.Chrome", "Google web browser.", "Browsers", InstallerKind.Winget, true),
            new PackageEntry("DuckDuckGo Browser", "DuckDuckGo.DesktopBrowser", "Privacy-focused web browser.", "Browsers", InstallerKind.Winget, true),
            new PackageEntry("GNS3", "GNS3.GNS3", "Network emulation platform.", "Networking & Security", InstallerKind.Winget, true),
            new PackageEntry("Nmap", "Insecure.Nmap", "Network discovery and port scanner.", "Networking & Security", InstallerKind.Winget, true),
            new PackageEntry("Wireshark", "WiresharkFoundation.Wireshark", "Network traffic analyzer.", "Networking & Security", InstallerKind.Winget, true),
            new PackageEntry("Proton VPN", "Proton.ProtonVPN", "Privacy VPN client; sign-in is required after installation.", "Networking & Security", InstallerKind.Winget, true),
            new PackageEntry("Java JDK 21", "EclipseAdoptium.Temurin.21.JDK", "Required by Ghidra; added automatically when needed.", "Reverse Engineering", InstallerKind.Winget, false),
            new PackageEntry("Ghidra", "Ghidra", "Reverse-engineering framework.", "Reverse Engineering", InstallerKind.Winget, true, true)
        };

        private readonly Dictionary<PackageEntry, CheckBox> selection = new Dictionary<PackageEntry, CheckBox>();
        private readonly List<Label> categoryLabels = new List<Label>();
        private readonly Label title = new Label { Text = "NetSec Windows Setup", Font = new Font("Segoe UI Semibold", 20), AutoSize = true, Margin = new Padding(0, 0, 0, 3) };
        private readonly Label subtitle = new Label { Text = "Select the tools you need, then let Windows install them for you.", AutoSize = true, ForeColor = Color.DimGray };
        private readonly Button installButton = new Button { Text = "Install selected apps", AutoSize = true, Padding = new Padding(14, 7, 14, 7) };
        private readonly Button selectRecommendedButton = new Button { Text = "Select recommended", AutoSize = true, Padding = new Padding(8, 5, 8, 5) };
        private readonly Button themeButton = new Button { AutoSize = true, Padding = new Padding(8, 5, 8, 5) };
        private readonly RichTextBox log = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9) };
        private readonly Label status = new Label { AutoSize = true, Text = "Ready to install your selected tools." };
        private readonly Label logLabel = new Label { Text = "Installation log", AutoSize = true, Font = new Font("Segoe UI Semibold", 10), Margin = new Padding(0, 8, 0, 5) };
        private readonly FlowLayoutPanel selections = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Top, Padding = new Padding(12, 8, 12, 8) };
        private readonly Panel selectionPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(1) };
        private readonly TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26), ColumnCount = 1, RowCount = 5 };
        private bool darkMode = true;
        private bool installing;

        public SetupForm()
        {
            Text = "NetSec Windows Setup";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(780, 690);
            Size = new Size(900, 820);
            Font = new Font("Segoe UI", 10);

            var heading = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            heading.Controls.Add(title);
            heading.Controls.Add(subtitle);

            themeButton.Click += delegate
            {
                darkMode = !darkMode;
                ApplyTheme();
            };
            var header = new TableLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 12)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.Controls.Add(heading, 0, 0);
            header.Controls.Add(themeButton, 1, 0);

            BuildPackageList();
            selectionPanel.Controls.Add(selections);

            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 8, 0, 2)
            };
            installButton.Click += async delegate { await InstallSelectedAsync(); };
            selectRecommendedButton.Click += delegate { SelectRecommended(); };
            buttons.Controls.Add(installButton);
            buttons.Controls.Add(selectRecommendedButton);

            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            shell.Controls.Add(header, 0, 0);
            shell.Controls.Add(selectionPanel, 0, 1);
            shell.Controls.Add(buttons, 0, 2);
            shell.Controls.Add(status, 0, 3);
            var logContainer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
            logContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            logContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            logContainer.Controls.Add(logLabel, 0, 0);
            logContainer.Controls.Add(log, 0, 1);
            shell.Controls.Add(logContainer, 0, 4);
            Controls.Add(shell);

            ApplyTheme();
            AppendLog("Ready. Docker Desktop automatically includes WSL, and Ghidra automatically includes Java JDK 21.");
        }

        private void BuildPackageList()
        {
            foreach (var category in packages.Select(item => item.Category).Distinct())
            {
                var categoryLabel = new Label
                {
                    Text = category.ToUpperInvariant(),
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 9),
                    Margin = new Padding(0, categoryLabels.Count == 0 ? 0 : 11, 0, 2)
                };
                categoryLabels.Add(categoryLabel);
                selections.Controls.Add(categoryLabel);

                foreach (var item in packages.Where(package => package.Category == category))
                {
                    var checkBox = new CheckBox
                    {
                        Text = item.Name + "  —  " + item.Description,
                        Checked = item.SelectedByDefault,
                        AutoSize = true,
                        Margin = new Padding(4, 3, 4, 3)
                    };
                    var capturedItem = item;
                    checkBox.CheckedChanged += delegate { EnforceCompressionChoice(capturedItem, checkBox); };
                    selection[item] = checkBox;
                    selections.Controls.Add(checkBox);
                }
            }
        }

        private void EnforceCompressionChoice(PackageEntry changedItem, CheckBox changedCheckBox)
        {
            if (!changedCheckBox.Checked || (changedItem.Name != "WinRAR" && changedItem.Name != "7-Zip"))
            {
                return;
            }

            foreach (var item in packages)
            {
                if (item != changedItem && (item.Name == "WinRAR" || item.Name == "7-Zip"))
                {
                    selection[item].Checked = false;
                }
            }
        }

        private void SelectRecommended()
        {
            foreach (var item in packages)
            {
                selection[item].Checked = item.SelectedByDefault;
            }
        }

        private List<PackageEntry> GetSelectedPackagesWithDependencies()
        {
            var names = new HashSet<string>(packages.Where(item => selection[item].Checked).Select(item => item.Name));
            if (names.Contains("Docker Desktop"))
            {
                names.Add("WSL + Ubuntu");
            }
            if (names.Contains("Ghidra"))
            {
                names.Add("Java JDK 21");
            }
            return packages.Where(item => names.Contains(item.Name)).ToList();
        }

        private async Task InstallSelectedAsync()
        {
            if (installing)
            {
                return;
            }

            var selected = GetSelectedPackagesWithDependencies();
            if (selected.Count == 0)
            {
                MessageBox.Show("Select at least one app first.", "Nothing selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("The application will install " + selected.Count + " item(s). Continue?", "Confirm installation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            installing = true;
            ToggleControls(false);
            log.Clear();
            try
            {
                var automaticDependencies = selected.Where(item => !selection[item].Checked).ToList();
                if (automaticDependencies.Count > 0)
                {
                    AppendLog("Automatic dependencies: " + string.Join(", ", automaticDependencies.Select(item => item.Name)) + ".");
                }

                if (selected.Any(item => item.Kind == InstallerKind.Winget) && !IsWingetAvailable())
                {
                    const string message = "Windows Package Manager (winget) is missing. Open App Installer in Microsoft Store, install it, then start this application again.";
                    AppendLog(message);
                    if (MessageBox.Show(message, "App Installer required", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo("ms-windows-store://pdp/?ProductId=9NBLGGH4NNS1") { UseShellExecute = true });
                    }
                    return;
                }

                foreach (var item in selected)
                {
                    status.Text = "Installing " + item.Name + "...";
                    AppendLog("Installing " + item.Name + "...");
                    int exitCode;
                    if (item.Kind == InstallerKind.Wsl)
                    {
                        exitCode = await RunProcessAsync("wsl.exe", "--install -d Ubuntu");
                    }
                    else
                    {
                        exitCode = await RunProcessAsync("winget.exe", BuildWingetArguments(item));
                    }

                    AppendLog(exitCode == 0
                        ? "✓ " + item.Name + " completed."
                        : "✗ " + item.Name + " ended with code " + exitCode + ". You can run this application again to retry.");
                }

                status.Text = "Finished. Restart Windows if WSL or Docker Desktop was selected.";
                AppendLog("Finished. Restart Windows if WSL or Docker Desktop was selected.");
            }
            catch (Exception exception)
            {
                status.Text = "Installation stopped unexpectedly.";
                AppendLog("Error: " + exception.Message);
                MessageBox.Show("The setup encountered an error. See the installation log for details.", "Setup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                installing = false;
                ToggleControls(true);
            }
        }

        private static string BuildWingetArguments(PackageEntry item)
        {
            var target = item.UseNameLookup
                ? "--name \"" + item.Id + "\""
                : "--id " + item.Id;
            return "install " + target + " --exact --source winget --silent --accept-package-agreements --accept-source-agreements";
        }

        private static bool IsWingetAvailable()
        {
            var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "winget.exe");
            return File.Exists(localPath) || FindOnPath("winget.exe") != null;
        }

        private static string FindOnPath(string executable)
        {
            var pathEntries = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var path in pathEntries)
            {
                var candidate = Path.Combine(path.Trim(), executable);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            return null;
        }

        private async Task<int> RunProcessAsync(string executable, string arguments)
        {
            var info = new ProcessStartInfo(executable, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (var process = new Process { StartInfo = info })
            {
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs eventArgs)
                {
                    if (!string.IsNullOrWhiteSpace(eventArgs.Data))
                    {
                        AppendLog(eventArgs.Data);
                    }
                };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs eventArgs)
                {
                    if (!string.IsNullOrWhiteSpace(eventArgs.Data))
                    {
                        AppendLog(eventArgs.Data);
                    }
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await Task.Factory.StartNew(process.WaitForExit);
                return process.ExitCode;
            }
        }

        private void ToggleControls(bool enabled)
        {
            installButton.Enabled = enabled;
            selectRecommendedButton.Enabled = enabled;
            themeButton.Enabled = enabled;
            foreach (var checkBox in selection.Values)
            {
                checkBox.Enabled = enabled;
            }
        }

        private void ApplyTheme()
        {
            var background = darkMode ? Color.FromArgb(15, 23, 42) : Color.FromArgb(246, 248, 252);
            var surface = darkMode ? Color.FromArgb(30, 41, 59) : Color.White;
            var primaryText = darkMode ? Color.FromArgb(241, 245, 249) : Color.FromArgb(15, 23, 42);
            var secondaryText = darkMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(71, 85, 105);
            var accent = darkMode ? Color.FromArgb(56, 189, 248) : Color.FromArgb(2, 132, 199);
            var subtleButton = darkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(226, 232, 240);

            BackColor = background;
            shell.BackColor = background;
            selectionPanel.BackColor = surface;
            selections.BackColor = surface;
            title.ForeColor = primaryText;
            subtitle.ForeColor = secondaryText;
            status.ForeColor = secondaryText;
            logLabel.ForeColor = primaryText;
            log.BackColor = darkMode ? Color.FromArgb(10, 16, 29) : Color.FromArgb(235, 240, 246);
            log.ForeColor = primaryText;

            foreach (var categoryLabel in categoryLabels)
            {
                categoryLabel.ForeColor = accent;
                categoryLabel.BackColor = surface;
            }
            foreach (var checkBox in selection.Values)
            {
                checkBox.ForeColor = primaryText;
                checkBox.BackColor = surface;
            }

            StyleButton(installButton, accent, Color.White);
            StyleButton(selectRecommendedButton, subtleButton, primaryText);
            StyleButton(themeButton, subtleButton, primaryText);
            themeButton.Text = darkMode ? "Light mode" : "Dark mode";
        }

        private static void StyleButton(Button button, Color background, Color foreground)
        {
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = background;
            button.ForeColor = foreground;
        }

        private void AppendLog(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), message);
                return;
            }

            log.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
            log.SelectionStart = log.TextLength;
            log.ScrollToCaret();
        }
    }

    internal enum InstallerKind
    {
        Winget,
        Wsl
    }

    internal sealed class PackageEntry
    {
        public PackageEntry(string name, string id, string description, string category, InstallerKind kind, bool selectedByDefault, bool useNameLookup = false)
        {
            Name = name;
            Id = id;
            Description = description;
            Category = category;
            Kind = kind;
            SelectedByDefault = selectedByDefault;
            UseNameLookup = useNameLookup;
        }

        public string Name { get; private set; }
        public string Id { get; private set; }
        public string Description { get; private set; }
        public string Category { get; private set; }
        public InstallerKind Kind { get; private set; }
        public bool SelectedByDefault { get; private set; }
        public bool UseNameLookup { get; private set; }
    }
}
