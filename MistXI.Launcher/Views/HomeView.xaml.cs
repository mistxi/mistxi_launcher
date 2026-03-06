using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Diagnostics;
using MistXI.Launcher.Models;

namespace MistXI.Launcher.Views;

public partial class HomeView : UserControl
{
    private readonly AppServices _svc;
    private LauncherState _state;
    private const string DefaultServer = "play.mistxi.com";
    private CancellationTokenSource? _cts;
    private bool _hasCheckedAddons = false;

    public HomeView(AppServices services)
    {
        _svc = services;
        _state = _svc.StateStore.Load();
        InitializeComponent();

        // Populate profile dropdown
        LoadProfiles();

        if (!string.IsNullOrWhiteSpace(_state.SavedUser))
        {
            UserBox.Text = _state.SavedUser;
            var pass = _svc.CredentialStore.Unprotect(_state.SavedPassDpapiB64);
            if (!string.IsNullOrWhiteSpace(pass))
            {
                PassBox.Password = pass;
                SaveCredsCheck.IsChecked = true;
            }
        }
        
        // Restore server selection
        foreach (ComboBoxItem item in ServerCombo.Items)
        {
            if (item.Tag as string == _state.SelectedServer)
            {
                ServerCombo.SelectedItem = item;
                break;
            }
        }
        
        // Check and download Ashita/XiLoader on startup
        Loaded += HomeView_Loaded;
    }
    
