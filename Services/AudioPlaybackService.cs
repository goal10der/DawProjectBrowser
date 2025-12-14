#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using ManagedBass;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
// Removed: System.Collections.Generic is not necessary

namespace DawProjectBrowser.Desktop.Services
{
    // The IDisposable is implicitly required to clean up BASS
    public class AudioPlaybackService : IAudioPlayer, IDisposable
    {
        // --- FIELDS AND CONSTANTS ---
        private int _streamHandle;
        private bool _isPaused;
        // Removed: private const int DEMO_DURATION_SECONDS = 5;
        private readonly Timer? _positionTimer; 
        private bool _isDisposed = false;
        
        // Final known location of the native library on the user's disk
        private static string? _finalNativeLibraryPath;

        // --- IAudioPlayer Properties ---

        public TimeSpan CurrentPosition 
        {
            get 
            {
                if (_streamHandle == 0 || _isDisposed) return TimeSpan.Zero;
                long posBytes = Bass.ChannelGetPosition(_streamHandle);
                double seconds = Bass.ChannelBytes2Seconds(_streamHandle, posBytes);
                return TimeSpan.FromSeconds(seconds);
            }
        }

        public TimeSpan CurrentDuration 
        {
            get
            {
                if (_streamHandle == 0 || _isDisposed) return TimeSpan.Zero;
                long lenBytes = Bass.ChannelGetLength(_streamHandle);
                double seconds = Bass.ChannelBytes2Seconds(_streamHandle, lenBytes);
                return TimeSpan.FromSeconds(seconds);
            }
        }

        public bool IsPaused => _isPaused;

        // --- IAudioPlayer Events ---
        public event EventHandler<TimeSpan>? PositionUpdated;
        public event EventHandler? PlaybackResumed;
        public event EventHandler? PlaybackPaused;
        public event EventHandler? PlaybackStopped; // (The actual event you use)

        // --- Constructor and Initialization (Robust BASS Loading) ---

        public AudioPlaybackService()
        {
            // 1. Determine OS and expected file name
            string nativeFileName = GetPlatformNativeFileName();
            string appDataDirectory = GetLocalAppDataDirectory();
            
            _finalNativeLibraryPath = Path.Combine(appDataDirectory, nativeFileName);
            
            // 2. Copy the native library to the known AppData path
            if (!File.Exists(_finalNativeLibraryPath))
            {
                CopyNativeLibraryToAppData(nativeFileName, appDataDirectory);
            }
            
            // 3. Configure Resolver (Now points ONLY to the fixed AppData path)
            try
            {
                NativeLibrary.SetDllImportResolver(typeof(Bass).Assembly, BassNativeLibraryResolver);
                Console.WriteLine("[DEBUG] Custom NativeLibrary Resolver configured to use AppData path.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to configure native resolver: {ex.Message}");
            }

            // 4. Initialize BASS (The resolver will now load from the fixed AppData path)
            if (!Bass.Init(-1, 44100, DeviceInitFlags.Default, IntPtr.Zero))
            {
                Console.WriteLine($"[ERROR] BASS Init failed: {Bass.LastError} (Native library path: {_finalNativeLibraryPath})");
            }
            else
            {
                Console.WriteLine("[AudioService] Initialized with ManagedBass.");
                _positionTimer = new Timer(UpdatePosition, null, 0, 100);
            }
        }
        
        // --- Library Management Helpers ---

        private static string GetPlatformNativeFileName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "libbass.dylib";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "bass.dll";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "libbass.so";
            return "bass";
        }

