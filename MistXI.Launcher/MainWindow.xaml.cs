using System.Windows;
using MistXI.Launcher.Views;
using MistXI.Launcher.Services;

namespace MistXI.Launcher;

public partial class MainWindow : Window
{
    private HomeView? _home;
    private ProfilesView? _profiles;
    private AddonsView? _addons;
    private SettingsView? _settings;
    private DispatcherTimer? _refreshTimer;

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            Services = AppServices.Create();
            
            // Set version from assembly
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var versionStr = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.1.0";
            VersionText.Text = versionStr;

            // Check if this is first run
            var state = Services.StateStore.Load();
            if (!state.SetupComplete || string.IsNullOrWhiteSpace(state.FfxiDir))
            {
                Services.Logger.Write("First run detected - showing setup wizard");
                var wizard = new FirstRunWizard(Services);
                var result = wizard.ShowDialog();
                
                if (result != true)
                {
                    Services.Logger.Write("Setup wizard cancelled - exiting");
                    Application.Current.Shutdown();
                    return;
                }
                
                // Reload state after wizard
                state = Services.StateStore.Load();
                Services.Logger.Write("Setup wizard completed, state reloaded");
            }

            Services.Logger.Write("Initializing views...");
            
            // Initialize views (after wizard completes or if setup already done)
            _home = new HomeView(Services);
            Services.Logger.Write("HomeView created");
            
            _profiles = new ProfilesView(Services);
            Services.Logger.Write("ProfilesView created");
            
            _addons = new AddonsView(Services);
            Services.Logger.Write("AddonsView created");
            
            _settings = new SettingsView(Services);
            Services.Logger.Write("SettingsView created");

            MainContent.Content = _home;
            StatusText.Text = "Ready.";
            PlayersOnlineText.Text = "—";

            Services.Logger.Write("MainWindow initialized.");
            Services.Logger.Write($"Log file: {Services.Logger.LogPath}");

            SetActiveNav("HOME");

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(60)
            };
            _refreshTimer.Tick += async (_, __) => await RefreshLiveDataAsync();
            Loaded += async (_, __) => await RefreshLiveDataAsync();
            _refreshTimer.Start();
            
            Services.Logger.Write("MainWindow initialization complete");
        }
        catch (Exception ex)
        {
            var logger = new Logger("MistXI");
            logger.Write("FATAL ERROR during MainWindow initialization", ex);
            
            MessageBox.Show(
                $"Fatal error initializing launcher:\n\n{ex.Message}\n\n" +
                $"Stack trace:\n{ex.StackTrace}\n\n" +
                $"Check log at: {logger.LogPath}",
                "Launcher Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            
            Application.Current.Shutdown();
        }
    }

    public AppServices Services { get; }

    private void Logo_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://mistxi.com",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Services.Logger.Write("Failed to open website", ex);
        }
    }

    private async Task RefreshLiveDataAsync()
    {
        if (_home == null) return;
        try
        {
            var ct = CancellationToken.None;

            var online = await Services.MistWeb.GetPlayersOnlineAsync(ct);
            PlayersOnlineText.Text = online?.ToString() ?? "—";

            var ann = await Services.MistWeb.GetLatestAnnouncementAsync(ct);
            _home.SetAnnouncement(ann);
        }
        catch (Exception ex)
        {
            Services.Logger.Write("RefreshLiveData failed");
            Services.Logger.Write(ex.ToString());
        }
    }

    private void SetActiveNav(string page)
    {
        NavHome.IsChecked = page == "HOME";
        NavProfiles.IsChecked = page == "PROFILES";
        NavAddons.IsChecked = page == "ADDONS";
        NavSettings.IsChecked = page == "SETTINGS";
    }

    private void NavHome_Click(object sender, RoutedEventArgs e)
    {
        if (_home == null) return;
        Services.Logger.Write("Navigate: HOME");
        _home.LoadProfiles(); // Refresh profiles
        MainContent.Content = _home;
        SetActiveNav("HOME");
    }

    private void NavProfiles_Click(object sender, RoutedEventArgs e)
    {
        if (_profiles == null) return;
        Services.Logger.Write("Navigate: PROFILES");
        MainContent.Content = _profiles;
        SetActiveNav("PROFILES");
    }

    private void NavAddons_Click(object sender, RoutedEventArgs e)
    {
        if (_addons == null) return;
        Services.Logger.Write("Navigate: ADDONS");
        MainContent.Content = _addons;
        SetActiveNav("ADDONS");
    }

    private void NavSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_settings == null) return;
        Services.Logger.Write("Navigate: SETTINGS");
        MainContent.Content = _settings;
        SetActiveNav("SETTINGS");
    }
}
