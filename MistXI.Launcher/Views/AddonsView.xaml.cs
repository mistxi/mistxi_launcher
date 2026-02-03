using System.Windows;
using System.Windows.Controls;
using MistXI.Launcher.Models;
using MistXI.Launcher.Services;

namespace MistXI.Launcher.Views;

public partial class AddonsView : UserControl
{
    private readonly AppServices _svc;
    private LauncherState _state;
    private GameProfile? _activeProfile;

    public AddonsView(AppServices services)
    {
        InitializeComponent();
        _svc = services;
        _state = _svc.StateStore.Load();
        
        LoadAddons();
        
        // Refresh when view is loaded (in case profile changed)
        Loaded += (s, e) => LoadAddons();
    }

    private void LoadAddons()
    {
        _state = _svc.StateStore.Load();
        _activeProfile = _state.Profiles.FirstOrDefault(p => p.Name == _state.ActiveProfileName);
        
        if (_activeProfile == null)
        {
            ActiveProfileText.Text = "No active profile";
            return;
        }

        ActiveProfileText.Text = _activeProfile.Name;

        var ashitaDir = Path.Combine(_svc.BaseDir, "runtime", "ashita");
        
        // Check if Ashita is downloaded
        if (!Directory.Exists(ashitaDir))
        {
            AddonsPanel.Children.Clear();
            AddonsPanel.Children.Add(new TextBlock
            {
                Text = "Ashita not downloaded yet. Launch the game once to download Ashita, then return here to manage addons.",
                Style = (Style)FindResource("Mist.Caption"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0),
                Foreground = System.Windows.Media.Brushes.Orange
            });
            
            PluginsPanel.Children.Clear();
            PluginsPanel.Children.Add(new TextBlock
            {
                Text = "Ashita not downloaded yet.",
                Style = (Style)FindResource("Mist.Caption"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0),
                Foreground = System.Windows.Media.Brushes.Orange
            });
            
            AddonCountText.Text = "(0 found - not downloaded)";
            PluginCountText.Text = "(0 found - not downloaded)";
            return;
        }
        
        // Scan and display addons
        var addons = _svc.AddonManager.ScanAddons(ashitaDir);
        AddonsPanel.Children.Clear();
        
        if (!addons.Any())
        {
            AddonsPanel.Children.Add(new TextBlock
            {
                Text = "No addons found. Ashita will be downloaded on first launch.",
                Style = (Style)FindResource("Mist.Caption"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0)
            });
        }
        else
        {
            foreach (var addon in addons)
            {
                var checkBox = new CheckBox
                {
                    Content = addon.Name,
                    IsChecked = _activeProfile.EnabledAddons.Contains(addon.Name),
                    Margin = new Thickness(0, 0, 0, 8),
                    Tag = addon
                };
                
                // If this is the fps addon and FpsCap is set, force it checked and disabled
                if (addon.Name.Equals("fps", StringComparison.OrdinalIgnoreCase) && _activeProfile.FpsCap > 0)
                {
                    checkBox.IsChecked = true;
                    checkBox.IsEnabled = false;
                    checkBox.ToolTip = "FPS addon is required when Frame Rate Cap is set in Profiles. Set to 'Default' to disable.";
                }
                else
                {
                    checkBox.Checked += AddonCheckBox_Changed;
                    checkBox.Unchecked += AddonCheckBox_Changed;
                }
                
                var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
                panel.Children.Add(checkBox);
                
                if (!string.IsNullOrWhiteSpace(addon.Description))
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = addon.Description,
                        Style = (Style)FindResource("Mist.Caption"),
                        Margin = new Thickness(24, 4, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    });
                }
                
                AddonsPanel.Children.Add(panel);
            }
        }
        
        AddonCountText.Text = $"({addons.Count} found)";

        // Scan and display plugins
        var plugins = _svc.AddonManager.ScanPlugins(ashitaDir);
        PluginsPanel.Children.Clear();
        
        if (!plugins.Any())
        {
            PluginsPanel.Children.Add(new TextBlock
            {
                Text = "No plugins found. Ashita will be downloaded on first launch.",
                Style = (Style)FindResource("Mist.Caption"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0)
            });
        }
        else
        {
            foreach (var plugin in plugins)
            {
                var checkBox = new CheckBox
                {
                    Content = plugin.Name,
                    IsChecked = _activeProfile.EnabledPlugins.Contains(plugin.Name),
                    Margin = new Thickness(0, 0, 0, 12),
                    Tag = plugin
                };
                checkBox.Checked += PluginCheckBox_Changed;
                checkBox.Unchecked += PluginCheckBox_Changed;
                
                PluginsPanel.Children.Add(checkBox);
            }
        }
        
        PluginCountText.Text = $"({plugins.Count} found)";
    }

    private void AddonCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_activeProfile == null) return;
        
        var checkBox = (CheckBox)sender;
        var addon = (AshitaAddonManager.AddonInfo)checkBox.Tag;

        if (checkBox.IsChecked == true)
        {
            if (!_activeProfile.EnabledAddons.Contains(addon.Name))
            {
                _activeProfile.EnabledAddons.Add(addon.Name);
                _svc.Logger.Write($"Enabled addon: {addon.Name}");
            }
            
            // Automatically enable "addons" plugin if any addon is checked
            if (!_activeProfile.EnabledPlugins.Contains("addons"))
            {
                _activeProfile.EnabledPlugins.Add("addons");
                _svc.Logger.Write("Auto-enabled 'addons' plugin (required for addons to work)");
                
                // Update addons plugin checkbox if visible
                RefreshAddonsPluginCheckbox();
            }
        }
        else
        {
            _activeProfile.EnabledAddons.Remove(addon.Name);
            _svc.Logger.Write($"Disabled addon: {addon.Name}");
            
            // If no addons are enabled, optionally remove addons plugin
            // (keeping it enabled is harmless, so we'll leave it)
        }

        // Ensure the profile in state is updated
        var profileInState = _state.Profiles.FirstOrDefault(p => p.Name == _activeProfile.Name);
        if (profileInState != null)
        {
            profileInState.EnabledAddons = _activeProfile.EnabledAddons;
            profileInState.EnabledPlugins = _activeProfile.EnabledPlugins;
        }

        _svc.StateStore.Save(_state);
        
        // Regenerate script immediately
        GenerateAshitaScript();
        
        _svc.Logger.Write($"Addon script regenerated for profile: {_activeProfile.Name}");
    }

    private void PluginCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_activeProfile == null) return;
        
        var checkBox = (CheckBox)sender;
        var plugin = (AshitaAddonManager.AddonInfo)checkBox.Tag;

        if (checkBox.IsChecked == true)
        {
            if (!_activeProfile.EnabledPlugins.Contains(plugin.Name))
            {
                _activeProfile.EnabledPlugins.Add(plugin.Name);
                _svc.Logger.Write($"Enabled plugin: {plugin.Name}");
            }
        }
        else
        {
            _activeProfile.EnabledPlugins.Remove(plugin.Name);
            _svc.Logger.Write($"Disabled plugin: {plugin.Name}");
        }

        // Ensure the profile in state is updated
        var profileInState = _state.Profiles.FirstOrDefault(p => p.Name == _activeProfile.Name);
        if (profileInState != null)
        {
            profileInState.EnabledPlugins = _activeProfile.EnabledPlugins;
        }

        _svc.StateStore.Save(_state);
        
        // Regenerate script immediately
        GenerateAshitaScript();
        
        _svc.Logger.Write($"Plugin script regenerated for profile: {_activeProfile.Name}");
    }
    
    private void RefreshAddonsPluginCheckbox()
    {
        if (_activeProfile == null) return;
        
        // Find the "addons" plugin checkbox and update its state
        foreach (var child in PluginsPanel.Children)
        {
            if (child is CheckBox checkbox && checkbox.Tag is AshitaAddonManager.AddonInfo plugin)
            {
                if (plugin.Name.Equals("addons", StringComparison.OrdinalIgnoreCase))
                {
                    checkbox.IsChecked = _activeProfile.EnabledPlugins.Contains("addons");
                    break;
                }
            }
        }
    }

    private void GenerateAshitaScript()
    {
        if (_activeProfile == null) return;

        try
        {
            var ashitaDir = Path.Combine(_svc.BaseDir, "runtime", "ashita");
            var scriptsDir = Path.Combine(ashitaDir, "scripts");
            Directory.CreateDirectory(scriptsDir);

            var scriptPath = Path.Combine(scriptsDir, "mistxi.txt");
            var script = _svc.AddonManager.GenerateAshitaScript(
                _activeProfile.EnabledAddons,
                _activeProfile.EnabledPlugins,
                _activeProfile.FpsCap
            );

            File.WriteAllText(scriptPath, script);
            _svc.Logger.Write($"Ashita script written to: {scriptPath}");
        }
        catch (Exception ex)
        {
            _svc.Logger.Write("Failed to generate Ashita script", ex);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadAddons();
    }
}
