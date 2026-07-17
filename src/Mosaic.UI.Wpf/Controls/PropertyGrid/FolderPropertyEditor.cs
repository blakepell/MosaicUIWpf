using Microsoft.Win32;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A simple folder picker editor for string properties.
    /// Shows an OpenFolderDialog and returns the selected file path.
    /// If the dialog is cancelled, returns null (no change).
    /// </summary>
    public class FolderPropertyEditor : IPropertyEditor
    {
        /// <summary>
        /// Opens a folder dialog initialized with the current value (if a valid path).
        /// Returns the selected folder path, or null if the user cancels.
        /// </summary>
        /// <param name="currentValue">Current property value (expected string path).</param>
        /// <param name="propertyType">Target property type.</param>
        /// <param name="owner">Owner object (the object that owns the property).</param>
        /// <returns>New value (string path) or null to indicate no change.</returns>
        public object? Edit(object? currentValue, Type propertyType, object owner)
        {
            // Only support string properties for file path
            if (propertyType != typeof(string))
            {
                return null;
            }

            var dlg = new OpenFolderDialog
            {
                Multiselect = false
            };

            // If current value is a file path, initialize dialog
            if (currentValue is string s && !string.IsNullOrWhiteSpace(s))
            {
                try
                {
                    if (currentValue is string dir && Directory.Exists(dir))
                    {
                        dlg.InitialDirectory = dir;
                    }
                }
                catch
                {
                    // ignore malformed path
                }
            }

            // Try to find an active window to own the dialog for correct window parenting
            var ownerWindow = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                                  ?? Application.Current?.MainWindow;

            bool? result;

            try
            {
                result = ownerWindow != null ? dlg.ShowDialog(ownerWindow) : dlg.ShowDialog();
            }
            catch
            {
                // As a fallback use the parameterless call
                result = dlg.ShowDialog();
            }

            if (result == true)
            {
                return dlg.FolderName;
            }

            // Return null to indicate no change (dialog cancelled)
            return null;
        }
    }
}