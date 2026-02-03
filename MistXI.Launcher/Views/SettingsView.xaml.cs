using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MistXI.Launcher.Models;
using MistXI.Launcher.Services;

namespace MistXI.Launcher.Views;

public partial class SettingsView : UserControl
{
    private readonly AppServices _svc;
    private LauncherState _state;
    private readonly FfxiDetector _detector;

    public SettingsView(AppServices services)
    {
        InitializeComponent();
        _svc = services;
        _detector = new FfxiDetector();
        _state = _svc.StateStore.Load();

        FfxiPathBox.Text = _state.FfxiDir ?? "";
        PolPathBox.Text = _state.PlayOnlineViewerDir ?? "";
        RefreshRebootStatus();
        ValidatePaths();
        
        FfxiPathBox.TextChanged += (s, e) => ValidatePaths();
    }

    private void BrowseFfxi_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialogCompat();
        if (dlg.ShowDialog() == true) 
        {
            FfxiPathBox.Text = dlg.FolderName;
            ValidatePaths();
        }
    }

    private void AutoDetect_Click(object sender, RoutedEventArgs e)
    {
        AutoDetectBtn.IsEnabled = false;
        AutoDetectBtn.Content = "Searching...";
        
        Task.Run(() =>
        {
            var detectedPath = _detector.AutoDetectFfxiPath();
            
            Dispatcher.Invoke(() =>
            {
                if (detectedPath != null)
                {
                    FfxiPathBox.Text = detectedPath;
                    ValidatePaths();
                    MessageBox.Show(
                        $"FFXI installation found!\n\n{detectedPath}",
                        "Auto-Detect Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "Could not automatically locate FFXI installation.\n\n" +
                        "Please use the Browse button to locate it manually.",
                        "Auto-Detect Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
                
                AutoDetectBtn.IsEnabled = true;
                AutoDetectBtn.Content = "Auto-Detect";
            });
        });
    }

    private void AutoDetectPol_Click(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        btn.Content = "Searching...";
        
        Task.Run(() =>
        {
            var detectedPath = _detector.AutoDetectPlayOnlineViewerPath();
            
            Dispatcher.Invoke(() =>
            {
                if (detectedPath != null)
                {
                    PolPathBox.Text = detectedPath;
                    ValidatePaths();
                    MessageBox.Show(
                        $"PlayOnline Viewer found!\n\n{detectedPath}",
                        "Auto-Detect Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "Could not automatically locate PlayOnline Viewer.\n\n" +
                        "Please use the Browse button to locate it manually.",
                        "Auto-Detect Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
                
                btn.IsEnabled = true;
                btn.Content = "Auto-Detect";
            });
        });
    }

    private void ValidatePaths()
    {
        var ffxiPath = FfxiPathBox.Text.Trim();
        
        if (!string.IsNullOrWhiteSpace(ffxiPath))
        {
            FfxiValidation.Text = _detector.GetValidationMessage(ffxiPath);
            FfxiValidation.Foreground = FfxiValidation.Text.StartsWith("✅") 
                ? (System.Windows.Media.Brush)FindResource("Mist.Accent")
                : (System.Windows.Media.Brush)FindResource("Mist.Gold");
        }
        else
        {
            FfxiValidation.Text = "";
        }
        
        var polPath = PolPathBox.Text.Trim();
        
        if (!string.IsNullOrWhiteSpace(polPath))
        {
            PolValidation.Text = _detector.GetPolValidationMessage(polPath);
            PolValidation.Foreground = PolValidation.Text.StartsWith("✅") 
                ? (System.Windows.Media.Brush)FindResource("Mist.Accent")
                : (System.Windows.Media.Brush)FindResource("Mist.Gold");
        }
        else
        {
            PolValidation.Text = "";
        }
    }
        

    private void BrowsePol_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialogCompat();
        if (dlg.ShowDialog() == true) PolPathBox.Text = dlg.FolderName;
    }

    private void SavePaths_Click(object sender, RoutedEventArgs e)
    {
        _state = _svc.StateStore.Load();
        _state.FfxiDir = string.IsNullOrWhiteSpace(FfxiPathBox.Text) ? null : FfxiPathBox.Text.Trim();
        _state.PlayOnlineViewerDir = string.IsNullOrWhiteSpace(PolPathBox.Text) ? null : PolPathBox.Text.Trim();
        _svc.StateStore.Save(_state);
        PathStatus.Text = "Saved.";
        RefreshRebootStatus();
    }

    private void OpenPol_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _state = _svc.StateStore.Load();
            if (string.IsNullOrWhiteSpace(_state.PlayOnlineViewerDir) || !Directory.Exists(_state.PlayOnlineViewerDir))
            {
                MessageBox.Show("Set your PlayOnlineViewer folder first (Paths).", "MistXI");
                return;
            }

            var candidates = new[] { "pol.exe", "PlayOnlineViewer.exe", "playonline.exe" };
            var exe = candidates.Select(c => Path.Combine(_state.PlayOnlineViewerDir!, c)).FirstOrDefault(File.Exists);
            if (exe is null)
            {
                MessageBox.Show("Could not locate PlayOnline executable in the selected PlayOnlineViewer folder.", "MistXI");
                return;
            }

            // Use Process.Start directly for better compatibility
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = Path.GetDirectoryName(exe)!,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Failed to open PlayOnline", ex);
            MessageBox.Show(
                $"Failed to launch PlayOnline:\n\n{ex.Message}\n\n" +
                $"Try running pol.exe manually from:\n{_state.PlayOnlineViewerDir}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private async void CopyData_Click(object sender, RoutedEventArgs e)
    {
        _state = _svc.StateStore.Load();
        if (string.IsNullOrWhiteSpace(_state.PlayOnlineViewerDir) || !Directory.Exists(_state.PlayOnlineViewerDir))
        {
            MessageBox.Show("Set your PlayOnlineViewer folder first (Paths).", "MistXI");
            return;
        }
        if (string.IsNullOrWhiteSpace(_state.FfxiDir) || !Directory.Exists(_state.FfxiDir))
        {
            MessageBox.Show("Set your FINAL FANTASY XI folder first (Paths).", "MistXI");
            return;
        }

        var src = Path.Combine(_state.PlayOnlineViewerDir!, "data");
        var dst = Path.Combine(_state.FfxiDir!, "data");
        if (!Directory.Exists(src))
        {
            MessageBox.Show("PlayOnlineViewer\\data folder not found.", "MistXI");
            return;
        }

        var confirm = MessageBox.Show(
            "This will copy the PlayOnline Viewer data folder to FFXI.\n\n" +
            "Administrator privileges are required to write to Program Files.\n\n" +
            "Continue?",
            "Copy Data Folder",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            // Use the elevated helper via PlayOnlineService
            await _svc.PlayOnline.CopyDataFolderAsync(
                _state.PlayOnlineViewerDir!,
                _state.FfxiDir!,
                null, // No progress UI for manual copy
                CancellationToken.None
            );
            
            MessageBox.Show(
                "Data folder copied successfully!",
                "Copy Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show(
                "Copy cancelled - administrator privileges were declined.",
                "Copy Cancelled",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Copy POL data failed", ex);
            MessageBox.Show(
                $"Copy failed:\n\n{ex.Message}",
                "Copy Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private void MarkReboot_Click(object sender, RoutedEventArgs e)
    {
        _state = _svc.StateStore.Load();
        _state.PendingRebootAtUtc = DateTimeOffset.UtcNow;
        _svc.StateStore.Save(_state);
        RefreshRebootStatus();
        MessageBox.Show("Reboot marked as required. Use 'Reboot Now' after closing other apps.", "MistXI");
    }

    private void RebootNow_Click(object sender, RoutedEventArgs e)
    {
        _svc.Logger.Write("Reboot Now clicked.");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = "/r /t 0",
                UseShellExecute = false
            });
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Reboot command failed", ex);
            MessageBox.Show("Could not start reboot: " + ex.Message, "MistXI");
        }
    }

    private void OpenData_Click(object sender, RoutedEventArgs e) => _svc.Proc.OpenFolder(_svc.BaseDir);

    // ============================================================================
    // FFXI Installer Handlers
    // ============================================================================

    private CancellationTokenSource? _installerCts;

    private async void DownloadInstaller_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _installerCts = new CancellationTokenSource();
            DownloadInstallerBtn.IsEnabled = false;
            LaunchSetupBtn.IsEnabled = false;
            InstallerProgressBar.Visibility = Visibility.Visible;
            InstallerProgressBar.IsIndeterminate = false;
            InstallerStatusText.Text = "Preparing to download...";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.TextDim");

            var installerDir = Path.Combine(_svc.BaseDir, "installers", "ffxi");

            // Check disk space (need ~10 GB: 3.5 GB download + 3.5 GB extracted + buffer)
            var drive = new DriveInfo(Path.GetPathRoot(installerDir)!);
            if (drive.AvailableFreeSpace < 10_000_000_000L)
            {
                MessageBox.Show(
                    $"Insufficient disk space. Need at least 10 GB free, but only {drive.AvailableFreeSpace / 1_000_000_000.0:F1} GB available.\n\n" +
                    "The installer requires space for:\n" +
                    "• 3.5 GB download\n" +
                    "• 3.5 GB extraction\n" +
                    "• 3 GB buffer",
                    "Insufficient Space",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            var progress = new Progress<(int fileIndex, int totalFiles, long bytesDownloaded, long? totalBytes, string status)>(p =>
            {
                var percent = p.totalBytes.HasValue ? (double)p.bytesDownloaded / p.totalBytes.Value * 100 : 0;
                InstallerProgressBar.Value = (p.fileIndex - 1) * 20 + (percent / 5); // Each file is 20% of total
                InstallerStatusText.Text = p.status;
            });

            await _svc.FfxiInstaller.DownloadInstallerAsync(installerDir, progress, _installerCts.Token);

            InstallerStatusText.Text = "✅ Download complete! Ready to extract and launch setup.";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Accent");
            InstallerProgressBar.Value = 100;
            LaunchSetupBtn.IsEnabled = true;

            MessageBox.Show(
                "FFXI installer downloaded successfully!\n\n" +
                "Click 'Launch Setup' to extract and run the installer.\n\n" +
                "You'll need to complete the installation wizard manually.",
                "Download Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (OperationCanceledException)
        {
            InstallerStatusText.Text = "Download cancelled";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Gold");
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Installer download failed", ex);
            InstallerStatusText.Text = $"❌ Download failed: {ex.Message}";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Gold");
            MessageBox.Show(
                $"Failed to download installer:\n\n{ex.Message}\n\n" +
                "Please check your internet connection and try again.",
                "Download Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        finally
        {
            DownloadInstallerBtn.IsEnabled = true;
            _installerCts?.Dispose();
            _installerCts = null;
        }
    }

    private async void LaunchSetup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LaunchSetupBtn.IsEnabled = false;
            InstallerProgressBar.IsIndeterminate = true;
            InstallerStatusText.Text = "Extracting installer files...";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.TextDim");

            var installerDir = Path.Combine(_svc.BaseDir, "installers", "ffxi");
            
            var progress = new Progress<string>(status => InstallerStatusText.Text = status);
            var extractedDir = await _svc.FfxiInstaller.ExtractInstallerAsync(installerDir, progress);

            InstallerProgressBar.IsIndeterminate = false;
            InstallerProgressBar.Visibility = Visibility.Collapsed;
            InstallerStatusText.Text = "✅ Launching FFXI setup wizard...";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Accent");

            _svc.FfxiInstaller.LaunchSetupWizard(extractedDir);

            MessageBox.Show(
                "FFXI Setup Wizard has been launched!\n\n" +
                "Please complete the installation:\n" +
                "1. Install PlayOnline Viewer\n" +
                "2. Install FINAL FANTASY XI\n" +
                "3. Run updates if prompted\n\n" +
                "After installation, return to the launcher and use 'Auto-Detect' to find your FFXI installation.\n\n" +
                "You can then click 'Clean Up Files' to free up disk space.",
                "Setup Launched",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            LaunchSetupBtn.IsEnabled = true;
            CleanupInstallerBtn.IsEnabled = true;
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Installer launch failed", ex);
            InstallerProgressBar.IsIndeterminate = false;
            InstallerProgressBar.Visibility = Visibility.Collapsed;
            InstallerStatusText.Text = $"❌ Failed to launch setup: {ex.Message}";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Gold");
            MessageBox.Show(
                $"Failed to launch installer:\n\n{ex.Message}",
                "Launch Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            LaunchSetupBtn.IsEnabled = true;
        }
    }

    private void CleanupInstaller_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will delete all installer files (~3.5 GB) to free up disk space.\n\n" +
            "Only do this AFTER you've successfully installed FFXI!\n\n" +
            "Are you sure you want to clean up?",
            "Confirm Cleanup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var installerDir = Path.Combine(_svc.BaseDir, "installers", "ffxi");
            _svc.FfxiInstaller.CleanupInstallerFiles(installerDir);

            InstallerStatusText.Text = "✅ Installer files cleaned up successfully";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Accent");
            InstallerProgressBar.Value = 0;
            InstallerProgressBar.Visibility = Visibility.Collapsed;
            LaunchSetupBtn.IsEnabled = false;
            CleanupInstallerBtn.IsEnabled = false;

            MessageBox.Show(
                "Installer files have been deleted.\n\nFreed up approximately 3.5 GB of disk space!",
                "Cleanup Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Installer cleanup failed", ex);
            MessageBox.Show(
                $"Failed to clean up installer files:\n\n{ex.Message}\n\n" +
                "You can manually delete the files from:\n" +
                Path.Combine(_svc.BaseDir, "installers", "ffxi"),
                "Cleanup Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }
    }

    private async void PatchFfxi_Click(object sender, RoutedEventArgs e)
    {
        _state = _svc.StateStore.Load();

        // Validate paths are set
        if (string.IsNullOrWhiteSpace(_state.FfxiDir) || !Directory.Exists(_state.FfxiDir))
        {
            MessageBox.Show(
                "Please set your FFXI folder path first.",
                "FFXI Path Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(_state.PlayOnlineViewerDir) || !Directory.Exists(_state.PlayOnlineViewerDir))
        {
            MessageBox.Show(
                "Please set your PlayOnlineViewer folder path first.\n\n" +
                "This is needed to copy the data folder after patching.",
                "PlayOnline Path Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        var result = MessageBox.Show(
            "This will patch your FFXI installation for MistXI.\n\n" +
            "Steps:\n" +
            "1. Install DSP patch (if needed)\n" +
            "2. Launch PlayOnline for file check\n" +
            "3. Copy data folder\n" +
            "4. Prepare for reboot\n\n" +
            "The PlayOnline update time varies based on your hardware and connection.\n\n" +
            "Continue?",
            "Patch FFXI",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            PatchFfxiButton.IsEnabled = false;
            PatchStatusText.Text = "";

            // Step 1: Check if DSP patch is needed
            if (_svc.PlayOnline.IsDspPatchNeeded(_state.FfxiDir))
            {
                PatchStatusText.Text = "⏳ Downloading DSP patch...";
                var patchPath = await _svc.PlayOnline.DownloadDspPatchAsync(
                    new Progress<string>(s => PatchStatusText.Text = $"⏳ {s}"),
                    CancellationToken.None
                );

                PatchStatusText.Text = "⏳ Installing DSP patch (UAC prompt will appear)...";
                
                try
                {
                    await _svc.PlayOnline.InstallDspPatchAsync(patchPath, _state.FfxiDir, 
                        new Progress<string>(s => PatchStatusText.Text = $"⏳ {s}"));

                    _state.DspPatchInstalled = true;
                    _svc.StateStore.Save(_state);

                    PatchStatusText.Text = "✅ DSP patch installed successfully!";
                    await Task.Delay(2000);
                }
                catch (OperationCanceledException)
                {
                    PatchStatusText.Text = "⚠️ DSP patch cancelled - you declined administrator access";
                    MessageBox.Show(
                        "DSP patch installation was cancelled.\n\n" +
                        "The patch requires administrator privileges to modify files in Program Files.\n\n" +
                        "You can try again later by clicking 'Patch FFXI' again.",
                        "Installation Cancelled",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    return;
                }
            }
            else
            {
                PatchStatusText.Text = "ℹ️ DSP patch not needed (already installed or not required)";
                await Task.Delay(1500);
            }

            // Step 2: Launch PlayOnline for file check
            var polResult = MessageBox.Show(
                "Now you need to run PlayOnline file check.\n\n" +
                "Steps:\n" +
                "1. Click OK to launch PlayOnline Viewer\n" +
                "2. Login with: ABCD1234 / ABCD1234\n" +
                "3. Click 'Check Files' on the left\n" +
                "4. Select 'FINAL FANTASY XI' from dropdown\n" +
                "5. Click 'Check Files' button\n" +
                "6. Wait for validation to complete\n" +
                "7. Click 'Fix Errors' when prompted\n" +
                "8. Wait for updates to complete\n" +
                "9. Exit PlayOnline when done\n" +
                "10. Come back here and click 'Continue'\n\n" +
                "Ready to launch PlayOnline?",
                "PlayOnline File Check",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information
            );

            if (polResult != MessageBoxResult.OK)
            {
                PatchStatusText.Text = "❌ Patching cancelled";
                return;
            }

            _svc.PlayOnline.LaunchPlayOnlineViewer(_state.PlayOnlineViewerDir);
            PatchStatusText.Text = "⏳ Waiting for PlayOnline file check...\n\nClick 'Continue After POL Update' button below when finished.";

            // Show continue button
            var continueBtn = new Button
            {
                Content = "Continue After POL Update →",
                Style = (Style)FindResource("Mist.PrimaryButton"),
                Margin = new Thickness(0, 12, 0, 0),
                Padding = new Thickness(16, 12, 16, 12)
            };
            continueBtn.Click += async (s, args) => await ContinuePatchingAfterPolAsync(continueBtn);
            
            // Find the parent StackPanel and add button
            var parent = (StackPanel)PatchStatusText.Parent;
            parent.Children.Add(continueBtn);
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Patch FFXI failed", ex);
            PatchStatusText.Text = $"❌ Error: {ex.Message}";
            MessageBox.Show(
                $"Failed to patch FFXI:\n\n{ex.Message}",
                "Patching Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        finally
        {
            PatchFfxiButton.IsEnabled = true;
        }
    }

    private async Task ContinuePatchingAfterPolAsync(Button continueBtn)
    {
        try
        {
            continueBtn.IsEnabled = false;
            PatchStatusText.Text = "⏳ Verifying PlayOnline update...";

            _state = _svc.StateStore.Load();

            // Step 3: Copy data folder
            PatchStatusText.Text = "⏳ Copying data folder from PlayOnline to FFXI...\nThis may take several minutes...";

            await _svc.PlayOnline.CopyDataFolderAsync(
                _state.PlayOnlineViewerDir!,
                _state.FfxiDir!,
                new Progress<int>(p => PatchStatusText.Text = $"⏳ Copying data folder: {p}%"),
                CancellationToken.None
            );

            _state.DataFolderCopied = true;
            _state.PlayOnlineUpdated = true;
            _svc.StateStore.Save(_state);

            PatchStatusText.Text = "✅ Data folder copied successfully!";
            await Task.Delay(2000);

            // Step 4: Reboot reminder
            var rebootResult = MessageBox.Show(
                "Patching complete! ✅\n\n" +
                "IMPORTANT: You must reboot your computer for the changes to take effect.\n\n" +
                "After rebooting:\n" +
                "1. Create your MistXI account at: https://mistxi.com/create-account\n" +
                "2. Return to the launcher and log in to play!\n\n" +
                "Reboot now?",
                "Reboot Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information
            );

            if (rebootResult == MessageBoxResult.Yes)
            {
                _state.PendingRebootAtUtc = DateTimeOffset.UtcNow;
                _svc.StateStore.Save(_state);
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown",
                    Arguments = "/r /t 30",
                    UseShellExecute = false
                });
                
                MessageBox.Show(
                    "Your computer will reboot in 30 seconds.\n\n" +
                    "Save any open work now!",
                    "Rebooting...",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
            else
            {
                _state.PendingRebootAtUtc = DateTimeOffset.UtcNow;
                _svc.StateStore.Save(_state);
                
                PatchStatusText.Text = "✅ Patching complete! Remember to reboot before playing.";
                
                MessageBox.Show(
                    "Patching complete!\n\n" +
                    "Remember to:\n" +
                    "1. REBOOT your computer\n" +
                    "2. Create account: https://mistxi.com/create-account\n" +
                    "3. Return here to play!",
                    "Patching Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }

            // Remove continue button
            var parent = (StackPanel)continueBtn.Parent;
            parent.Children.Remove(continueBtn);
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Continue patching failed", ex);
            PatchStatusText.Text = $"❌ Error: {ex.Message}";
            MessageBox.Show(
                $"Failed to complete patching:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private void ResetState_Click(object sender, RoutedEventArgs e)
    {
        _svc.StateStore.Save(new LauncherState());
        _state = _svc.StateStore.Load();
        FfxiPathBox.Text = "";
        PolPathBox.Text = "";
        RefreshRebootStatus();
        MessageBox.Show("State reset.", "MistXI");
    }

    private void RefreshRebootStatus()
    {
        _state = _svc.StateStore.Load();
        if (_state.PendingRebootAtUtc is null)
        {
            RebootStatus.Text = "Reboot gate: not set.";
            return;
        }

        var ok = _svc.RebootGate.HasRebootedSince(_state.PendingRebootAtUtc);
        RebootStatus.Text = ok ? "Reboot gate: satisfied (boot time is after marker)." : "Reboot gate: reboot still required.";
    }

}

internal sealed class OpenFolderDialogCompat
{
    public string FolderName { get; private set; } = "";

    public bool? ShowDialog()
    {
        var dlg = new OpenFileDialog
        {
            CheckFileExists = false,
            CheckPathExists = true,
            ValidateNames = false,
            FileName = "Select Folder",
            Filter = "Folders|no.files"
        };

        var ok = dlg.ShowDialog();
        if (ok == true)
            FolderName = Path.GetDirectoryName(dlg.FileName) ?? "";

        return ok;
    }
}
