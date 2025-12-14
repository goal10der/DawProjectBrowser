#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Linq;

// Community Toolkit MVVM
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

// Avalonia
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;

// Project Namespaces
using DawProjectBrowser.Desktop.Models;
using DawProjectBrowser.Desktop.Services;
using DawProjectBrowser.Desktop.Views;

namespace DawProjectBrowser.Desktop.ViewModels
{
    public partial class ProjectListViewModel : ObservableObject, IDisposable
    {
        private readonly FileBrowserService _fileBrowserService = new();
        private readonly IAudioPlayer _audioPlayer;
        private readonly ThemeManagerService _themeManagerService = new(); // Instance managed here
        private StorageService? _storageService;

        // Path for storing persistence
        private static readonly string SettingsFilePath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                         "DawProjectBrowser", 
                         "last_folder.txt");

        // --- AUDIO UI PROPERTIES ---
        [ObservableProperty] private bool _isAudioPlaying;
        [ObservableProperty] private bool _isAudioPaused;
        
        // Duration Property - Updates the formatted string when set
        private TimeSpan _trackDuration = TimeSpan.Zero;
        public TimeSpan TrackDuration 
        {
            get => _trackDuration;
            set
            {
                if (SetProperty(ref _trackDuration, value))
                {
                    OnPropertyChanged(nameof(FormattedDuration));
                }
            }
        }
        
        // Position Property - Updates the formatted string when set
        private TimeSpan _trackPosition = TimeSpan.Zero;
        public TimeSpan TrackPosition
        {
            get => _trackPosition;
            set
            {
                if (SetProperty(ref _trackPosition, value))
                {
                    OnPropertyChanged(nameof(FormattedPosition));
                }
            }
        }
        
        // Formatted Read-Only Properties for the UI
        public string FormattedDuration => _trackDuration.ToString(@"m\:ss");
        public string FormattedPosition => _trackPosition.ToString(@"m\:ss");


        [ObservableProperty] private string _currentlyPlayingProject = "No Track Loaded";

        // --- MAIN PROPERTIES ---
        [ObservableProperty] 
        private string _currentProjectFolder = "Please click 'Browse Folder...' to begin.";
        
        public ObservableCollection<DawProject> Projects { get; } = new ObservableCollection<DawProject>();
        
        [ObservableProperty]
        private DawProject? _selectedProject;

        // --- COMMANDS ---
        public IAsyncRelayCommand BrowseFolderCommand { get; }
        public IAsyncRelayCommand<DawProject?> PlayDemoCommand { get; }
        public IRelayCommand<DawProject?> OpenProjectCommand { get; }
        public IRelayCommand OpenSettingsCommand { get; }
        public IRelayCommand StopDemoCommand { get; }
        public IRelayCommand PauseDemoCommand { get; }
        // The SeekCommand now takes a double (TotalSeconds) from the Slider Value
        public IRelayCommand<double> SeekCommand { get; } 

        // --- CONSTRUCTOR ---
        public ProjectListViewModel(IAudioPlayer audioPlayer)
        {
            _audioPlayer = audioPlayer;
            
            // Audio Events
            _audioPlayer.PositionUpdated += OnAudioPositionUpdated;
            _audioPlayer.PlaybackStopped += OnPlaybackStopped;
            _audioPlayer.PlaybackResumed += OnPlaybackResumed;
            _audioPlayer.PlaybackPaused += OnPlaybackPaused;

            // Command Init
            BrowseFolderCommand = new AsyncRelayCommand(BrowseFolder);
            PlayDemoCommand = new AsyncRelayCommand<DawProject?>(PlayOrResumeDemoAsync);
            OpenProjectCommand = new RelayCommand<DawProject?>(OpenProject);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            
            StopDemoCommand = new RelayCommand(StopPlayback);
            PauseDemoCommand = new RelayCommand(PausePlayback);
            SeekCommand = new RelayCommand<double>(SeekPlayback);
            
            LoadLastFolder();
        }

        // --- AUDIO EVENT HANDLERS ---
        private void OnAudioPositionUpdated(object? sender, TimeSpan position) => TrackPosition = position;

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