    private async void HomeView_Loaded(object sender, RoutedEventArgs e)
    {
        // Check for launcher updates first
        try
        {
            var updateInfo = await _svc.Updater.CheckForUpdateAsync();
            if (updateInfo != null)
            {
                var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                var currentVersionStr = currentVersion != null ? $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}" : "1.1.0";
                
                var result = MessageBox.Show(
                    $"A new version of the MistXI Launcher is available!\n\n" +
                    $"Current Version: {currentVersionStr}\n" +
                    $"Latest Version: {updateInfo.Version}\n\n" +
                    $"Release Notes:\n{updateInfo.ReleaseNotes.Substring(0, Math.Min(200, updateInfo.ReleaseNotes.Length))}...\n\n" +
                    $"Would you like to update now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );
                
                if (result == MessageBoxResult.Yes)
                {
                    StatusLine.Text = "Status: Downloading launcher update...";
                    var success = await _svc.Updater.DownloadAndApplyUpdateAsync(
                        updateInfo.DownloadUrl, 
                        new Progress<string>(s => StatusLine.Text = $"Status: {s}")
                    );
                    
                    if (!success)
                    {
                        MessageBox.Show(
                            "Failed to apply update. Please download manually from GitHub.",
                            "Update Failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                    // If successful, launcher will restart automatically
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Failed to check for launcher updates", ex);
            // Non-fatal, continue loading
        }
        
        // Check for client version mismatch
        if (!string.IsNullOrWhiteSpace(_state.FfxiDir))
        {
            try
            {
                var mismatch = await _svc.Version.CheckVersionMismatchAsync(_state.FfxiDir);
                if (mismatch == true)
                {
                    var result = MessageBox.Show(
                        "Client version mismatch detected!\n\n" +
                        "The server has been updated and your client is out of date.\n\n" +
                        "You'll need to update your client before you can play.\n\n" +
                        "Fix this now?",
                        "Version Mismatch",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // Trigger the fix
                        await AutoFixVersionMismatchAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _svc.Logger.Write("Failed to check client version", ex);
                // Non-fatal, continue
            }
        }
        
        var ashitaDir = Path.Combine(_svc.BaseDir, "runtime", "ashita");
        var ashitaExe = Path.Combine(ashitaDir, "Ashita-cli.exe");
        var xiloaderPath = Path.Combine(ashitaDir, "bootloader", "xiloader.exe");
        
        // Check if either is missing or needs update
        if (!File.Exists(ashitaExe) || !File.Exists(xiloaderPath))
        {
            StatusLine.Text = "Status: Downloading Ashita and XiLoader...";
            ProgressBar.IsIndeterminate = true;
            
            try
            {
                await _svc.Ashita.EnsureLatestAshitaAsync(ashitaDir, new Progress<string>(s => StatusLine.Text = $"Status: {s}"), CancellationToken.None);
                await _svc.XiLoader.EnsureLatestXiLoaderAsync(xiloaderPath, new Progress<string>(s => StatusLine.Text = $"Status: {s}"), CancellationToken.None, _state.XiLoaderVersion);
                
                // Download recommended addons/plugins
                StatusLine.Text = "Status: Checking recommended addons...";
                await _svc.AddonDownloader.EnsureRecommendedAddonsAsync(ashitaDir, new Progress<string>(s => StatusLine.Text = $"Status: {s}"), CancellationToken.None);
                
                StatusLine.Text = "Status: Ready to launch";
            }
            catch (Exception ex)
            {
                _svc.Logger.Write("Failed to download Ashita/XiLoader on startup", ex);
                StatusLine.Text = "Status: Download failed (will retry on launch)";
            }
            finally
            {
                ProgressBar.IsIndeterminate = false;
            }
        }
        
        // Always check for recommended addons on startup (quick if already installed)
        if (!_hasCheckedAddons)
        {
            _hasCheckedAddons = true;
            try
            {
                StatusLine.Text = "Status: Checking recommended addons...";
                await _svc.AddonDownloader.EnsureRecommendedAddonsAsync(ashitaDir, new Progress<string>(s => StatusLine.Text = $"Status: {s}"), CancellationToken.None);
                StatusLine.Text = "Status: Ready";
            }
            catch (Exception ex)
            {
                _svc.Logger.Write("Failed to check recommended addons on startup", ex);
                StatusLine.Text = "Status: Ready";
            }
        }
        else
        {
            // Already checked, just set status to ready
            StatusLine.Text = "Status: Ready";
        }
    }

    public void LoadProfiles()
    {
        _state = _svc.StateStore.Load();
        
        ProfileCombo.Items.Clear();
        foreach (var profile in _state.Profiles)
        {
            ProfileCombo.Items.Add(profile.Name);
        }

        // Select active profile
        var activeProfile = _state.Profiles.FirstOrDefault(p => p.Name == _state.ActiveProfileName);
        if (activeProfile != null)
        {
            ProfileCombo.SelectedItem = activeProfile.Name;
            UpdateProfileSummary(activeProfile);
        }
        else if (_state.Profiles.Any())
        {
            ProfileCombo.SelectedIndex = 0;
            UpdateProfileSummary(_state.Profiles.First());
        }
    }

    private void ProfileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is string selectedProfileName)
        {
            var profile = _state.Profiles.FirstOrDefault(p => p.Name == selectedProfileName);
            if (profile != null)
            {
                // Update active profile
                _state.ActiveProfileName = selectedProfileName;
                _svc.StateStore.Save(_state);
                
                // Update the profile summary display
                UpdateProfileSummary(profile);
                
                _svc.Logger.Write($"Active profile changed to: {selectedProfileName}");
            }
        }
    }

    private void ServerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ServerCombo.SelectedItem is ComboBoxItem item && item.Tag is string host)
        {
            _state.SelectedServer = host;
            _svc.StateStore.Save(_state);
            _svc.Logger.Write($"Server changed to: {host}");
        }
    }

    private void UpdateProfileSummary(GameProfile profile)
    {
        ProfileSummary.Text = $"{profile.ResolutionWidth}x{profile.ResolutionHeight} • " +
                             $"{(profile.Windowed ? "Windowed" : "Fullscreen")} • " +
                             $"Quality {profile.GraphicsQuality}";
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        _cts = new CancellationTokenSource();
        
        try
        {
            StartBtn.IsEnabled = false;
            CancelBtn.Visibility = Visibility.Visible;
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            
            _state = _svc.StateStore.Load();

            if (string.IsNullOrWhiteSpace(_state.FfxiDir) || !Directory.Exists(_state.FfxiDir))
            {
                StatusLine.Text = "Status: Please set your FFXI folder in SETTINGS.";
                MessageBox.Show(
                    "FFXI installation folder not found.\n\n" +
                    "Please go to SETTINGS and configure your FFXI installation path.",
                    "Setup Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            if (_state.PendingRebootAtUtc is not null && !_svc.RebootGate.HasRebootedSince(_state.PendingRebootAtUtc))
            {
                StatusLine.Text = "Status: Reboot required. Go to SETTINGS and reboot, then try again.";
                MessageBox.Show(
                    "A system reboot is required to complete setup.\n\n" +
                    "Please go to SETTINGS → Reboot Gate and click 'Reboot Now'.",
                    "Reboot Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Check for version mismatch BEFORE launching
            StatusLine.Text = "Status: Checking client version...";
            try
            {
                var mismatch = await _svc.Version.CheckVersionMismatchAsync(_state.FfxiDir);
                if (mismatch == true)
                {
                    var result = MessageBox.Show(
                        "🚨 Client Version Mismatch!\n\n" +
                        "Your client is out of date and needs to be updated.\n\n" +
                        "The game will fail to launch with a POL-3331 error if you continue.\n\n" +
                        "Would you like to fix this now?",
                        "Update Required",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        await AutoFixVersionMismatchAsync();
                        return; // Don't launch game
                    }
                    else
                    {
                        StatusLine.Text = "Status: Launch cancelled - version mismatch";
                        return; // Don't launch game
                    }
                }
                // If mismatch is null (can't detect) or false (versions match), continue with launch
            }
            catch (Exception versionEx)
            {
                _svc.Logger.Write("Version check failed (non-fatal, continuing)", versionEx);
                // Continue with launch - version check is best-effort
            }

            var user = UserBox.Text.Trim();
            var pass = PassBox.Password;

            if (SaveCredsCheck.IsChecked == true && !string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
            {
                _state.SavedUser = user;
                _state.SavedPassDpapiB64 = _svc.CredentialStore.Protect(pass);
                _svc.StateStore.Save(_state);
            }

            // Get active profile
            var activeProfile = _state.Profiles.FirstOrDefault(p => p.Name == _state.ActiveProfileName)
                             ?? _state.Profiles.FirstOrDefault()
                             ?? new GameProfile();

            var ashitaDir = Path.Combine(_svc.BaseDir, "runtime", "ashita");
            StatusLine.Text = "Status: Downloading/updating Ashita…";
            await _svc.Ashita.EnsureLatestAshitaAsync(ashitaDir, new Progress<string>(s => StatusLine.Text = "Status: " + s), _cts.Token);

            var bootloaderDir = Path.Combine(ashitaDir, "bootloader");
            var xiloaderPath = Path.Combine(bootloaderDir, "xiloader.exe");

            StatusLine.Text = "Status: Downloading/updating XiLoader…";
            await _svc.XiLoader.EnsureLatestXiLoaderAsync(xiloaderPath, new Progress<string>(s => StatusLine.Text = "Status: " + s), _cts.Token, _state.XiLoaderVersion);

            // Download recommended addons/plugins (StatusTimers, MobDB, Deeps)
            StatusLine.Text = "Status: Checking recommended addons...";
            await _svc.AddonDownloader.EnsureRecommendedAddonsAsync(ashitaDir, new Progress<string>(s => StatusLine.Text = "Status: " + s), _cts.Token);

            var iniText = _svc.Ini.BuildMistIni(_state.FfxiDir!, _state.SelectedServer,
                string.IsNullOrWhiteSpace(user) ? null : user,
                string.IsNullOrWhiteSpace(pass) ? null : pass,
                activeProfile); // Pass the active profile

            // Ashita expects boot configs under ./config/boot by default.
            var bootCfgDir = Path.Combine(ashitaDir, "config", "boot");
            Directory.CreateDirectory(bootCfgDir);

            string iniPath;
            string iniName;
            if (SaveCredsCheck.IsChecked == true)
            {
                iniName = "mistxi.ini";
                iniPath = Path.Combine(bootCfgDir, iniName);
                File.WriteAllText(iniPath, iniText);
            }
            else
            {
                iniName = "mistxi.session.ini";
                iniPath = Path.Combine(bootCfgDir, iniName);
                File.WriteAllText(iniPath, iniText);
            }
            
            var ashitaExe = Path.Combine(ashitaDir, "Ashita-cli.exe");
            
            if (!File.Exists(ashitaExe))
            {
                StatusLine.Text = "Status: Error - Ashita executable not found.";
                MessageBox.Show(
                    "Ashita-cli.exe was not found after download.\n\n" +
                    "This might be a temporary download issue. Please try again.",
                    "Launch Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }
            
            // Generate mistxi.txt script from active profile
            // This ensures the script exists even after Ashita updates wipe the scripts folder
            StatusLine.Text = "Status: Generating addon script...";
            try
            {
                var scriptsDir = Path.Combine(ashitaDir, "scripts");
                Directory.CreateDirectory(scriptsDir);
                var scriptPath = Path.Combine(scriptsDir, "mistxi.txt");
                
                if (activeProfile != null && activeProfile.Name != null)
                {
                    var script = _svc.AddonManager.GenerateAshitaScript(
                        activeProfile.EnabledAddons,
                        activeProfile.EnabledPlugins,
                        activeProfile.FpsCap
                    );
                    
                    File.WriteAllText(scriptPath, script);
                    _svc.Logger.Write($"Generated mistxi.txt for profile '{activeProfile.Name}' at: {scriptPath}");
                }
                else
                {
                    // No active profile, create empty script
                    File.WriteAllText(scriptPath, "# No active profile - empty script\n");
                    _svc.Logger.Write("No active profile found, created empty mistxi.txt");
                }
            }
            catch (Exception ex)
            {
                _svc.Logger.Write("Failed to generate mistxi.txt script", ex);
                // Non-fatal - continue launching
            }
            
            StatusLine.Text = "Status: Launching…";
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 100;
            
            // Verify xiloader.exe exists and is accessible
            if (!File.Exists(xiloaderPath))
            {
                StatusLine.Text = "Status: Error - XiLoader not found.";
                MessageBox.Show(
                    $"XiLoader.exe was not found at:\n{xiloaderPath}\n\n" +
                    "Please try restarting the launcher to re-download it.",
                    "Launch Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }
            
            // Log paths for debugging
            _svc.Logger.Write($"Ashita Dir: {ashitaDir}");
            _svc.Logger.Write($"XiLoader Path: {xiloaderPath}");
            _svc.Logger.Write($"INI Path: {iniPath}");
            _svc.Logger.Write($"INI Name: {iniName}");
            
            try
            {
                // Per Ashita docs, pass the boot config name; Ashita loads it from config/boot.
                // Request elevation because Ashita needs to inject into FFXI process
                _svc.Proc.Start(ashitaExe, $"\"{iniName}\"", ashitaDir, requireElevation: true);
                StatusLine.Text = "Status: Game launched successfully!";
                
                // Monitor for version mismatch errors in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var errorDetected = await _svc.Version.MonitorForVersionErrorAsync();
                        if (errorDetected)
                        {
                            // Run on UI thread
                            await Dispatcher.InvokeAsync(async () =>
                            {
                                var result = MessageBox.Show(
                                    "The game failed to start.\n\n" +
                                    "This is likely a version mismatch (POL-3331 error).\n\n" +
                                    "Would you like to automatically fix this?",
                                    "Version Error Detected",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning
                                );
                                
                                if (result == MessageBoxResult.Yes)
                                {
                                    await AutoFixVersionMismatchAsync();
                                }
                            });
                        }
                    }
                    catch (Exception monitorEx)
                    {
                        _svc.Logger.Write("Version monitoring failed", monitorEx);
                    }
                });
            }
            catch (Exception ex)
            {
                StatusLine.Text = "Status: Launch failed.";
                var errorMsg = "Failed to launch the game.\n\n";
                
                if (ex.Message.Contains("denied") || ex.Message.Contains("access"))
                {
                    errorMsg += "PERMISSION ERROR:\n" +
                              "Windows is blocking access to the game files.\n\n" +
                              "Detailed Info:\n" +
                              $"Ashita Dir: {ashitaDir}\n" +
                              $"XiLoader: {xiloaderPath}\n" +
                              $"INI: {iniPath}\n\n" +
                              "Solutions:\n" +
                              "1. Check that xiloader.exe isn't being blocked by antivirus\n" +
                              "2. Ensure you have read/write access to AppData folder\n" +
                              "3. Try deleting the runtime folder and restarting launcher";
                }
                else
                {
                    errorMsg += $"Error: {ex.Message}\n\n" +
                              $"Ashita Dir: {ashitaDir}\n" +
                              $"XiLoader: {xiloaderPath}";
                }
                
                MessageBox.Show(errorMsg, "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _svc.Logger.Write("Launch failed", ex);
                return;
            }
            
            if (SaveCredsCheck.IsChecked != true)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    try { if (File.Exists(iniPath)) File.Delete(iniPath); } catch { }
                });
            }
            
            await Task.Delay(2000);
            StatusLine.Text = "Status: Ready.";
        }
        catch (OperationCanceledException)
        {
            _svc.Logger.Write("Launch cancelled by user");
            StatusLine.Text = "Status: Cancelled.";
            MessageBox.Show(
                "Launch cancelled.",
                "Cancelled",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (HttpRequestException ex)
        {
            _svc.Logger.Write("Network error during launch", ex);
            StatusLine.Text = "Status: Network error - check your internet connection.";
            MessageBox.Show(
                "Could not download required files.\n\n" +
                "Please check your internet connection and try again.\n\n" +
                $"Details: {ex.Message}",
                "Network Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        catch (UnauthorizedAccessException ex)
        {
            _svc.Logger.Write("Permission denied during launch", ex);
            StatusLine.Text = "Status: Permission denied - try running as administrator.";
            MessageBox.Show(
                "Permission denied while accessing files.\n\n" +
                "Try running MistXI Launcher as administrator, or check that your antivirus " +
                "isn't blocking the application.\n\n" +
                $"Details: {ex.Message}",
                "Permission Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        catch (IOException ex) when (ex.Message.Contains("space") || ex.Message.Contains("disk"))
        {
            _svc.Logger.Write("Disk error during launch", ex);
            StatusLine.Text = "Status: Disk error - check available space.";
            MessageBox.Show(
                "Disk error occurred.\n\n" +
                "Please check that you have enough free disk space and that " +
                "the drive is accessible.\n\n" +
                $"Details: {ex.Message}",
                "Disk Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("StartGame failed", ex);
            StatusLine.Text = "Status: Error - " + ex.Message;
            MessageBox.Show(
                "An unexpected error occurred while launching the game.\n\n" +
                "Please check the log file for details (Settings → Open Launcher Data Folder).\n\n" +
                $"Error: {ex.Message}",
                "Launch Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        finally
        {
            StartBtn.IsEnabled = true;
            CancelBtn.Visibility = Visibility.Collapsed;
            ProgressBar.Visibility = Visibility.Collapsed;
            ProgressBar.Value = 0;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        CancelBtn.IsEnabled = false;
        StatusLine.Text = "Status: Cancelling...";
    }


private string? _announcementUrl;

public void SetAnnouncement(MistXI.Launcher.Services.Announcement? ann)
{
    try
    {
        if (ann == null)
        {
            AnnouncementText.Text = "No news found yet. Visit mistxi.com/news/";
            _announcementUrl = "https://mistxi.com/news/";
            return;
        }

        var parts = new List<string>();
        parts.Add(ann.Title);

        if (ann.Date is not null)
            parts.Add(ann.Date.Value.LocalDateTime.ToString("MMM d, yyyy"));

        if (!string.IsNullOrWhiteSpace(ann.Summary))
            parts.Add(ann.Summary!);

        AnnouncementText.Text = string.Join("\n\n", parts);
        _announcementUrl = string.IsNullOrWhiteSpace(ann.Url) ? "https://mistxi.com/news/" : ann.Url;
    }
    catch
    {
        // ignore
    }
}

private void ViewNews_Click(object sender, RoutedEventArgs e)
{
    try
    {
        Process.Start(new ProcessStartInfo("https://mistxi.com/news/") { UseShellExecute = true });
    }
    catch (Exception ex)
    {
        _svc.Logger.Write("ViewNews_Click failed", ex);
    }
}

private async Task AutoFixVersionMismatchAsync()
{
    try
    {
        StatusLine.Text = "Status: Fixing version mismatch...";
        
        // Delete 0.DAT to trigger update (with elevation)
        await _svc.PlayOnline.TriggerClientUpdateAsync(_state.FfxiDir!);
        
        // Launch PlayOnline Viewer
        if (!string.IsNullOrWhiteSpace(_state.PlayOnlineViewerDir))
        {
            _svc.PlayOnline.LaunchPlayOnlineViewer(_state.PlayOnlineViewerDir);
            
            MessageBox.Show(
                "Version mismatch fix started!\n\n" +
                "PlayOnline Viewer is launching...\n\n" +
                "Steps to complete the update:\n" +
                "1. Click 'Check Files' on the left\n" +
                "2. Select 'FINAL FANTASY XI' from dropdown\n" +
                "3. Click 'Check Files' button\n" +
                "4. Click 'Fix Errors' when prompted\n" +
                "5. Wait for update to complete\n\n" +
                "Once complete, return here and click 'START GAME'",
                "Update Instructions",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            
            StatusLine.Text = "Status: Waiting for PlayOnline update...";
        }
        else
        {
            MessageBox.Show(
                "PlayOnline Viewer path not set.\n\n" +
                "Please go to Settings and configure the PlayOnline Viewer path,\n" +
                "then use the 'Patch FFXI' button to complete the update.",
                "Configuration Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }
    }
    catch (Exception ex)
    {
        _svc.Logger.Write("AutoFixVersionMismatch failed", ex);
        MessageBox.Show(
            $"Failed to auto-fix version mismatch.\n\nError: {ex.Message}\n\n" +
            "Please go to Settings → Patch FFXI to update manually.",
            "Auto-Fix Failed",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }
}

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            _svc.Logger.Write($"Failed to open link: {e.Uri.AbsoluteUri}", ex);
        }
    }

    private void ForgetCreds_Click(object sender, RoutedEventArgs e)
    {
        _state = _svc.StateStore.Load();
        _state.SavedUser = null;
        _state.SavedPassDpapiB64 = null;
        _svc.StateStore.Save(_state);

        UserBox.Text = "";
        PassBox.Password = "";
        SaveCredsCheck.IsChecked = false;
        StatusLine.Text = "Status: Saved login removed.";
    }
}
