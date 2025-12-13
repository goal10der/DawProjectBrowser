using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Avalonia.Media.Imaging; // IBitmap support - had to look this up!

namespace DawProjectBrowser.Desktop.Models
{
    public partial class DawProject : ObservableObject
    {
        // Basic project info
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string DawType { get; set; } = string.Empty;  // FL Studio, Ableton, etc.
        public string? DemoClipPath { get; set; }  // removed redundant = null

        // DAW logo handling - using backing field pattern here
        private Bitmap? dawLogoBitmap;

        public Bitmap? DawLogoPathBitmap
        {
            get 
            { 
                return dawLogoBitmap; 
            }
            set
            {
                bool changed = SetProperty(ref dawLogoBitmap, value);
                if (changed)
                {
                    // Keep this debug output for now - useful during development
                    Console.WriteLine($"Logo bitmap updated for {Name}: {(value != null ? "loaded" : "null")}");
                }
            }
        }

        // Audio playback state
        private bool isCurrentlyPlaying = false;  // explicit initialization

        public bool IsPlaying
        {
            get => isCurrentlyPlaying;
            set 
            {
                SetProperty(ref isCurrentlyPlaying, value);
                // Note: might want to add some event handling here later
            }
        }
    }
}