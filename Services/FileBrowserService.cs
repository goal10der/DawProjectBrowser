// DawProjectBrowser.Desktop/Services/FileBrowserService.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using System.Reflection; // Needed for Assembly.GetExecutingAssembly
using DawProjectBrowser.Desktop.Models;

// CRITICAL FIXES: Required Avalonia usings for IBitmap loading
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
        
        /// <summary>
        /// Resolves the application asset path and returns the loaded IBitmap directly.
        /// </summary>
        private Bitmap? GetDawLogoBitmap(string dawType)
        {
            const string AssemblyName = "DawProjectBrowser.Desktop";
            // FINAL FIX: Strict lowercase slash notation
            const string BasePath = $"avares://{AssemblyName}/Assets/DAWLogos/"; 
            
            string normalizedType = dawType.Replace(" ", "_").ToLowerInvariant(); 
            string logoFileName;

            // Map the normalized type to the expected asset file name
            switch (normalizedType)
            {
                case "logic_pro":
                    logoFileName = "logic_pro.png";
                    break;
                case "fl_studio":
                    logoFileName = "fl_studio.png";
                    break;
                case "ableton_live":
                    logoFileName = "ableton_live.png";
                    break;
                default:
                    logoFileName = "appIcon.ico"; 
                    break;
            }

            string logoUri = BasePath + logoFileName;

            try
            {
                // CRITICAL: Manually force the resource stream to open and load the bitmap
                Uri uri = new Uri(logoUri);

                // This method forces immediate loading of the resource stream
                using (var stream = AssetLoader.Open(uri))
                {
                    Console.WriteLine($"[DEBUG-BITMAP-LOAD] Successfully opened stream for: {logoUri}");
                    return new Bitmap(stream);
                }
            }
            catch (Exception ex)
            {
                // Log detailed error if the asset cannot be opened or converted to a bitmap
                Console.WriteLine($"[ERROR-BITMAP-LOAD] Failed to load IBitmap for {logoUri}. Error: {ex.Message}");
                return null; // Return null on failure
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
                        
                        // CRITICAL: Call the new IBitmap loading method
                        Bitmap? logoBitmap = GetDawLogoBitmap(dawType); 

                        bool demoExists = !string.IsNullOrEmpty(demoClipPath);
                        
                        projects.Add(new DawProject
                        {
                            Name = demoName, 
                            FilePath = projectFilePath,
                            DawType = dawType,
                            DemoClipPath = demoClipPath,
                            // CRITICAL FIX: Assign the IBitmap to the new property name
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