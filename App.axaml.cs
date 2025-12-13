using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DawProjectBrowser.Desktop.ViewModels;
using DawProjectBrowser.Desktop.Views;
using DawProjectBrowser.Desktop.Services; // Required for IAudioPlayer
using System.IO;
using System;
using Avalonia.Themes;

namespace DawProjectBrowser.Desktop
{
    public partial class App : Application
    {
        // FIX: Add the missing static definition for CustomThemesPath
        public static readonly string CustomThemesPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Themes", "Custom");

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 1. Create the Audio Player instance (DI setup)
                // This line now REQUIRES Program.CreateAudioPlayer() to be defined.
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
                    viewModel.SetStorageService(mainWindow.StorageProvider);
                }
                
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}