        private static string GetLocalAppDataDirectory()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "DawProjectBrowser"
            );
            Directory.CreateDirectory(path);
            return path;
        }
        
        private static void CopyNativeLibraryToAppData(string nativeFileName, string appDataDirectory)
        {
            string sourcePath = Path.Combine(AppContext.BaseDirectory, nativeFileName);
            string destinationPath = Path.Combine(appDataDirectory, nativeFileName);
            
            // Fallback for when the extractor uses the simplified "bass.so" name (Linux specific)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                 string sourcePath_Bass = Path.Combine(AppContext.BaseDirectory, "bass.so");
                 if (File.Exists(sourcePath_Bass))
                 {
                     sourcePath = sourcePath_Bass;
                 }
            }

            if (File.Exists(sourcePath))
            {
                try
                {
                    File.Copy(sourcePath, destinationPath, true); 
                    Console.WriteLine($"[ASSET COPY] Copied native library from {sourcePath} to {destinationPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ASSET COPY ERROR] Failed to copy native library: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[ASSET COPY ERROR] Source file not found in extraction folder: {sourcePath}");
            }
        }

        // --- Native Library Resolver Logic (SIMPLIFIED) ---

        private static IntPtr BassNativeLibraryResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == "bass" && !string.IsNullOrEmpty(_finalNativeLibraryPath))
            {
                if (File.Exists(_finalNativeLibraryPath))
                {
                    Console.WriteLine($"[RESOLVER] SUCCESS: Loading fixed path: {_finalNativeLibraryPath}");
                    return NativeLibrary.Load(_finalNativeLibraryPath);
                }
                else
                {
                    Console.WriteLine($"[RESOLVER ERROR] Native library not found at fixed path: {_finalNativeLibraryPath}");
                }
            }
            return IntPtr.Zero; // Fall back to default resolution
        }

        // --- IAudioPlayer Interface Methods ---

        public Task Play(string filePath) => PlayDemoClip(filePath); 

        public void Stop() => StopCurrentPlayback(); 

        public void Pause()
        {
            if (_streamHandle != 0 && Bass.ChannelPause(_streamHandle))
            {
                _isPaused = true;
                PlaybackPaused?.Invoke(this, EventArgs.Empty);
                Console.WriteLine("[DEBUG] Playback paused.");
            }
        }

        public void Resume()
        {
            if (_streamHandle != 0 && _isPaused)
            {
                if (Bass.ChannelPlay(_streamHandle))
                {
                    _isPaused = false;
                    PlaybackResumed?.Invoke(this, EventArgs.Empty);
                    Console.WriteLine("[DEBUG] Playback resumed.");
                }
            }
        }

        public void SetPosition(TimeSpan position)
        {
            if (_streamHandle != 0)
            {
                long posBytes = Bass.ChannelSeconds2Bytes(_streamHandle, position.TotalSeconds);
                Bass.ChannelSetPosition(_streamHandle, posBytes);
                PositionUpdated?.Invoke(this, position);
            }
        }
        
        // --- Helper / BASS Logic Methods ---
        
        public Task PlayDemoClip(string filePath)
        {
            StopCurrentPlayback(); // Free previous stream

            if (!File.Exists(filePath)) 
            {
                Console.WriteLine($"[ERROR] Audio file missing: {filePath}");
                return Task.CompletedTask;
            }

            // Create a stream from the file
            _streamHandle = Bass.CreateStream(filePath, 0, 0, BassFlags.Default);

            if (_streamHandle == 0)
            {
                Console.WriteLine($"[ERROR] Could not create stream: {Bass.LastError}");
                return Task.CompletedTask;
            }

            // Play the stream
            Bass.ChannelPlay(_streamHandle);
            _isPaused = false;
            Console.WriteLine($"[DEBUG] Playing: {Path.GetFileName(filePath)}");

            // Setup "End Sync" (Event when audio finishes naturally)
            Bass.ChannelSetSync(_streamHandle, SyncFlags.End, 0, OnChannelEnd, IntPtr.Zero);

            // 🛑 REMOVED: The manual timer block that forced the 5-second stop.
            // The clip will now play to its natural end, triggering OnChannelEnd.

            return Task.CompletedTask;
        }

        private void OnChannelEnd(int handle, int channel, int data, IntPtr user)
        {
            if (channel == _streamHandle)
            {
                StopCurrentPlayback(isInternalCallback: true);
            }
        }
        
        public void StopCurrentPlayback(bool isInternalCallback = false)
        {
            if (_streamHandle != 0)
            {
                if (!isInternalCallback)
                {
                    Bass.ChannelStop(_streamHandle);
                    Bass.StreamFree(_streamHandle);
                }
                
                _streamHandle = 0;
                _isPaused = false;
                
                // Fire event
                PlaybackStopped?.Invoke(this, EventArgs.Empty);
                Console.WriteLine("[DEBUG] Playback stopped/freed.");
            }
        }
        
        private void UpdatePosition(object? state)
        {
            if (_streamHandle != 0 && !_isPaused)
            {
                PlaybackState playbackState = Bass.ChannelIsActive(_streamHandle);
                if (playbackState == PlaybackState.Playing)
                {
                    PositionUpdated?.Invoke(this, CurrentPosition);
                }
            }
        }

        // --- IDisposable Implementation ---

        public void Dispose()
        {
            if (_isDisposed) return;
            
            _isDisposed = true;
            _positionTimer?.Dispose();
            StopCurrentPlayback();
            Bass.Free(); // Frees the physical output device
            GC.SuppressFinalize(this);
            Console.WriteLine("[AudioService] Disposed and BASS freed.");
        }
    }
}