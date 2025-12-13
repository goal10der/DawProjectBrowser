#nullable enable
using System;
using System.Diagnostics; 
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DawProjectBrowser.Desktop.Services
{
    // Now implementing the rich IAudioPlayer interface
    public class AudioPlaybackService : IAudioPlayer
    {
        private Process? currentPlaybackProcess;
        
        // Keep demo clips short - nobody wants to listen to a whole song
        private const int DEMO_DURATION_SECONDS = 5;

        // --- NEW: Implementation of Required IAudioPlayer Members (Dummy/Placeholder) ---
        
        // Properties (Return defaults, as ffplay cannot provide real-time data)
        public TimeSpan CurrentPosition => TimeSpan.Zero;
        public TimeSpan CurrentDuration => TimeSpan.FromSeconds(DEMO_DURATION_SECONDS); // A reasonable guess
        public bool IsPaused => false; // We treat this player as either Stopped or Playing
        
        // Events (Must be defined, even if never fired)
        public event EventHandler<TimeSpan>? PositionUpdated;
        public event EventHandler? PlaybackResumed;
        public event EventHandler? PlaybackPaused;
        
        // Renamed/Wrapped Methods
        public Task Play(string filePath) => PlayDemoClip(filePath); // Maps rich Play() to simple PlayDemoClip()
        public void Stop() => StopCurrentPlayback(); // Maps rich Stop() to simple StopCurrentPlayback()
        
        // Dummy Control Methods (ffplay cannot pause, resume, or seek)
        public void Pause() { /* Do nothing */ Console.WriteLine("[DEBUG] Pause requested, but unsupported by ffplay."); }
        public void Resume() { /* Do nothing */ Console.WriteLine("[DEBUG] Resume requested, but unsupported by ffplay."); }
        public void SetPosition(TimeSpan position) { /* Do nothing */ Console.WriteLine("[DEBUG] Seek requested, but unsupported by ffplay."); }
        // --- END: IAudioPlayer Members ---

        // The actual event you fire in your logic
        public event EventHandler? PlaybackStopped;


        public AudioPlaybackService()
        {
            Console.WriteLine("[AudioService] Initialized with OS native fallback approach");
        }

        // Renamed PlayDemoClip to be public and return Task to satisfy IAudioPlayer.Play() contract
        public Task PlayDemoClip(string filePath)
        {
            // Stop anything that might be playing first
            StopCurrentPlayback(); 

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[ERROR] Audio file missing: {filePath}");
                return Task.CompletedTask;
            }
            
            string fileExt = Path.GetExtension(filePath).ToLower();
            string playerCommand = "";
            string playerArgs = "";

            try
            {
                // Removed the redundant 'useFfplay' variable to resolve the compiler warning.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (fileExt == ".wav")
                    {
                        playerCommand = "powershell";
                        playerArgs = $"-c \"$player = New-Object Media.SoundPlayer '{filePath}'; $player.PlaySync();\"";
                    }
                    else
                    {
                        playerCommand = "ffplay.exe";
                        playerArgs = $"-nodisp -autoexit -t {DEMO_DURATION_SECONDS} \"{filePath}\"";
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (fileExt == ".wav" || fileExt == ".mp3" || fileExt == ".m4a")
                    {
                        playerCommand = "afplay";
                        playerArgs = $"-t {DEMO_DURATION_SECONDS} \"{filePath}\"";
                    }
                    else
                    {
                        playerCommand = "ffplay";
                        playerArgs = $"-nodisp -autoexit -t {DEMO_DURATION_SECONDS} \"{filePath}\"";
                    }
                }
                else 
                {
                    playerCommand = "ffplay";
                    playerArgs = $"-nodisp -autoexit -t {DEMO_DURATION_SECONDS} \"{filePath}\""; 
                }
                
                if (string.IsNullOrEmpty(playerCommand))
                {
                    Console.WriteLine("[ERROR] Couldn't figure out how to play audio on this platform");
                    return Task.CompletedTask;
                }

                currentPlaybackProcess = new Process();
                currentPlaybackProcess.StartInfo.FileName = playerCommand;
                currentPlaybackProcess.StartInfo.Arguments = playerArgs;
                currentPlaybackProcess.StartInfo.UseShellExecute = false;
                currentPlaybackProcess.StartInfo.RedirectStandardOutput = true;
                currentPlaybackProcess.StartInfo.RedirectStandardError = true;
                currentPlaybackProcess.StartInfo.CreateNoWindow = true;

                // Set up event to know when the demo is finished playing
                currentPlaybackProcess.EnableRaisingEvents = true;
                currentPlaybackProcess.Exited += (sender, e) =>
                {
                    PlaybackStopped?.Invoke(this, EventArgs.Empty);
                    Console.WriteLine("[DEBUG] Audio playback finished (Process Exited)");
                };
                
                currentPlaybackProcess.Start();
                Console.WriteLine($"[DEBUG] Playing: {Path.GetFileName(filePath)} using {playerCommand}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Playback failed: {ex.Message}");
                Console.WriteLine($"Make sure {playerCommand} is installed and accessible");
            }

            return Task.CompletedTask;
        }
        
        public void StopCurrentPlayback()
        {
            if (currentPlaybackProcess != null && !currentPlaybackProcess.HasExited)
            {
                try
                {
                    currentPlaybackProcess.Kill();
                    currentPlaybackProcess.WaitForExit(100);  
                    
                    // Manually invoke stopped event if the process was killed before natural end
                    PlaybackStopped?.Invoke(this, EventArgs.Empty); 
                    Console.WriteLine("[DEBUG] Audio playback stopped by user action");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] Had trouble stopping playback: {ex.Message}");
                }
            }
            
            // Clean up
            if (currentPlaybackProcess != null)
            {
                currentPlaybackProcess.Dispose();
                currentPlaybackProcess = null;
            }
        }
        
        // This method from the original service is kept, but it is not part of IAudioPlayer
        public void OpenFileInDefaultApp(string projectPath)
        {
             // ... implementation ...
        }

        public void Dispose()
        {
            StopCurrentPlayback();
            GC.SuppressFinalize(this);
        }
    }
}