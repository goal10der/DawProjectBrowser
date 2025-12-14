using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DawProjectBrowser.Desktop.ViewModels;
using DawProjectBrowser.Desktop.Views;
using DawProjectBrowser.Desktop.Services;
using System.IO;
using System;
using Avalonia.Themes;
using System.Reflection; 

namespace DawProjectBrowser.Desktop
{
    public partial class App : Application
    {
        // FIX 1: Define the CustomThemesPath using the AppData equivalent
        public static readonly string CustomThemesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "DawProjectBrowser", 
            "Assets", 
            "Themes", 
            "Custom"
        );

        public override void Initialize()
        {
            // This line is essential for cross-platform XAML behavior resolution.
            // It uses the BehaviorService class found in the Avalonia.Xaml.Interactions namespace.
            
            
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // NEW 1: Initialize Assets before anything tries to load them.
            AssetInitializationService.EnsureDefaultAssetsAreExternal();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 1. Create the Audio Player instance (DI setup)
                IAudioPlayer audioPlayer = Program.CreateAudioPlayer(); 

                // 2. Create the Main ViewModel, injecting the Dependency
                var viewModel = new ProjectListViewModel(audioPlayer);

                // 3. Create and assign the Main Window
                var mainWindow = new MainWindow
                {
                    DataContext = viewModel,
                };

                // 4. Set the storage service using the MainWindow's provider
                if (mainWindow.StorageProvider != null)
                {
                    // Check for StorageProvider availability before using it.
                    viewModel.SetStorageService(mainWindow.StorageProvider);
                }
                
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}