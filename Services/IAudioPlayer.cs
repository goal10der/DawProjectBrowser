using System;
using System.Threading.Tasks;

namespace DawProjectBrowser.Desktop.Services
{
    // Interface for the rich audio player features
    public interface IAudioPlayer : IDisposable
    {
        // --- State Properties ---
        TimeSpan CurrentPosition { get; }
        TimeSpan CurrentDuration { get; }
        bool IsPaused { get; } 

        // --- Events to Notify ViewModel ---
        
        event EventHandler<TimeSpan> PositionUpdated; 
        event EventHandler PlaybackStopped;
        event EventHandler PlaybackResumed;
        event EventHandler PlaybackPaused;
        
        // --- Control Methods ---
        
        Task Play(string filePath); 
        void Pause();               
        void Resume();              
        void Stop();                
        void SetPosition(TimeSpan position); 
    }
}