// DawProjectBrowser.Desktop/Services/AssetInitializationService.cs

using System;
using System.IO;
using Avalonia.Platform;
using System.Linq; 

namespace DawProjectBrowser.Desktop.Services
{
    public static class AssetInitializationService
    {
        private const string AppName = "DawProjectBrowser"; // Used for the AppData sub-folder name
        private const string EmbeddedLogosSubPath = "Assets/DAWLogos";
        
        // NEW CONSTANT: Path to embedded theme files
        private const string EmbeddedThemesSubPath = "Assets/Themes"; 

        private const string AssemblyName = "DawProjectBrowser.Desktop";

        private static readonly string[] EmbeddedLogoFiles = {
            "ableton_live.png",
            "fl_studio.png",
            "logic_pro.png",
            "appIcon.ico" 
        };
        
        // NEW: List of default theme files to copy out (adjust names as per your project)
        private static readonly string[] EmbeddedThemeFiles = {
            "DefaultTheme.axaml", 
            "DarkTheme.axaml"
        };
        
        // Helper to get the user-specific, external assets root folder (AppData equivalent)
        private static string GetExternalAssetRootDirectory()
        {
            // Gets the correct OS application data path
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDir = Path.Combine(appData, AppName);
            return Path.Combine(appDir, "Assets");
        }
        
        public static void EnsureDefaultAssetsAreExternal()
        {
            string externalRoot = GetExternalAssetRootDirectory();
            string externalLogosDir = Path.Combine(externalRoot, "DAWLogos");
            string externalThemesDir = Path.Combine(externalRoot, "Themes");
            
            // 1. Create all necessary directories
            try
            {
                Directory.CreateDirectory(externalLogosDir);
                Directory.CreateDirectory(externalThemesDir);
                Directory.CreateDirectory(Path.Combine(externalThemesDir, "Custom"));
            }
            // FIX: The missing catch block to complete the try/catch structure
            catch (Exception ex)
            {
                Console.WriteLine($"[ASSET-INIT] ERROR creating AppData directories: {ex.Message}");
                // If directory creation fails (e.g., permissions), we stop the copy process.
                return; 
            }
            
            // 2. Loop through and copy LOGO files
            foreach (var fileName in EmbeddedLogoFiles)
            {
                string externalPath = Path.Combine(externalLogosDir, fileName);
                
                if (File.Exists(externalPath))
                {
                    continue;
                }

                string embeddedUriPath = $"avares://{AssemblyName}/{EmbeddedLogosSubPath}/{fileName}";
                
                try
                {
                    using (var embeddedStream = AssetLoader.Open(new Uri(embeddedUriPath)))
                    using (var fileStream = File.Create(externalPath))
                    {
                        embeddedStream.CopyTo(fileStream);
                        Console.WriteLine($"[ASSET-INIT] Copied default logo to: {externalPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ASSET-INIT] ERROR copying logo file {fileName}. Error: {ex.Message}");
                }
            }

            // 3. Loop through and copy THEME files
            foreach (var fileName in EmbeddedThemeFiles)
            {
                string externalPath = Path.Combine(externalThemesDir, fileName);

                if (File.Exists(externalPath))
                {
                    continue;
                }

                string embeddedUriPath = $"avares://{AssemblyName}/{EmbeddedThemesSubPath}/{fileName}";
                
                try
                {
                    using (var embeddedStream = AssetLoader.Open(new Uri(embeddedUriPath)))
                    using (var fileStream = File.Create(externalPath))
                    {
                        embeddedStream.CopyTo(fileStream);
                        Console.WriteLine($"[ASSET-INIT] Copied default theme file to: {externalPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ASSET-INIT] ERROR copying theme file {fileName}. Error: {ex.Message}");
                }
            }
        }
    }
}