// DawProjectBrowser.Desktop/ViewModels/SettingsViewModel.cs

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DawProjectBrowser.Desktop.Services;
using DawProjectBrowser.Desktop.Views;

namespace DawProjectBrowser.Desktop.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ThemeManagerService _themeManagerService;
        private readonly SettingsWindow _settingsWindow;
        
        // Property bound to the TextBlock in the UI
        public string ThemeFolderPath { get; } = App.CustomThemesPath;

        // Collection bound to the ComboBox ItemsSource
        public ObservableCollection<string> AvailableThemes { get; } = new();

        // Property bound to the ComboBox SelectedItem
        [ObservableProperty]
        private string _selectedThemeName = "(Default)"; // Default selection


        // Command for the "Apply & Close" button
        public IRelayCommand ApplyAndCloseCommand { get; }

        // Command for the "Cancel" button
        public IRelayCommand CloseWindowCommand { get; }


        public SettingsViewModel(ThemeManagerService themeManagerService, SettingsWindow settingsWindow)
        {
            _themeManagerService = themeManagerService;
            _settingsWindow = settingsWindow;

            ApplyAndCloseCommand = new RelayCommand(ApplyThemeAndClose);
            CloseWindowCommand = new RelayCommand(CloseWindow);
            
            LoadAvailableThemes();
        }

        private void LoadAvailableThemes()
        {
            AvailableThemes.Clear();
            AvailableThemes.Add("(Default)"); // The first option is always to revert to the system/base theme

            try
            {
                // Find all .axaml files in the custom themes directory
                var themeFiles = Directory.EnumerateFiles(App.CustomThemesPath, "*.axaml", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileNameWithoutExtension) // Get just the file name without extension
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();
                
                foreach (var name in themeFiles)
                {
                    // FIX CS8604: Using the null-forgiving operator as we've already filtered out nulls/whitespace
                    AvailableThemes.Add(name!); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load custom themes: {ex.Message}");
            }
        }
        
        private void ApplyThemeAndClose()
        {
            if (SelectedThemeName == "(Default)")
            {
                // If default is selected, remove the custom theme
                _themeManagerService.RevertToDefaultTheme();
            }
            else
            {
                // Apply the selected custom theme
                string fullPath = Path.Combine(App.CustomThemesPath, SelectedThemeName + ".axaml");
                _themeManagerService.LoadAndApplyTheme(fullPath);
            }

            CloseWindow();
        }

        private void CloseWindow()
        {
            _settingsWindow.Close();
        }
    }
}