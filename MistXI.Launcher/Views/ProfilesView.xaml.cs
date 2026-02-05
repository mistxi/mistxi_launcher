using System.Windows;
using System.Windows.Controls;
using MistXI.Launcher.Models;

namespace MistXI.Launcher.Views;

public partial class ProfilesView : UserControl
{
    private readonly AppServices _svc;
    private LauncherState _state;
    private GameProfile? _currentProfile;
    private bool _isLoading;

    public ProfilesView(AppServices services)
    {
        InitializeComponent();
        _svc = services;
        _state = _svc.StateStore.Load();
        
        LoadProfiles();
        UpdateActiveProfileDisplay();
    }

    private void LoadProfiles()
    {
        _isLoading = true;
        ProfileList.Items.Clear();
        
        foreach (var profile in _state.Profiles)
        {
            ProfileList.Items.Add(profile.Name);
        }

        if (_state.Profiles.Any())
        {
            var activeProfile = _state.Profiles.FirstOrDefault(p => p.Name == _state.ActiveProfileName)
                             ?? _state.Profiles.First();
            ProfileList.SelectedItem = activeProfile.Name;
        }
        
        _isLoading = false;
    }

    private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || ProfileList.SelectedItem == null) return;

        var profileName = ProfileList.SelectedItem.ToString()!;
        _currentProfile = _state.Profiles.FirstOrDefault(p => p.Name == profileName);

        if (_currentProfile != null)
        {
            LoadProfileIntoUI(_currentProfile);
            SettingsPanel.IsEnabled = true;
            SaveProfileBtn.IsEnabled = false;
        }
    }

    private void LoadProfileIntoUI(GameProfile profile)
    {
        _isLoading = true;

        ProfileNameBox.Text = profile.Name;
        ResWidthBox.Text = profile.ResolutionWidth.ToString();
        ResHeightBox.Text = profile.ResolutionHeight.ToString();
        
        // Set window mode dropdown
        foreach (ComboBoxItem item in WindowModeCombo.Items)
        {
            if (item.Tag is string tag && int.Parse(tag) == profile.WindowMode)
            {
                WindowModeCombo.SelectedItem = item;
                break;
            }
        }
        
        GraphicsQualitySlider.Value = profile.GraphicsQuality;
        UpdateGraphicsQualityText();
        
        HardwareMouseCheck.IsChecked = profile.HardwareMouseCursor;
        MipMappingCheck.IsChecked = profile.MipMapping;
        BumpMappingCheck.IsChecked = profile.BumpMapping;
        EnvDiffuseCheck.IsChecked = profile.EnvDiffuseMapping;
        
        BackBufferCountBox.Text = profile.BackBufferCount.ToString();
        MultiSampleTypeBox.Text = profile.MultiSampleType.ToString();
        
        // Set FpsCap ComboBox
        FpsCapCombo.SelectedIndex = profile.FpsCap; // 0=Default, 1=60fps, 2=30fps
        
        // Set PresentationInterval ComboBox
        PresentationIntervalCombo.SelectedIndex = profile.PresentationInterval switch
        {
            -1 => 0,
            0 => 1,
            1 => 2,
            2 => 3,
            _ => 0 // Default to Auto if unknown
        };
        MapCompressionCombo.SelectedIndex = profile.MapCompressionType;
        
        // Audio Settings
        SoundVolumeSlider.Value = profile.SoundEffectsVolume;
        SoundEnabledCheck.IsChecked = profile.SoundEnabled;
        SoundAlwaysOnCheck.IsChecked = profile.SoundAlwaysOn;
        
        // Input Settings - Keyboard
        KeyboardBlockBindsDuringInputCheck.IsChecked = profile.KeyboardBlockBindsDuringInput;
        KeyboardSilentBindsCheck.IsChecked = profile.KeyboardSilentBinds;
        KeyboardWindowsKeyEnabledCheck.IsChecked = profile.KeyboardWindowsKeyEnabled;
        
        // Input Settings - Mouse
        MouseUnhookCheck.IsChecked = profile.MouseUnhook;
        
        // Input Settings - Gamepad
        GamepadAllowBackgroundCheck.IsChecked = profile.GamepadAllowBackground;
        GamepadDisableEnumerationCheck.IsChecked = profile.GamepadDisableEnumeration;
        
        // Visual Settings
        ShowOpeningMovieCheck.IsChecked = profile.ShowOpeningMovie;
        SimplifiedCharCreationCheck.IsChecked = profile.SimplifiedCharCreation;
        MaintainAspectRatioCheck.IsChecked = profile.MaintainAspectRatio;
        GammaSlider.Value = profile.GammaBase;
        UpdateGammaText();
        
        // Developer Settings
        LogLevelCombo.SelectedIndex = profile.LogLevel;
        CrashDumpsCheck.IsChecked = profile.CrashDumps;
        AddonsSilentCheck.IsChecked = profile.AddonsSilent;
        PluginsSilentCheck.IsChecked = profile.PluginsSilent;
        
        SoundVolumeSlider.Value = profile.SoundEffectsVolume;
        UpdateSoundVolumeText();

        _isLoading = false;
    }

    private void ProfileSettings_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading || _currentProfile == null) return;
        
        if (sender == GraphicsQualitySlider)
            UpdateGraphicsQualityText();
        else if (sender == SoundVolumeSlider)
            UpdateSoundVolumeText();
        else if (sender == GammaSlider)
            UpdateGammaText();
            
        SaveProfileBtn.IsEnabled = true;
        SaveStatusText.Text = "* Unsaved changes";
    }

    private void ProfileSettings_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isLoading || _currentProfile == null) return;
        
        SaveProfileBtn.IsEnabled = true;
        SaveStatusText.Text = "* Unsaved changes";
    }

    private void ProfileSettings_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isLoading || _currentProfile == null) return;
        
        SaveProfileBtn.IsEnabled = true;
        SaveStatusText.Text = "* Unsaved changes";
    }

    private void UpdateGraphicsQualityText()
    {
        var value = (int)GraphicsQualitySlider.Value;
        var quality = value switch
        {
            0 => "Lowest",
            1 => "Very Low",
            2 => "Low",
            3 => "Medium",
            4 => "High (Recommended)",
            5 => "Very High",
            6 => "Maximum",
            _ => "Unknown"
        };
        GraphicsQualityText.Text = $"Quality: {quality} ({value})";
    }

    private void UpdateSoundVolumeText()
    {
        var value = (int)SoundVolumeSlider.Value;
        SoundVolumeText.Text = $"Max Sounds: {value} (12 = Lowest, 20 = Highest)";
    }

    private void UpdateGammaText()
    {
        var value = (int)GammaSlider.Value;
        var description = value == 0 ? "Default" : value < 0 ? "Darker" : "Brighter";
        GammaText.Text = $"Gamma: {value} ({description})";
    }

    private void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProfile == null) return;

        try
        {
            // Update profile from UI
            var oldName = _currentProfile.Name;
            _currentProfile.Name = ProfileNameBox.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(_currentProfile.Name))
            {
                MessageBox.Show("Profile name cannot be empty.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                _currentProfile.Name = oldName;
                return;
            }

            // Check for duplicate names
            if (_state.Profiles.Any(p => p.Name == _currentProfile.Name && p != _currentProfile))
            {
                MessageBox.Show("A profile with this name already exists.", "Duplicate Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                _currentProfile.Name = oldName;
                return;
            }

            _currentProfile.ResolutionWidth = int.TryParse(ResWidthBox.Text, out var w) ? w : 1920;
            _currentProfile.ResolutionHeight = int.TryParse(ResHeightBox.Text, out var h) ? h : 1080;
            
            // Get WindowMode from ComboBox Tag
            if (WindowModeCombo.SelectedItem is ComboBoxItem wmItem && wmItem.Tag is string wmTag)
            {
                _currentProfile.WindowMode = int.Parse(wmTag);
                // Also set Windowed for backward compatibility (1=windowed, 0 or 3=not windowed)
                _currentProfile.Windowed = _currentProfile.WindowMode == 1;
            }
            
            _currentProfile.GraphicsQuality = (int)GraphicsQualitySlider.Value;
            _currentProfile.HardwareMouseCursor = HardwareMouseCheck.IsChecked == true;
            _currentProfile.MipMapping = MipMappingCheck.IsChecked == true;
            _currentProfile.BumpMapping = BumpMappingCheck.IsChecked == true;
            _currentProfile.EnvDiffuseMapping = EnvDiffuseCheck.IsChecked == true;
            _currentProfile.BackBufferCount = int.TryParse(BackBufferCountBox.Text, out var bbc) ? bbc : -1;
            _currentProfile.MultiSampleType = int.TryParse(MultiSampleTypeBox.Text, out var mst) ? mst : -1;
            
            // FpsCap is saved separately in FpsCap_Changed handler
            
            // Get PresentationInterval from ComboBox Tag
            if (PresentationIntervalCombo.SelectedItem is ComboBoxItem piItem && piItem.Tag is string piTag)
            {
                _currentProfile.PresentationInterval = int.Parse(piTag);
            }
            _currentProfile.MapCompressionType = MapCompressionCombo.SelectedIndex;
            _currentProfile.SoundEffectsVolume = (int)SoundVolumeSlider.Value;
            
            // Audio Settings
            _currentProfile.SoundEnabled = SoundEnabledCheck.IsChecked == true;
            _currentProfile.SoundAlwaysOn = SoundAlwaysOnCheck.IsChecked == true;
            
            // Input Settings - Keyboard
            _currentProfile.KeyboardBlockBindsDuringInput = KeyboardBlockBindsDuringInputCheck.IsChecked == true;
            _currentProfile.KeyboardSilentBinds = KeyboardSilentBindsCheck.IsChecked == true;
            _currentProfile.KeyboardWindowsKeyEnabled = KeyboardWindowsKeyEnabledCheck.IsChecked == true;
            
            // Input Settings - Mouse
            _currentProfile.MouseUnhook = MouseUnhookCheck.IsChecked == true;
            
            // Input Settings - Gamepad
            _currentProfile.GamepadAllowBackground = GamepadAllowBackgroundCheck.IsChecked == true;
            _currentProfile.GamepadDisableEnumeration = GamepadDisableEnumerationCheck.IsChecked == true;
            
            // Visual Settings
            _currentProfile.ShowOpeningMovie = ShowOpeningMovieCheck.IsChecked == true;
            _currentProfile.SimplifiedCharCreation = SimplifiedCharCreationCheck.IsChecked == true;
            _currentProfile.MaintainAspectRatio = MaintainAspectRatioCheck.IsChecked == true;
            _currentProfile.GammaBase = (int)GammaSlider.Value;
            
            // Developer Settings
            if (LogLevelCombo.SelectedItem is ComboBoxItem logItem && logItem.Tag is string logTag)
            {
                _currentProfile.LogLevel = int.Parse(logTag);
            }
            _currentProfile.CrashDumps = CrashDumpsCheck.IsChecked == true;
            _currentProfile.AddonsSilent = AddonsSilentCheck.IsChecked == true;
            _currentProfile.PluginsSilent = PluginsSilentCheck.IsChecked == true;

            // Update active profile name if this profile's name changed and it's active
            if (oldName != _currentProfile.Name && _state.ActiveProfileName == oldName)
            {
                _state.ActiveProfileName = _currentProfile.Name;
            }

            _svc.StateStore.Save(_state);
            _svc.Logger.Write($"Profile saved: {_currentProfile.Name}");

            SaveProfileBtn.IsEnabled = false;
            SaveStatusText.Text = "✅ Profile saved successfully";
            
            // Refresh the list to show new name
            LoadProfiles();
            UpdateActiveProfileDisplay();
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Failed to save profile", ex);
            SaveStatusText.Text = "❌ Error saving profile";
            MessageBox.Show($"Failed to save profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        var baseName = "New Profile";
        var name = baseName;
        var counter = 1;
        
        while (_state.Profiles.Any(p => p.Name == name))
        {
            name = $"{baseName} {counter++}";
        }

        var newProfile = new GameProfile { Name = name };
        _state.Profiles.Add(newProfile);
        _svc.StateStore.Save(_state);
        
        LoadProfiles();
        ProfileList.SelectedItem = name;
        
        _svc.Logger.Write($"New profile created: {name}");
    }

    private void FpsCap_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || _currentProfile == null || FpsCapCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
            return;

        int fpsCap = int.Parse(tag);
        _currentProfile.FpsCap = fpsCap;

        // If not default, ensure fps addon is enabled
        if (fpsCap > 0)
        {
            if (!_currentProfile.EnabledAddons.Contains("fps"))
            {
                _currentProfile.EnabledAddons.Add("fps");
                _svc.Logger.Write("FPS addon automatically enabled due to FPS cap setting");
            }
        }

        // Ensure the profile in state is updated
        var profileInState = _state.Profiles.FirstOrDefault(p => p.Name == _currentProfile.Name);
        if (profileInState != null)
        {
            profileInState.FpsCap = _currentProfile.FpsCap;
            profileInState.EnabledAddons = _currentProfile.EnabledAddons;
        }

        // Save profile
        _svc.StateStore.Save(_state);

        // Regenerate the mistxi.txt script with addons + FPS commands
        var ashitaDir = Path.Combine(_svc.BaseDir, "runtime", "ashita");
        Directory.CreateDirectory(ashitaDir);
        
        try
        {
            // Write to ashita root directory (Ashita v4 changed script location)
            var scriptPath = Path.Combine(ashitaDir, "mistxi.txt");
            var script = _svc.AddonManager.GenerateAshitaScript(
                _currentProfile.EnabledAddons,
                _currentProfile.EnabledPlugins,
                _currentProfile.FpsCap
            );
            
            File.WriteAllText(scriptPath, script);
            _svc.Logger.Write($"FPS changed to {fpsCap}, mistxi.txt regenerated at: {scriptPath}");
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Failed to regenerate Ashita script after FPS change", ex);
        }
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProfile == null) return;

        if (_state.Profiles.Count == 1)
        {
            MessageBox.Show("Cannot delete the last profile.", "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to delete the profile '{_currentProfile.Name}'?",
            "Delete Profile",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            var profileName = _currentProfile.Name;
            _state.Profiles.Remove(_currentProfile);
            
            // Update active profile if we deleted it
            if (_state.ActiveProfileName == profileName)
            {
                _state.ActiveProfileName = _state.Profiles.First().Name;
            }
            
            _svc.StateStore.Save(_state);
            _svc.Logger.Write($"Profile deleted: {profileName}");
            
            LoadProfiles();
            UpdateActiveProfileDisplay();
        }
    }

    private void SetActive_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProfile == null) return;

        _state.ActiveProfileName = _currentProfile.Name;
        _svc.StateStore.Save(_state);
        _svc.Logger.Write($"Active profile set to: {_currentProfile.Name}");
        
        UpdateActiveProfileDisplay();
        MessageBox.Show($"'{_currentProfile.Name}' is now the active profile.", "Profile Activated", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void UpdateActiveProfileDisplay()
    {
        ActiveProfileText.Text = $"Active Profile: {_state.ActiveProfileName}";
    }
}
