using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using MistXI.Launcher.Models;
using MistXI.Launcher.Services;

namespace MistXI.Launcher;

public partial class FirstRunWizard : Window
{
    private readonly AppServices _svc;
    private readonly FfxiDetector _detector;
    private LauncherState _state;

    public FirstRunWizard(AppServices services)
    {
        InitializeComponent();
        _svc = services;
        _detector = new FfxiDetector();
        _state = _svc.StateStore.Load();

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Try to auto-detect FFXI installation
        await Task.Run(() =>
        {
            var detectedPath = _detector.AutoDetectFfxiPath();
            var detectedPolPath = _detector.AutoDetectPlayOnlineViewerPath();
            
            Dispatcher.Invoke(() =>
            {
                if (detectedPath != null)
                {
                    FfxiPathBox.Text = detectedPath;
                    DetectionStatusText.Text = "✅ FFXI installation found automatically!";
                    DetectionStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Accent");
                    ValidationText.Text = _detector.GetValidationMessage(detectedPath);
                    ValidationText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Accent");
                }
                else
                {
                    DetectionStatusText.Text = "❌ FFXI installation not found";
                    DetectionStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Gold");
                    ValidationText.Text = "Please locate FFXI manually or download it.";
                    DownloadInstallButton.Visibility = Visibility.Visible;
                }

                // Auto-detect PlayOnline Viewer
                if (detectedPolPath != null)
                {
                    PolPathBox.Text = detectedPolPath;
                }

                // Enable finish only if both are found
                FinishButton.IsEnabled = detectedPath != null && detectedPolPath != null;
            });
        });
    }

