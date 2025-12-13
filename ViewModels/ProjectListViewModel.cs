#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DawProjectBrowser.Desktop.Models;
using DawProjectBrowser.Desktop.Services; 
using DawProjectBrowser.Desktop.Views; 
using Avalonia; 
using Avalonia.Controls; 
using System.Diagnostics;
using System.IO; 
// Removed NAudio and System.Timers here, as the NAudioPlayer service handles them

namespace DawProjectBrowser.Desktop.ViewModels
{
    // Ensure IDisposable is kept for audio cleanup
    public partial class ProjectListViewModel : ObservableObject, IDisposable
    {
        private readonly FileBrowserService _fileBrowserService = new();
        
        private readonly IAudioPlayer _audioPlayer; 
        
        private readonly ThemeManagerService _themeManagerService = new(); 
        private StorageService? _storageService;
        
        private static readonly string SettingsFilePath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                         "DawProjectBrowser", 
                         "last_folder.txt");

        // --- NEW UI BINDING PROPERTIES FOR AUDIO BAR ---
        [ObservableProperty]
        private bool _isAudioPlaying; 
        
        [ObservableProperty] 
        private bool _isAudioPaused; 

        [ObservableProperty]
        private TimeSpan _trackDuration = TimeSpan.Zero; 
        
        [ObservableProperty]
        private TimeSpan _trackPosition = TimeSpan.Zero; 
        
        [ObservableProperty]
        private string _currentlyPlayingProject = "No Track Loaded";
        // --------------------------------------------------

        [ObservableProperty]
        private string _currentProjectFolder = "Please click 'Browse Folder...' to begin.";
        
        public ObservableCollection<DawProject> Projects { get; } = new ObservableCollection<DawProject>();
        
        private DawProject? _selectedProject;
        public DawProject? SelectedProject
        {
            get => _selectedProject;
            set => SetProperty(ref _selectedProject, value);
        }

        // Commands (actions)
        public IAsyncRelayCommand BrowseFolderCommand { get; }
        public IAsyncRelayCommand<DawProject?> PlayDemoCommand { get; } 
        public IRelayCommand<DawProject?> OpenProjectCommand { get; }
        public IRelayCommand OpenSettingsCommand { get; }
        
        public IRelayCommand StopDemoCommand { get; } 
        public IRelayCommand PauseDemoCommand { get; } 
        public IRelayCommand<double> SeekCommand { get; } 


        // Constructor: Now accepts the IAudioPlayer
        public ProjectListViewModel(IAudioPlayer audioPlayer)
        {
            _audioPlayer = audioPlayer;
            
            // Subscribe to the audio player's events to update UI bindings (These lines are now valid again)
            _audioPlayer.PositionUpdated += OnAudioPositionUpdated;
            _audioPlayer.PlaybackStopped += OnPlaybackStopped;
            _audioPlayer.PlaybackResumed += OnPlaybackResumed;
            _audioPlayer.PlaybackPaused += OnPlaybackPaused;


            // Initialize commands
            BrowseFolderCommand = new AsyncRelayCommand(BrowseFolder);
            PlayDemoCommand = new AsyncRelayCommand<DawProject?>(PlayOrResumeDemoAsync); 
            OpenProjectCommand = new RelayCommand<DawProject?>(OpenProject);
            OpenSettingsCommand = new RelayCommand(OpenSettings); 
            
            // Initialize NEW control commands
            StopDemoCommand = new RelayCommand(StopPlayback);
            PauseDemoCommand = new RelayCommand(PausePlayback);
            
            SeekCommand = new RelayCommand<double>(SeekPlayback); 
            
            LoadLastFolder(); // Called in constructor
        }

        // --- Audio Event Handlers ---
        
        private void OnAudioPositionUpdated(object? sender, TimeSpan position)
        {
            TrackPosition = position;
        }

        private void OnPlaybackStopped(object? sender, EventArgs e)
        {
            IsAudioPlaying = false;
            IsAudioPaused = false;
            TrackPosition = TimeSpan.Zero;
            TrackDuration = TimeSpan.Zero;
            CurrentlyPlayingProject = "No Track Loaded";
            Console.WriteLine("[DEBUG] Audio playback stopped/finished.");
        }
        
        private void OnPlaybackPaused(object? sender, EventArgs e)
        {
            IsAudioPlaying = false;
            IsAudioPaused = true;
            Console.WriteLine("[DEBUG] Audio playback paused.");
        }
        
        private void OnPlaybackResumed(object? sender, EventArgs e)
        {
            IsAudioPlaying = true;
            IsAudioPaused = false;
            Console.WriteLine("[DEBUG] Audio playback resumed.");
        }

        // --- Core Playback Logic (Called by Commands) ---

