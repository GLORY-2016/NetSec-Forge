using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace NetSecSetup
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 && (args[0] == "--install" || args[0] == "-i"))
            {
                RunSilentInstall(args);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }

        private static void RunSilentInstall(string[] args)
        {
            var packagesToInstall = new List<string>();
            bool autoYes = false;

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--yes" || args[i] == "-y")
                {
                    autoYes = true;
                }
                else if (!args[i].StartsWith("-"))
                {
                    packagesToInstall.Add(args[i]);
                }
            }

            if (packagesToInstall.Count == 0)
            {
                Console.WriteLine("Usage: NetSecSetup.exe --install <package1> [package2...] [--yes]");
                Console.WriteLine("Example: NetSecSetup.exe --install \"WSL + Ubuntu\" \"Visual Studio Code\" \"Docker Desktop\" --yes");
                return;
            }

            Console.WriteLine("NetSec Windows Setup - Silent Mode");
            Console.WriteLine("Packages to install: " + string.Join(", ", packagesToInstall));

            if (!autoYes)
            {
                Console.Write("Continue? (y/N): ");
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input) || !input.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }
            }

            var form = new SetupForm();
            form.RunSilentInstall(packagesToInstall).Wait();
        }
    }

    internal sealed class SetupForm : Form
    {
        private readonly List<PackageEntry> packages = new List<PackageEntry>();
        private readonly Dictionary<string, List<string>> dependencies = new Dictionary<string, List<string>>();

        private void LoadPackagesFromJson()
        {
            try
            {
                var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "packages.json");
                if (File.Exists(jsonPath))
                {
                    var json = File.ReadAllText(jsonPath);
                    var serializer = new JavaScriptSerializer();
                    var data = serializer.Deserialize<Dictionary<string, object>>(json);

                    var packagesList = (List<object>)data["Packages"];
                    foreach (var pkgObj in packagesList)
                    {
                        var pkg = (Dictionary<string, object>)pkgObj;
                        var kind = (string)pkg["InstallerKind"];
                        packages.Add(new PackageEntry(
                            (string)pkg["Name"],
                            (string)pkg["Id"],
                            (string)pkg["Description"],
                            (string)pkg["Category"],
                            kind == "Wsl" ? InstallerKind.Wsl : InstallerKind.Winget,
                            Convert.ToBoolean(pkg["SelectedByDefault"]),
                            Convert.ToBoolean(pkg["UseNameLookup"])
                        ));
                    }

                    var depsObj = (Dictionary<string, object>)data["Dependencies"];
                    foreach (var kvp in depsObj)
                    {
                        dependencies[kvp.Key] = ((List<object>)kvp.Value).ConvertAll(o => (string)o);
                    }
                }
                else
                {
                    LoadDefaultPackages();
                }
            }
            catch
            {
                LoadDefaultPackages();
            }
        }

        private void LoadDefaultPackages()
        {
            packages.AddRange(new[]
            {
                new PackageEntry("WinRAR", "RARLab.WinRAR", "Archive manager (commercial, trial available).", "Essentials", InstallerKind.Winget, true),
                new PackageEntry("7-Zip", "7zip.7zip", "Free, open-source archive manager.", "Essentials", InstallerKind.Winget, false),
                new PackageEntry("Python", "Python.Python.3.13", "Python programming language.", "Development", InstallerKind.Winget, true),
                new PackageEntry("Git", "Git.Git", "Version-control system.", "Development", InstallerKind.Winget, true),
                new PackageEntry(".NET SDK", "Microsoft.DotNet.SDK.10", ".NET 10 SDK (LTS).", "Development", InstallerKind.Winget, true),
                new PackageEntry("Visual Studio Code", "Microsoft.VisualStudioCode", "Lightweight code editor.", "Development", InstallerKind.Winget, false),
                new PackageEntry("Visual Studio", "Microsoft.VisualStudio.2022.Community", "Full-featured IDE for .NET and C++ development (Community edition).", "Development", InstallerKind.Winget, true),
                new PackageEntry("Docker Desktop", "Docker.DockerDesktop", "Containers and local development environments.", "Development", InstallerKind.Winget, false),
                new PackageEntry("WSL + Ubuntu", "Ubuntu", "Linux environment for Windows.", "Development", InstallerKind.Wsl, false),
                new PackageEntry("Google Chrome", "Google.Chrome", "Google web browser.", "Web Browsers", InstallerKind.Winget, false),
                new PackageEntry("Brave", "Brave.Brave", "Privacy-focused browser.", "Web Browsers", InstallerKind.Winget, false),
                new PackageEntry("Mozilla Firefox", "Mozilla.Firefox", "Open-source privacy-focused browser.", "Web Browsers", InstallerKind.Winget, false),
                new PackageEntry("DuckDuckGo Browser", "DuckDuckGo.DesktopBrowser", "Privacy-focused web browser.", "Web Browsers", InstallerKind.Winget, false),
                new PackageEntry("Wireshark", "WiresharkFoundation.Wireshark", "Network traffic analyzer.", "Networking & Security", InstallerKind.Winget, false),
                new PackageEntry("Nmap", "Insecure.Nmap", "Network discovery and port scanner.", "Networking & Security", InstallerKind.Winget, false),
                new PackageEntry("GNS3", "GNS3.GNS3", "Network emulation platform.", "Networking & Security", InstallerKind.Winget, false),
                new PackageEntry("Proton VPN", "Proton.ProtonVPN", "Privacy VPN client; sign-in required after installation.", "Networking & Security", InstallerKind.Winget, false),
                new PackageEntry("Maltego CE", "MaltegoTechnologies.MaltegoCE", "Open-source intelligence and graph analysis.", "Networking & Security", InstallerKind.Winget, false),
                new PackageEntry("Win11Debloat", "Win11Debloat", "Remove Windows bloatware, telemetry, and unwanted apps. (Recommended for advanced users)", "System Utilities", InstallerKind.GitHub, false, false, "https://github.com/raphire/Win11Debloat"),
                new PackageEntry("Java JDK 21", "EclipseAdoptium.Temurin.21.JDK", "Required by Ghidra; added automatically when needed.", "Reverse Engineering", InstallerKind.Winget, false),
                new PackageEntry("Ghidra", "Ghidra", "Reverse-engineering framework.", "Reverse Engineering", InstallerKind.Winget, false, true)
            });

            dependencies["Docker Desktop"] = new List<string> { "WSL + Ubuntu" };
            dependencies["Ghidra"] = new List<string> { "Java JDK 21" };
        }

        private readonly Dictionary<PackageEntry, CheckBox> selection = new Dictionary<PackageEntry, CheckBox>();
        private readonly Dictionary<PackageEntry, ProgressBar> progressBars = new Dictionary<PackageEntry, ProgressBar>();
        private readonly List<Label> categoryLabels = new List<Label>();
        private readonly Label title = new Label { Text = "NetSec Windows Setup", Font = new Font("Segoe UI Semibold", 20), AutoSize = true, Margin = new Padding(0, 0, 0, 3) };
        private readonly Label subtitle = new Label { Text = "Select the tools you need, then let Windows install them for you.", AutoSize = true, ForeColor = Color.DimGray };
        private readonly Button installButton = new Button { Text = "Install selected apps", AutoSize = true, Padding = new Padding(14, 7, 14, 7) };
        private readonly Button selectRecommendedButton = new Button { Text = "Select recommended", AutoSize = true, Padding = new Padding(8, 5, 8, 5) };
        private readonly Button selectAllButton = new Button { Text = "Select all", AutoSize = true, Padding = new Padding(8, 5, 8, 5) };
        private readonly Button deselectAllButton = new Button { Text = "Deselect all", AutoSize = true, Padding = new Padding(8, 5, 8, 5) };
        private readonly Button themeButton = new Button { AutoSize = true, Padding = new Padding(8, 5, 8, 5) };
        private readonly RichTextBox log = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9) };
        private readonly Label status = new Label { AutoSize = true, Text = "Ready to install your selected tools." };
        private readonly Label logLabel = new Label { Text = "Installation log", AutoSize = true, Font = new Font("Segoe UI Semibold", 10), Margin = new Padding(0, 8, 0, 5) };
        private readonly FlowLayoutPanel selections = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Top, Padding = new Padding(12, 8, 12, 8) };
        private readonly Panel selectionPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8) };
        private readonly Panel logPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        private readonly TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26), ColumnCount = 1, RowCount = 5 };
        private readonly PictureBox logoBox = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, Width = 48, Height = 48, Margin = new Padding(0, 0, 12, 0) };
        private Color cardBorderColor = Color.FromArgb(51, 65, 85);
        private bool darkMode = true;
        private bool installing;

        public SetupForm()
        {
            Text = "NetSec Windows Setup";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(780, 690);
            Size = new Size(900, 820);
            Font = new Font("Segoe UI", 10);

            LoadPackagesFromJson();

            var heading = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };

            // Load logo
            try
            {
                var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
                if (File.Exists(logoPath))
                {
                    using (var original = Image.FromFile(logoPath))
                    {
                        var resized = new Bitmap(original, new Size(48, 48));
                        logoBox.Image = resized;
                    }
                }
            }
            catch { }

            heading.Controls.Add(logoBox);
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
            selectAllButton.Click += delegate { SelectAll(true); };
            deselectAllButton.Click += delegate { SelectAll(false); };
            buttons.Controls.Add(installButton);
            buttons.Controls.Add(selectRecommendedButton);
            buttons.Controls.Add(selectAllButton);
            buttons.Controls.Add(deselectAllButton);

            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            shell.Controls.Add(header, 0, 0);
            shell.Controls.Add(selectionPanel, 0, 1);
            shell.Controls.Add(buttons, 0, 2);
            shell.Controls.Add(status, 0, 3);
            logPanel.Controls.Add(log);
            var logContainer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
            logContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            logContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            logContainer.Controls.Add(logLabel, 0, 0);
            logContainer.Controls.Add(logPanel, 0, 1);
            shell.Controls.Add(logContainer, 0, 4);
            Controls.Add(shell);

            // Soft rounded "card" borders around the tool list and the log, matching
            // the pill-shaped buttons for a slightly more polished, cohesive look.
            selectionPanel.Paint += DrawCardBorder;
            logPanel.Paint += DrawCardBorder;

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
                    var panel = new FlowLayoutPanel
                    {
                        AutoSize = true,
                        FlowDirection = FlowDirection.LeftToRight,
                        WrapContents = false,
                        Margin = new Padding(4, 3, 4, 3)
                    };

                    var checkBox = new CheckBox
                    {
                        Text = item.Name + "  —  " + item.Description,
                        Checked = item.SelectedByDefault,
                        AutoSize = true,
                        Margin = new Padding(0, 3, 8, 3)
                    };
                    var capturedItem = item;
                    checkBox.CheckedChanged += delegate { EnforceCompressionChoice(capturedItem, checkBox); };
                    selection[item] = checkBox;

                    var progressBar = new ProgressBar
                    {
                        Width = 200,
                        Height = 18,
                        Style = ProgressBarStyle.Marquee,
                        Visible = false,
                        Margin = new Padding(0, 2, 0, 2)
                    };
                    progressBars[item] = progressBar;

                    panel.Controls.Add(checkBox);
                    panel.Controls.Add(progressBar);
                    selections.Controls.Add(panel);
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

        private void SelectAll(bool check)
        {
            foreach (var item in packages)
            {
                selection[item].Checked = check;
            }
        }

        private List<PackageEntry> GetSelectedPackagesWithDependencies()
        {
            var names = new HashSet<string>(packages.Where(item => selection[item].Checked).Select(item => item.Name));

            var added = true;
            while (added)
            {
                added = false;
                foreach (var kvp in dependencies)
                {
                    if (names.Contains(kvp.Key))
                    {
                        foreach (var dep in kvp.Value)
                        {
                            if (!names.Contains(dep))
                            {
                                names.Add(dep);
                                added = true;
                            }
                        }
                    }
                }
            }

            return packages.Where(item => names.Contains(item.Name)).ToList();
        }

        internal async Task RunSilentInstall(List<string> packageNames)
        {
            var selected = packages.Where(p => packageNames.Contains(p.Name)).ToList();
            if (selected.Count == 0)
            {
                Console.WriteLine("No matching packages found.");
                return;
            }

            var names = new HashSet<string>(packageNames);

            var added = true;
            while (added)
            {
                added = false;
                foreach (var kvp in dependencies)
                {
                    if (names.Contains(kvp.Key))
                    {
                        foreach (var dep in kvp.Value)
                        {
                            if (!names.Contains(dep))
                            {
                                names.Add(dep);
                                Console.WriteLine("Auto-adding dependency: " + dep);
                                added = true;
                            }
                        }
                    }
                }
            }

            var withDeps = packages.Where(p => names.Contains(p.Name)).ToList();

            if (withDeps.Any(item => item.Kind == InstallerKind.Winget) && !IsWingetAvailable())
            {
                Console.WriteLine("ERROR: winget not found. Install App Installer from Microsoft Store first.");
                return;
            }

            if (withDeps.Any(item => item.Kind == InstallerKind.Winget))
            {
                Console.WriteLine("Updating winget sources...");
                await RunProcessAsync("winget.exe", "source update");
            }

            foreach (var item in withDeps)
            {
                Console.Write("Installing " + item.Name + "... ");
                int exitCode;
                if (item.Kind == InstallerKind.Wsl)
                {
                    exitCode = await RunProcessAsync("wsl.exe", "--install -d Ubuntu");
                }
                else if (item.Kind == InstallerKind.GitHub)
                {
                    exitCode = await InstallFromGitHubAsync(item);
                }
                else
                {
                    exitCode = await RunProcessAsync("winget.exe", BuildWingetArguments(item));
                }

                Console.WriteLine(exitCode == 0 ? "OK" : "FAILED (code " + exitCode + ")");
            }

            Console.WriteLine("Done. Restart if WSL or Docker Desktop was installed.");
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

                if (selected.Any(item => item.Kind == InstallerKind.Winget))
                {
                    status.Text = "Updating winget sources...";
                    AppendLog("Updating winget sources...");
                    await RunProcessAsync("winget.exe", "source update");
                }

                var failedPackages = new List<PackageEntry>();

                foreach (var item in selected)
                {
                    status.Text = "Installing " + item.Name + "...";
                    AppendLog("Installing " + item.Name + "...");

                    ProgressBar pb;
                    if (progressBars.TryGetValue(item, out pb))
                    {
                        pb.Visible = true;
                        pb.Style = ProgressBarStyle.Marquee;
                    }

                    int exitCode;
                    if (item.Kind == InstallerKind.Wsl)
                    {
                        exitCode = await RunProcessAsync("wsl.exe", "--install -d Ubuntu");
                    }
                    else if (item.Kind == InstallerKind.GitHub)
                    {
                        exitCode = await InstallFromGitHubAsync(item);
                    }
                    else
                    {
                        exitCode = await RunProcessAsync("winget.exe", BuildWingetArguments(item));
                    }

                    if (progressBars.TryGetValue(item, out pb))
                    {
                        pb.Visible = false;
                        pb.Style = ProgressBarStyle.Blocks;
                        pb.Value = exitCode == 0 ? 100 : 0;
                    }

                    if (exitCode == 0)
                    {
                        AppendLog("✓ " + item.Name + " completed.");
                    }
                    else
                    {
                        AppendLog("✗ " + item.Name + " ended with code " + exitCode + ".");
                        failedPackages.Add(item);
                    }
                }

                if (failedPackages.Count > 0)
                {
                    var retry = MessageBox.Show(
                        failedPackages.Count + " package(s) failed. Retry failed installations?",
                        "Installation complete with errors",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (retry == DialogResult.Yes)
                    {
                        foreach (var item in failedPackages)
                        {
                            status.Text = "Retrying " + item.Name + "...";
                            AppendLog("Retrying " + item.Name + "...");

                            ProgressBar pb;
                    if (progressBars.TryGetValue(item, out pb))
                            {
                                pb.Visible = true;
                                pb.Style = ProgressBarStyle.Marquee;
                            }

                            int exitCode;
                            if (item.Kind == InstallerKind.Wsl)
                            {
                                exitCode = await RunProcessAsync("wsl.exe", "--install -d Ubuntu");
                            }
                            else if (item.Kind == InstallerKind.GitHub)
                            {
                                exitCode = await InstallFromGitHubAsync(item);
                            }
                            else
                            {
                                exitCode = await RunProcessAsync("winget.exe", BuildWingetArguments(item));
                            }

                            if (progressBars.TryGetValue(item, out pb))
                            {
                                pb.Visible = false;
                                pb.Style = ProgressBarStyle.Blocks;
                                pb.Value = exitCode == 0 ? 100 : 0;
                            }

                            AppendLog(exitCode == 0
                                ? "✓ " + item.Name + " completed on retry."
                                : "✗ " + item.Name + " failed again with code " + exitCode + ".");
                        }
                    }
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

        private async Task<int> InstallFromGitHubAsync(PackageEntry item)
        {
            if (string.IsNullOrEmpty(item.GitHubUrl))
            {
                AppendLog("No GitHub URL specified for " + item.Name);
                return -1;
            }

            try
            {
                // Enable TLS 1.2 for GitHub API (required for HTTPS)
                System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;

                AppendLog("Fetching latest release from GitHub...");

                string response;
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "NetSecSetup");
                    var apiUrl = item.GitHubUrl.Replace("https://github.com/", "https://api.github.com/repos/") + "/releases/latest";
                    response = client.DownloadString(apiUrl);
                }

                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                var release = serializer.Deserialize<Dictionary<string, object>>(response);

                // Some GitHub-hosted tools (e.g. Win11Debloat) don't publish a compiled
                // .exe/.msi/.zip installer — they publish the raw PowerShell script itself
                // as the release asset. Recognize those too instead of failing outright.
                var assets = (ArrayList)release["assets"];
                string downloadUrl = null;
                string assetName = null;
                foreach (var extension in new[] { ".exe", ".msi", ".zip", ".ps1" })
                {
                    foreach (var assetObj in assets)
                    {
                        var asset = (Dictionary<string, object>)assetObj;
                        var name = (string)asset["name"];
                        if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = (string)asset["browser_download_url"];
                            assetName = name;
                            break;
                        }
                    }
                    if (downloadUrl != null)
                    {
                        break;
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    AppendLog("No suitable installer asset found for " + item.Name);
                    return -1;
                }

                AppendLog("Downloading " + item.Name + "...");
                var extensionUsed = Path.GetExtension(assetName);
                var tempPath = Path.Combine(Path.GetTempPath(), "NetSecSetup_" + item.Name.Replace(" ", "_") + extensionUsed);

                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "NetSecSetup");
                    client.DownloadFile(downloadUrl, tempPath);
                }

                AppendLog("Installing " + item.Name + "...");

                if (extensionUsed.Equals(".ps1", StringComparison.OrdinalIgnoreCase))
                {
                    // Scripts like Win11Debloat present their own interactive menu and
                    // request their own elevation, so launch them in a visible window
                    // rather than trying to run them silently like a binary installer.
                    var scriptArguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + tempPath + "\"";
                    return await RunInteractiveProcessAsync("powershell.exe", scriptArguments);
                }

                if (extensionUsed.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    var extractPath = Path.Combine(Path.GetTempPath(), "NetSecSetup_" + item.Name.Replace(" ", "_"));
                    if (Directory.Exists(extractPath))
                    {
                        Directory.Delete(extractPath, true);
                    }
                    ZipFile.ExtractToDirectory(tempPath, extractPath);

                    var runBat = Directory.GetFiles(extractPath, "Run.bat", SearchOption.AllDirectories).FirstOrDefault();
                    if (runBat != null)
                    {
                        return await RunInteractiveProcessAsync(runBat, string.Empty);
                    }

                    var ps1 = Directory.GetFiles(extractPath, "*.ps1", SearchOption.AllDirectories).FirstOrDefault();
                    if (ps1 != null)
                    {
                        var scriptArguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + ps1 + "\"";
                        return await RunInteractiveProcessAsync("powershell.exe", scriptArguments);
                    }

                    AppendLog("No runnable script found inside the downloaded archive for " + item.Name);
                    return -1;
                }

                return await RunProcessAsync(tempPath, "/S");
            }
            catch (Exception ex)
            {
                AppendLog("GitHub install failed for " + item.Name + ": " + ex.Message);
                return -1;
            }
        }

        private async Task<int> RunInteractiveProcessAsync(string executable, string arguments)
        {
            // NetSecSetup itself already requires "requireAdministrator" (see app.manifest),
            // so a direct child process inherits that elevated token — no extra "runas"
            // verb needed here, which would otherwise risk a redundant UAC prompt.
            var info = new ProcessStartInfo(executable, arguments)
            {
                UseShellExecute = true
            };

            using (var process = new Process { StartInfo = info })
            {
                try
                {
                    process.Start();
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    AppendLog("Failed to launch " + executable + ": " + ex.Message);
                    return -1;
                }

                await Task.Factory.StartNew(process.WaitForExit);
                return process.ExitCode;
            }
        }

        private void ToggleControls(bool enabled)
        {
            installButton.Enabled = enabled;
            selectRecommendedButton.Enabled = enabled;
            selectAllButton.Enabled = enabled;
            deselectAllButton.Enabled = enabled;
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

            foreach (var pb in progressBars.Values)
            {
                pb.BackColor = darkMode ? Color.FromArgb(10, 16, 29) : Color.FromArgb(235, 240, 246);
                pb.ForeColor = accent;
            }

            StyleButton(installButton, accent, Color.White);
            StyleButton(selectRecommendedButton, subtleButton, primaryText);
            StyleButton(selectAllButton, subtleButton, primaryText);
            StyleButton(deselectAllButton, subtleButton, primaryText);
            StyleButton(themeButton, subtleButton, primaryText);
            themeButton.Text = darkMode ? "Light mode" : "Dark mode";

            logPanel.BackColor = log.BackColor;
            cardBorderColor = subtleButton;
            selectionPanel.Invalidate();
            logPanel.Invalidate();
        }

        private void DrawCardBorder(object sender, PaintEventArgs e)
        {
            var control = (Control)sender;
            if (control.Width <= 2 || control.Height <= 2)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, control.Width - 1, control.Height - 1);
            using (var path = RoundedRectangle(rect, 10))
            using (var pen = new Pen(cardBorderColor, 1))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        private static void StyleButton(Button button, Color background, Color foreground)
        {
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = background;
            button.ForeColor = foreground;
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.MouseOverBackColor = Lighten(background, 0.12f);
            button.FlatAppearance.MouseDownBackColor = Darken(background, 0.12f);

            // Give buttons a soft pill shape instead of Windows' default hard rectangle.
            ApplyRoundedRegion(button);
            button.SizeChanged -= ButtonOnSizeChanged;
            button.SizeChanged += ButtonOnSizeChanged;
        }

        private static void ButtonOnSizeChanged(object sender, EventArgs e)
        {
            ApplyRoundedRegion((Button)sender);
        }

        private static void ApplyRoundedRegion(Button button)
        {
            if (button.Width <= 0 || button.Height <= 0)
            {
                return;
            }

            const int radius = 8;
            var bounds = new Rectangle(0, 0, button.Width, button.Height);
            using (var path = RoundedRectangle(bounds, radius))
            {
                var oldRegion = button.Region;
                button.Region = new Region(path);
                if (oldRegion != null)
                {
                    oldRegion.Dispose();
                }
            }
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var path = new GraphicsPath();
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Color Lighten(Color color, float amount)
        {
            return Color.FromArgb(
                color.A,
                (int)Math.Min(255, color.R + (255 - color.R) * amount),
                (int)Math.Min(255, color.G + (255 - color.G) * amount),
                (int)Math.Min(255, color.B + (255 - color.B) * amount));
        }

        private static Color Darken(Color color, float amount)
        {
            return Color.FromArgb(
                color.A,
                (int)Math.Max(0, color.R * (1 - amount)),
                (int)Math.Max(0, color.G * (1 - amount)),
                (int)Math.Max(0, color.B * (1 - amount)));
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
        Wsl,
        GitHub
    }

    internal sealed class PackageEntry
    {
        public PackageEntry(string name, string id, string description, string category, InstallerKind kind, bool selectedByDefault, bool useNameLookup = false, string githubUrl = null)
        {
            Name = name;
            Id = id;
            Description = description;
            Category = category;
            Kind = kind;
            SelectedByDefault = selectedByDefault;
            UseNameLookup = useNameLookup;
            GitHubUrl = githubUrl;
        }

        public string Name { get; private set; }
        public string Id { get; private set; }
        public string Description { get; private set; }
        public string Category { get; private set; }
        public InstallerKind Kind { get; private set; }
        public bool SelectedByDefault { get; private set; }
        public bool UseNameLookup { get; private set; }
        public string GitHubUrl { get; private set; }
    }
}