    private void FfxiPathBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var path = FfxiPathBox.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(path))
        {
            ValidationText.Text = "";
            FinishButton.IsEnabled = false;
            return;
        }

        var message = _detector.GetValidationMessage(path);
        ValidationText.Text = message;

        if (message.StartsWith("✅"))
        {
            ValidationText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Accent");
            DownloadInstallButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            ValidationText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Gold");
        }
        
        // Check both paths before enabling finish
        CheckBothPathsAndEnableFinish();
    }

    private void PolPathBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var path = PolPathBox.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(path))
        {
            PolValidationText.Text = "";
            FinishButton.IsEnabled = false;
            return;
        }

        var message = _detector.GetPolValidationMessage(path);
        PolValidationText.Text = message;

        if (message.StartsWith("✅"))
        {
            PolValidationText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Accent");
        }
        else
        {
            PolValidationText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Gold");
        }
        
        // Check both paths before enabling finish
        CheckBothPathsAndEnableFinish();
    }

    private void CheckBothPathsAndEnableFinish()
    {
        var ffxiValid = !string.IsNullOrWhiteSpace(FfxiPathBox.Text) && 
                        _detector.IsValidFfxiPath(FfxiPathBox.Text.Trim());
        var polValid = !string.IsNullOrWhiteSpace(PolPathBox.Text) && 
                       _detector.IsValidPlayOnlineViewerPath(PolPathBox.Text.Trim());
        
        FinishButton.IsEnabled = ffxiValid && polValid;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialogCompat();
        if (dlg.ShowDialog() == true)
        {
            FfxiPathBox.Text = dlg.FolderName;
        }
    }

    private void BrowsePol_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialogCompat();
        if (dlg.ShowDialog() == true)
        {
            PolPathBox.Text = dlg.FolderName;
        }
    }

    private async void ReDetect_Click(object sender, RoutedEventArgs e)
    {
        ReDetectButton.IsEnabled = false;
        DetectionStatusText.Text = "🔍 Searching for FFXI installation...";
        DetectionStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.TextDim");
        
        await Task.Run(() =>
        {
            var detectedPath = _detector.AutoDetectFfxiPath();
            var detectedPolPath = _detector.AutoDetectPlayOnlineViewerPath();
            
            Dispatcher.Invoke(() =>
            {
                if (detectedPath != null)
                {
                    FfxiPathBox.Text = detectedPath;
                    DetectionStatusText.Text = "✅ FFXI installation found!";
                    DetectionStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Accent");
                    ValidationText.Text = _detector.GetValidationMessage(detectedPath);
                    ValidationText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Accent");
                    
                    // Check for installer files to cleanup
                    CheckAndOfferInstallerCleanup();
                }
                else
                {
                    DetectionStatusText.Text = "❌ FFXI installation not found";
                    DetectionStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Gold");
                    ValidationText.Text = "Please locate FFXI manually or download it.";
                }

                // Auto-detect PlayOnline Viewer
                if (detectedPolPath != null)
                {
                    PolPathBox.Text = detectedPolPath;
                    PolValidationText.Text = "✅ Valid PlayOnline Viewer installation";
                    PolValidationText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Accent");
                }

                CheckBothPathsAndEnableFinish();
                ReDetectButton.IsEnabled = true;
            });
        });
    }

    private void CheckAndOfferInstallerCleanup()
    {
        try
        {
            var installerDir = Path.Combine(_svc.BaseDir, "installers", "ffxi");
            if (Directory.Exists(installerDir) && Directory.EnumerateFileSystemEntries(installerDir).Any())
            {
                var cleanup = MessageBox.Show(
                    "FFXI installation detected!\n\n" +
                    "The installer files are no longer needed (~3.5 GB).\n\n" +
                    "Would you like to clean them up to free disk space?",
                    "Clean Up Installer Files?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (cleanup == MessageBoxResult.Yes)
                {
                    _svc.FfxiInstaller.CleanupInstallerFiles(installerDir);
                    MessageBox.Show(
                        "Installer files cleaned up!\n\nFreed approximately 3.5 GB of disk space.",
                        "Cleanup Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Failed to cleanup installer files", ex);
            // Non-critical, don't bother user
        }
    }

    private CancellationTokenSource? _installerCts;

    private async void DownloadInstall_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will download the official FFXI installer from Square Enix (~3.5 GB).\n\n" +
            "The launcher will:\n" +
            "1. Download all installer files automatically\n" +
            "2. Extract the installer\n" +
            "3. Launch the setup wizard for you to complete\n\n" +
            "⚠️ You'll need about 10 GB of free disk space.\n\n" +
            "Continue?",
            "Download FFXI Installer",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result != MessageBoxResult.Yes) return;

        try
        {
            _installerCts = new CancellationTokenSource();
            DownloadInstallButton.IsEnabled = false;
            InstallerProgressBar.Visibility = Visibility.Visible;
            InstallerProgressBar.IsIndeterminate = false;
            InstallerStatusText.Visibility = Visibility.Visible;
            InstallerStatusText.Text = "Preparing to download...";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.TextDim");

            var installerDir = Path.Combine(_svc.BaseDir, "installers", "ffxi");

            // Check disk space
            var drive = new DriveInfo(Path.GetPathRoot(installerDir)!);
            if (drive.AvailableFreeSpace < 10_000_000_000L)
            {
                MessageBox.Show(
                    $"Insufficient disk space.\n\n" +
                    $"Need: 10 GB\n" +
                    $"Available: {drive.AvailableFreeSpace / 1_000_000_000.0:F1} GB\n\n" +
                    "Please free up space and try again.",
                    "Insufficient Space",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            var progress = new Progress<(int fileIndex, int totalFiles, long bytesDownloaded, long? totalBytes, string status)>(p =>
            {
                var percent = p.totalBytes.HasValue ? (double)p.bytesDownloaded / p.totalBytes.Value * 100 : 0;
                InstallerProgressBar.Value = (p.fileIndex - 1) * 20 + (percent / 5);
                InstallerStatusText.Text = p.status;
            });

            await _svc.FfxiInstaller.DownloadInstallerAsync(installerDir, progress, _installerCts.Token);

            InstallerStatusText.Text = "✅ Download complete! Ready to launch setup.";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Accent");
            InstallerProgressBar.Value = 100;
            LaunchSetupButton.Visibility = Visibility.Visible;

            MessageBox.Show(
                "Installer downloaded successfully!\n\n" +
                "Click 'Launch FFXI Setup Wizard' to continue.",
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
                "Check your internet connection and try again.",
                "Download Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        finally
        {
            DownloadInstallButton.IsEnabled = true;
            _installerCts?.Dispose();
            _installerCts = null;
        }
    }

    private async void LaunchSetup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LaunchSetupButton.IsEnabled = false;
            InstallerProgressBar.IsIndeterminate = true;
            InstallerStatusText.Text = "Extracting installer files (this may take a few minutes)...";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.TextDim");

            var installerDir = Path.Combine(_svc.BaseDir, "installers", "ffxi");
            
            var progress = new Progress<string>(status => InstallerStatusText.Text = status);
            var extractedDir = await _svc.FfxiInstaller.ExtractInstallerAsync(installerDir, progress);

            InstallerProgressBar.IsIndeterminate = false;
            InstallerProgressBar.Visibility = Visibility.Collapsed;
            InstallerStatusText.Text = "✅ Launching setup wizard...";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Accent");

            _svc.FfxiInstaller.LaunchSetupWizard(extractedDir);

            MessageBox.Show(
                "FFXI Setup Wizard launched!\n\n" +
                "Please complete the installation:\n" +
                "1. Install PlayOnline Viewer\n" +
                "2. Install FINAL FANTASY XI\n" +
                "3. Complete any updates\n\n" +
                "After installation completes:\n" +
                "• Come back to this wizard\n" +
                "• The path should auto-detect\n" +
                "• If not, use 'Browse' to locate it\n\n" +
                "Default location:\n" +
                "C:\\Program Files (x86)\\PlayOnline\\SquareEnix\\FINAL FANTASY XI",
                "Complete Installation",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            LaunchSetupButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Installer launch failed", ex);
            InstallerProgressBar.IsIndeterminate = false;
            InstallerProgressBar.Visibility = Visibility.Collapsed;
            InstallerStatusText.Text = $"❌ Failed to launch setup";
            InstallerStatusText.Foreground = (System.Windows.Media.Brush)FindResource("Mist.Gold");
            MessageBox.Show(
                $"Failed to launch installer:\n\n{ex.Message}",
                "Launch Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            LaunchSetupButton.IsEnabled = true;
        }
    }

    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        // Save the paths
        _state.FfxiDir = FfxiPathBox.Text.Trim();
        _state.PlayOnlineViewerDir = string.IsNullOrWhiteSpace(PolPathBox.Text) 
            ? null 
            : PolPathBox.Text.Trim();
        _state.SetupComplete = true;
        
        _svc.StateStore.Save(_state);
        _svc.Logger.Write("First-run setup completed");
        _svc.Logger.Write($"FFXI Dir: {_state.FfxiDir}");
        _svc.Logger.Write($"POL Dir: {_state.PlayOnlineViewerDir ?? "Not set"}");

        // Remind about account creation
        var accountMsg = MessageBox.Show(
            "Setup complete! ✅\n\n" +
            "Before you can play, you need to create a MistXI account.\n\n" +
            "Would you like to open the account creation page now?\n\n" +
            "https://mistxi.com/create-account",
            "Create Your Account",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information
        );

        if (accountMsg == MessageBoxResult.Yes)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://mistxi.com/create-account",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _svc.Logger.Write("Failed to open account creation page", ex);
                MessageBox.Show(
                    "Could not open browser. Please visit:\nhttps://mistxi.com/create-account",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to cancel setup?\n\n" +
            "You'll need to configure paths in Settings before you can play.",
            "Cancel Setup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            DialogResult = false;
            Close();
        }
    }
}

// Reuse the folder dialog helper
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
