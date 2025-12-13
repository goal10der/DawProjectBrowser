#nullable enable
using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DawProjectBrowser.Desktop.ViewModels;
using DawProjectBrowser.Desktop.Models; 

namespace DawProjectBrowser.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Add handler to safely dispose the ViewModel when the window closes
            this.Closing += (sender, e) =>
            {
                // Safely call Dispose() on the ViewModel if it implements IDisposable
                (this.DataContext as IDisposable)?.Dispose();
                System.Console.WriteLine("[DEBUG] ViewModel Disposed (Audio cleanup complete).");
            };
        }

        // NOTE: The compiler generates the body for InitializeComponent(). 
        // If this method in the code-behind file has a 'throw new NotImplementedException()', 
        // you MUST REMOVE IT or uncomment it (if the original file had it commented out)
        // to allow the compiler to wire up the XAML components.
        // For a clean implementation, simply remove the method if it was copied incorrectly.
        // Assuming you should remove the entire body (including the throw) or the method itself
        // if it was added manually.

        // REMOVED: The Slider_PointerReleased event handler is gone.
        
        // Event handler for the clickable DAW image button.
        private void ProjectCardButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DawProject clickedProject)
            {
                if (DataContext is ProjectListViewModel viewModel)
                {
                    viewModel.OpenProjectCommand.Execute(clickedProject); 
                    System.Console.WriteLine($"[CODE-BEHIND] Command executed for: {clickedProject.Name}");
                }
            }
        }
    }
}