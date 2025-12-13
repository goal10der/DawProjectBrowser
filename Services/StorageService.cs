// DawProjectBrowser.Desktop/Services/StorageService.cs

// FIX: Use the correct core Avalonia namespace for storage components
using Avalonia.Platform.Storage; 
using System.Threading.Tasks;

namespace DawProjectBrowser.Desktop.Services
{
    public class StorageService
    {
        private readonly IStorageProvider _storageProvider;

        public StorageService(IStorageProvider storageProvider)
        {
            _storageProvider = storageProvider;
        }

        public async Task<string?> OpenFolderPickerAsync()
        {
            var options = new FolderPickerOpenOptions
            {
                Title = "Select DAW Projects Root Folder",
                AllowMultiple = false
            };
            
            var folders = await _storageProvider.OpenFolderPickerAsync(options);

            if (folders != null && folders.Count > 0)
            {
                // TryGetLocalPath is the correct way to get the path on Arch Linux/Windows/Mac
                return folders[0].TryGetLocalPath();
            }

            return null;
        }
    }
}