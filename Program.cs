using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using DawProjectBrowser.Desktop.ViewModels;
using DawProjectBrowser.Desktop.Views;
using DawProjectBrowser.Desktop.Services; // CRITICAL: Need access to IAudioPlayer and the new service

namespace DawProjectBrowser.Desktop
{
    class Program
    {
        // Initialization code before Avalonia starts
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration setup
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .AfterSetup(OnSetup); 

        // This method runs once Avalonia is set up but before the main window is shown.
        private static void OnSetup(AppBuilder builder)
        {
            // Since the logic was moved to App.axaml.cs, this method is now redundant for DI setup.
            // Leave empty.
        }

        /// <summary>
        /// FIX: Adds the definition for the method that App.axaml.cs is calling.
        /// This centralizes the creation of the audio service.
        /// </summary>
        public static IAudioPlayer CreateAudioPlayer()
        {
            // CRITICAL FIX: Return the new OpenALPlayer instance
            return new AudioPlaybackService();
        }
    }
}