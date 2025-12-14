using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DawProjectBrowser.Desktop.Services;
using DawProjectBrowser.Desktop; // Required to access App.CustomThemesPath

namespace DawProjectBrowser.Desktop.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ThemeManagerService _themeManagerService;
        private readonly Action _closeWindowAction; // Stored action to close the UI
        
        // Property bound to the TextBlock in the UI
        public string ThemeFolderPath { get; } = App.CustomThemesPath;

        // Collection bound to the ComboBox ItemsSource
        public ObservableCollection<string> AvailableThemes { get; } = new();

        // Property bound to the ComboBox SelectedItem
        [ObservableProperty]
        private string _selectedThemeName = "(Default)";

        // Commands
        public IRelayCommand ApplyAndCloseCommand { get; }
        public IRelayCommand CloseWindowCommand { get; }

        // Constructor receives dependencies and the Close Action
        public SettingsViewModel(ThemeManagerService themeManagerService, Action closeWindowAction)
        {
            _themeManagerService = themeManagerService;
            _closeWindowAction = closeWindowAction;

            ApplyAndCloseCommand = new RelayCommand(ApplyThemeAndClose);
            CloseWindowCommand = new RelayCommand(CloseWindow);
            
            LoadAvailableThemes();
        }

        private void LoadAvailableThemes()
        {
            AvailableThemes.Clear();
            AvailableThemes.Add("(Default)");

            try
            {
                var themeFiles = Directory.EnumerateFiles(App.CustomThemesPath, "*.axaml", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();
                
                foreach (var name in themeFiles)
                {
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
                _themeManagerService.RevertToDefaultTheme();
            }
            else
            {
                string fullPath = Path.Combine(App.CustomThemesPath, SelectedThemeName + ".axaml");
                _themeManagerService.LoadAndApplyTheme(fullPath);
            }

            CloseWindow();
        }

        private void CloseWindow()
        {
            // Invoke the action passed during construction
            _closeWindowAction.Invoke();
        }
    }
}