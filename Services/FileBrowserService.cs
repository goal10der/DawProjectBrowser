// DawProjectBrowser.Desktop/Services/FileBrowserService.cs

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using DawProjectBrowser.Desktop.Models;

// Required Avalonia usings for IBitmap loading
using Avalonia.Platform;
using Avalonia.Media.Imaging;

namespace DawProjectBrowser.Desktop.Services 
{
    public class FileBrowserService
    {
        // Define which file extension relates to which DAW type. 
        private readonly Dictionary<string, string> _dawDefinitions = new()
        {
            { ".logicx", "Logic Pro" },
            { ".flp", "FL Studio" }, 
            { ".als", "Ableton Live" }
        };

        private readonly List<string> _audioExtensions = new List<string> { ".mp3", ".wav", ".flac", ".m4a" };
        private readonly List<string> _excludedFolderNames = new List<string> { "backup", "backups", ".git", ".svn", "render" };

        private string? FindMostRecentDemo(string projectDirectory)
        {
            try
            {
                var allFiles = Directory.GetFiles(projectDirectory, "*.*");
                var recentAudioFile = allFiles
                    .Where(filePath => _audioExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant()))
                    .Select(filePath => new FileInfo(filePath))
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                return recentAudioFile?.FullName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to find recent demo in {projectDirectory}: {ex.Message}");
                return null;
            }
        }
        
        // NEW: Helper to get the user-specific, external logos path (AppData equivalent)
        private static string GetExternalLogosDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDir = Path.Combine(appData, "DawProjectBrowser");
            
            // The path where the external logos will be found: [AppData]/DawProjectBrowser/Assets/DAWLogos
            return Path.Combine(appDir, "Assets", "DAWLogos");
        }

        /// <summary>
        /// Loads the DAW logo bitmap, prioritizing the external file system 
        /// (AppData folder) for customization, and falling back 
        /// to the embedded resource for first run/defaults.
        /// </summary>
        private Bitmap? GetDawLogoBitmap(string dawType)
        {
            // 1. Determine the logo file name
            string normalizedType = dawType.Replace(" ", "_").ToLowerInvariant();
            string logoFileName;
            switch (normalizedType)
            {
                case "logic_pro":
                case "fl_studio":
                case "ableton_live":
                    logoFileName = $"{normalizedType}.png";
                    break;
                default:
                    logoFileName = "appIcon.ico"; 
                    break;
            }

            // 2. --- CHECK EXTERNAL FILE SYSTEM FIRST (AppData) ---
            string externalPath = Path.Combine(GetExternalLogosDirectory(), logoFileName);

            if (File.Exists(externalPath))
            {
                try
                {
                    Console.WriteLine($"[DEBUG-BITMAP-LOAD] Loading logo from external file: {externalPath}");
                    using (var stream = File.OpenRead(externalPath))
                    {
                        return new Bitmap(stream);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR-BITMAP-LOAD] Failed to load external logo at {externalPath}. Falling back. Error: {ex.Message}");
                }
            }
            
            // 3. --- FALLBACK TO EMBEDDED RESOURCE (avares://) ---
            const string AssemblyName = "DawProjectBrowser.Desktop";
            // NOTE: The embedded resource path must match the folder structure exactly
            const string BasePath = $"avares://{AssemblyName}/Assets/DAWLogos/";
            string logoUri = BasePath + logoFileName;

            try
            {
                Uri uri = new Uri(logoUri);
                using (var stream = AssetLoader.Open(uri))
                {
                    Console.WriteLine($"[DEBUG-BITMAP-LOAD] Loading logo from embedded resource: {logoUri}");
                    return new Bitmap(stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR-BITMAP-LOAD] Failed to load embedded logo for {logoUri}. Error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Recursively scans a base path for DAW project files and associates them with a demo clip.
        /// </summary>
        public List<DawProject> GetProjects(string basePath) 
        {
            var projects = new List<DawProject>();
            
            if (!Directory.Exists(basePath))
            {
                Console.WriteLine($"[DEBUG] ERROR: Base path does not exist: {basePath}");
                return projects; 
            }
            
            Console.WriteLine($"[DEBUG] Starting RECURSIVE scan in base path: {basePath}"); 

            try
            {
                var allFiles = Directory.GetFiles(
                    path: basePath, 
                    searchPattern: "*.*", 
                    searchOption: SearchOption.AllDirectories
                );
                
                Console.WriteLine($"[DEBUG] Found a total of {allFiles.Length} files to check.");

                foreach (string projectFilePath in allFiles)
                {
                    string lowerCasePath = projectFilePath.ToLowerInvariant();
                    
                    bool isExcluded = _excludedFolderNames.Any(folder => lowerCasePath.Contains(Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar));

                    if (isExcluded)
                    {
                        continue;
                    }
                    
                    string fileExtension = Path.GetExtension(projectFilePath).ToLowerInvariant();
                    
                    if (_dawDefinitions.TryGetValue(fileExtension, out string? dawType))
                    {
                        string projectDirectory = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
                        string demoName = Path.GetFileNameWithoutExtension(projectFilePath);
                        
                        string? demoClipPath = FindMostRecentDemo(projectDirectory);
                        
                        Bitmap? logoBitmap = GetDawLogoBitmap(dawType); 

                        bool demoExists = !string.IsNullOrEmpty(demoClipPath);
                        
                        projects.Add(new DawProject
                        {
                            Name = demoName, 
                            FilePath = projectFilePath,
                            DawType = dawType,
                            DemoClipPath = demoClipPath,
                            DawLogoPathBitmap = logoBitmap 
                        });
                        
                        Console.WriteLine($"  -> Loaded: {demoName} ({dawType}). Demo: {(demoExists ? Path.GetFileName(demoClipPath) : "Not Found")}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"[ERROR] Access denied while scanning: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Unexpected scanning error: {ex.Message}");
            }
            
            Console.WriteLine($"[DEBUG] Scan complete. Total projects loaded: {projects.Count}"); 
            return projects; 
        }
    }
}