        // --- PLAYBACK LOGIC ---
        private async Task PlayOrResumeDemoAsync(DawProject? project)
        {
            // Case: Clicked main play/pause button (no specific project passed)
            if (project == null)
            {
                if (IsAudioPlaying) { _audioPlayer.Stop(); return; } // Assuming main button is stop/play
                if (_audioPlayer.IsPaused) { _audioPlayer.Resume(); return; }
                
                // Fallback: try to play selected
                project = SelectedProject;
                if (project == null || string.IsNullOrEmpty(project.DemoClipPath))
                {
                    Console.WriteLine("[DEBUG] No track selected to play.");
                    return;
                }
            }

            // Case: Playing a specific project (Card clicked or resuming Selected)
            SelectedProject = project;

            if (string.IsNullOrEmpty(project!.DemoClipPath)) return;
            
            _audioPlayer.Stop(); // Ensure stop before start
            await _audioPlayer.Play(project.DemoClipPath); 
            
            // Set Duration immediately after playing
            TrackDuration = _audioPlayer.CurrentDuration;
            TrackPosition = _audioPlayer.CurrentPosition;
            CurrentlyPlayingProject = project.Name;
            
            // UI state updates via Event Handlers, but set immediate intent here
            IsAudioPlaying = true;
            IsAudioPaused = false;
        }

        private void StopPlayback() => _audioPlayer.Stop();
        private void PausePlayback() => _audioPlayer.Pause();

        private void SeekPlayback(double totalSeconds)
        {
            // Convert the double value from the slider back to a TimeSpan
            TimeSpan newPosition = TimeSpan.FromSeconds(totalSeconds);
            
            _audioPlayer.SetPosition(newPosition);
            
            // Immediately update the UI property for instant feedback
            TrackPosition = newPosition; 
            
            Console.WriteLine($"[DEBUG] Audio seeking to: {TrackPosition.TotalSeconds:F1}s");
        }

        // --- STORAGE & SETTINGS ---
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
                    Directory.CreateDirectory(directory);
                
                File.WriteAllText(SettingsFilePath, path);
            }
            catch (Exception ex) { Console.WriteLine($"[ERROR] Failed to save settings: {ex.Message}"); }
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
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"[ERROR] Failed to load settings: {ex.Message}"); }
        }

        private async Task BrowseFolder()
        {
            if (_storageService == null) return;
            string? selectedPath = await _storageService.OpenFolderPickerAsync();

            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                CurrentProjectFolder = selectedPath;
                LoadProjects(selectedPath);
                SaveLastFolder(selectedPath);
            }
        }

        private void LoadProjects(string basePath)
        {
            Projects.Clear();
            var loadedProjects = _fileBrowserService.GetProjects(basePath);
            foreach (var project in loadedProjects) Projects.Add(project);
        }
        
        private void OpenProject(DawProject? project)
        {
            if (project == null || string.IsNullOrEmpty(project.FilePath)) return;

            StopPlayback();
            try
            {
                Process.Start(new ProcessStartInfo(project.FilePath) { UseShellExecute = true });
                Console.WriteLine($"[DEBUG] Opening DAW project: {project.FilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to launch project: {ex.Message}");
            }
        }
        
        // --- FIXED OPEN SETTINGS METHOD ---
        private void OpenSettings()
        {
            // 1. Create the Window
            var settingsWindow = new SettingsWindow();
            
            // 2. Create the Close Action the VM will use
            Action closeAction = settingsWindow.Close;
            
            // 3. Create the VM, passing the Service and the Close Action (Not the Window)
            var settingsViewModel = new SettingsViewModel(_themeManagerService, closeAction);
            
            // 4. Assign DataContext
            settingsWindow.DataContext = settingsViewModel;
            
            // 5. Show Window
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                settingsWindow.ShowDialog(desktop.MainWindow);
            }
            else
            {
                settingsWindow.Show();
            }
            Console.WriteLine("[DEBUG] Opened Settings Window.");
        }
        
        public void Dispose()
        {
            _audioPlayer.Dispose(); 
            GC.SuppressFinalize(this);
        }
    }
}