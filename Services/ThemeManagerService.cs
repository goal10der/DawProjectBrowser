// DawProjectBrowser.Desktop/Services/ThemeManagerService.cs (FINAL, REFLECTION-BASED BUILD FIX)

using System;
using System.IO;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls;
using System.Linq; 
using System.Reflection; // NEW: Essential for reflection

namespace DawProjectBrowser.Desktop.Services
{
    public class ThemeManagerService
    {
        private const string CustomThemeKey = "CustomThemeResourceDictionary";

        /// <summary>
        /// Loads a custom theme file from a path and applies it to the application's resources.
        /// Uses reflection to call the hidden/conflicting XAML parser method.
        /// </summary>
        /// <param name="themeFilePath">Absolute path to the user's custom .axaml file.</param>
        public void LoadAndApplyTheme(string themeFilePath)
        {
            if (Application.Current?.Resources == null)
            {
                Console.WriteLine("[ERROR] Application resources are not available. Cannot apply theme.");
                return;
            }

            if (!File.Exists(themeFilePath))
            {
                Console.WriteLine($"[ERROR] Custom theme file not found: {themeFilePath}");
                return;
            }
            
            if (!themeFilePath.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[ERROR] File must be a valid Avalonia XAML file (.axaml): {themeFilePath}");
                return;
            }

            try
            {
                // 1. Read the raw XAML content
                string xamlContent = File.ReadAllText(themeFilePath);

                // 2. Locate the static AvaloniaXamlLoader class (usually in Avalonia.Markup.Xaml)
                // We use reflection to find the method that the compiler is rejecting.
                Type? xamlLoaderType = Type.GetType("Avalonia.Markup.Xaml.AvaloniaXamlLoader, Avalonia.Markup.Xaml");
                
                if (xamlLoaderType == null)
                {
                    Console.WriteLine("[FATAL ERROR] Could not locate AvaloniaXamlLoader type via reflection.");
                    return;
                }
                
                // 3. Find the generic Parse<Styles>(string xaml) method
                MethodInfo? parseMethod = xamlLoaderType.GetMethod(
                    "Parse", 
                    BindingFlags.Public | BindingFlags.Static, 
                    null, 
                    new[] { typeof(string) }, // Match the single string argument
                    null
                )?.MakeGenericMethod(typeof(Styles)); // Make it generic for the Styles type

                if (parseMethod == null)
                {
                    Console.WriteLine("[FATAL ERROR] Could not locate the correct Parse<Styles>(string) method via reflection.");
                    return;
                }

                // 4. Invoke the method dynamically to parse the XAML content
                // The compiler can no longer complain about the method signature!
                object? parsedObject = parseMethod.Invoke(null, new object[] { xamlContent });

                if (parsedObject is not Styles newStyles)
                {
                    Console.WriteLine("[ERROR] Custom theme file content is not a valid Avalonia Styles object after parsing.");
                    return;
                }

                // 5. Apply the Styles (Remaining logic is sound)
                RevertToDefaultTheme(); 

                var customThemeResource = new ResourceDictionary();
                customThemeResource.MergedDictionaries.Add(newStyles);
                customThemeResource[CustomThemeKey] = true; 

                Application.Current.Resources.MergedDictionaries.Add(customThemeResource);

                Console.WriteLine($"[INFO] Successfully applied custom theme from: {themeFilePath}");
            }
            catch (Exception ex)
            {
                // Unpack TargetInvocationException to see the real error from the Parse method
                var innerEx = ex as TargetInvocationException;
                Console.WriteLine($"[ERROR] Failed to load or apply custom theme. Ensure the XAML file is valid: {(innerEx?.InnerException?.Message ?? ex.Message)}");
            }
        }
        
        /// <summary>
        /// Method to revert to the application's base theme by removing the custom theme.
        /// </summary>
        public void RevertToDefaultTheme()
        {
            if (Application.Current?.Resources == null) return;

            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
            
            for (int i = mergedDictionaries.Count - 1; i >= 0; i--)
            {
                var dictionary = mergedDictionaries[i];

                if (dictionary is ResourceDictionary rd && rd.TryGetValue(CustomThemeKey, out object? resourceValue))
                {
                    if (resourceValue is bool isCustomTheme && isCustomTheme)
                    {
                        mergedDictionaries.RemoveAt(i);
                        if (rd is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                        Console.WriteLine("[INFO] Removed existing custom theme.");
                        break; 
                    }
                }
            }
        }
    }
}