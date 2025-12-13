// DawProjectBrowser.Desktop/Views/SettingsWindow.axaml.cs

using Avalonia.Controls;
using DawProjectBrowser.Desktop.ViewModels;

namespace DawProjectBrowser.Desktop.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Method to set the DataContext dynamically when the window is launched from the main VM.
        /// </summary>
        public void SetViewModel(SettingsViewModel viewModel)
        {
            DataContext = viewModel;
        }
    }
}