        // This method handles both initial play (from card) and the toggle logic (from the central button)
        private async Task PlayOrResumeDemoAsync(DawProject? project)
        {
            // If project is null, the command was called from the main player bar (the circular button)
            if (project == null)
            {
                // Case 1: Called from central button AND audio is currently playing -> STOP IT (The square icon)
                if (IsAudioPlaying) 
                {
                    _audioPlayer.Stop(); // Calling Stop will trigger OnPlaybackStopped, updating IsAudioPlaying to false
                    return;
                }

                // Case 2: Called from central button AND audio is paused -> RESUME.
                if (_audioPlayer.IsPaused)
                {
                    _audioPlayer.Resume(); // Calling Resume will trigger OnPlaybackResumed, updating IsAudioPlaying to true
                    return;
                }
                
                // Case 3: Called from central button AND stopped (The play icon is showing).
                // We need a file to play. Use the last selected file if available.
                project = SelectedProject;

                if (project == null || string.IsNullOrEmpty(project.DemoClipPath))
                {
                    Console.WriteLine("[DEBUG] Central button clicked but no track selected or playable.");
                    return;
                }
                // Fall through to play the last selected project.
            }

            // --- Logic for playing a new or resumed track ---

            // A new project was selected from a card OR we resumed a stopped track.
            SelectedProject = project;

            if (string.IsNullOrEmpty(project!.DemoClipPath)) return;
            
            // Stop any currently playing track before starting a new one (important when clicking a new card)
            _audioPlayer.Stop();
            
            await _audioPlayer.Play(project.DemoClipPath); 
            
            // Update UI properties based on the loaded file
            TrackDuration = _audioPlayer.CurrentDuration;
            TrackPosition = _audioPlayer.CurrentPosition;
            CurrentlyPlayingProject = project.Name;
            
            // State will be updated by OnPlaybackResumed, but set initial state
            IsAudioPlaying = true;
            IsAudioPaused = false;
        }

        private void StopPlayback()
        {
            _audioPlayer.Stop();
        }
        
        private void PausePlayback()
        {
            _audioPlayer.Pause();
        }
        
        // CRITICAL: Now correctly implements seeking based on the double value from the slider
        private void SeekPlayback(double totalSeconds)
        {
            // Convert slider seconds back to a TimeSpan
            TimeSpan newPosition = TimeSpan.FromSeconds(totalSeconds);
            
            _audioPlayer.SetPosition(newPosition);
            
            // Immediately update the UI to reflect the new position
            TrackPosition = newPosition; 
            
            Console.WriteLine($"[DEBUG] Audio seeking to: {TrackPosition.TotalSeconds:F1}s");
        }
        
        // --- Existing Command/Service Handlers ---

        /// <summary>
        /// Called by the MainWindow (via Program.cs) to give the ViewModel access to OS dialogs.
        /// </summary>
        public void SetStorageService(IStorageProvider storageProvider)
        {
            _storageService = new StorageService(storageProvider);
        }
        
        private void SaveLastFolder(string path)
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(SettingsFilePath, path);
                Console.WriteLine($"[DEBUG-SETTINGS] Saved last folder: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR-SETTINGS] Failed to save folder: {ex.Message}");
            }
        }

        private void LoadLastFolder()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string lastPath = File.ReadAllText(SettingsFilePath).Trim();
                    if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
                    {
                        CurrentProjectFolder = lastPath;
                        LoadProjects(lastPath);
                        Console.WriteLine($"[DEBUG-SETTINGS] Loaded last folder: {lastPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR-SETTINGS] Failed to load folder: {ex.Message}");
            }
        }

        private async Task BrowseFolder()
        {
            if (_storageService == null)
            {
                Console.WriteLine("[ERROR] Storage Service not initialized.");
                return;
            }

            string? selectedPath = await _storageService.OpenFolderPickerAsync();

            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                CurrentProjectFolder = selectedPath;
                LoadProjects(selectedPath);
                
                SaveLastFolder(selectedPath); // Save after loading
            }
        }

        private void LoadProjects(string basePath)
        {
            Projects.Clear();
            var loadedProjects = _fileBrowserService.GetProjects(basePath);

            foreach (var project in loadedProjects)
            {
                Projects.Add(project);
            }
        }
        
        private void OpenProject(DawProject? project)
        {
            Console.WriteLine($"[DEBUG-PATH] Logo path being loaded: {project?.DawLogoPathBitmap}");

            if (project == null || string.IsNullOrEmpty(project.FilePath)) 
            {
                Console.WriteLine("[ERROR] Cannot open project: Project or FilePath is missing.");
                return;
            }

            StopPlayback(); 
            
            try
            {
                var startInfo = new ProcessStartInfo(project.FilePath)
                {
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                
                Console.WriteLine($"[DEBUG] Attempting to open DAW project: {project.FilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to launch project file: {project.FilePath}. Exception: {ex.Message}");
            }
        }
        
        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow();
            
            var settingsViewModel = new SettingsViewModel(_themeManagerService, settingsWindow);
            
            settingsWindow.SetViewModel(settingsViewModel);
            
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                settingsWindow.ShowDialog((Window)desktop.MainWindow);
            }
            else
            {
                settingsWindow.Show();
            }
            Console.WriteLine("[DEBUG] Opened Settings Window.");
        }
        
        // Dispose method implementation
        public void Dispose()
        {
            _audioPlayer.Dispose(); 
            GC.SuppressFinalize(this);
            Console.WriteLine("[DEBUG] ViewModel Disposed (Audio cleanup complete).");
        }
    }
}