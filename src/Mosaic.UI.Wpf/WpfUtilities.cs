/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Mosaic.UI.Wpf
{
    /// <summary>
    /// Miscellaneous WPF Utilities
    /// </summary>
    public static class WpfUtilities
    {
        /// <summary>
        /// Recursively searches for the FrameworkElement in the visual tree that has its DataContext equal to the specified object.
        /// </summary>
        /// <param name="parent">The starting element of the visual tree search.</param>
        /// <param name="obj">The object to search for in the DataContext of visual elements.</param>
        /// <returns>The FrameworkElement that holds the object, or null if not found.</returns>
        public static FrameworkElement? FindControlHoldingObject(this DependencyObject parent, object obj)
        {
            if (parent is FrameworkElement fe && fe.DataContext == obj)
            {
                return fe;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = child.FindControlHoldingObject(obj);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Open's an save file dialog.
        /// </summary>
        /// <param name="title">Default: Select File</param>
        /// <param name="filter">All files (*.*)|*.*</param>
        /// <param name="initialDirectory"></param>
        public static string? SaveFileRequest(string title = "Select File", string filter = "All files (*.*)|*.*", string? initialDirectory = null)
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter,
                Title = title,
                InitialDirectory = !string.IsNullOrWhiteSpace(initialDirectory) ? initialDirectory : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }

        /// <summary>
        /// Open's a open file dialog for a single file.
        /// </summary>
        /// <param name="title">Default: Select File</param>
        /// <param name="filter">All files (*.*)|*.*</param>
        /// <param name="initialDirectory"></param>
        /// <param name="validateNames">If the dialog should validate names and things like file locks.  The default is true.</param>
        public static string? OpenFileRequest(string title = "Select File", string filter = "All files (*.*)|*.*", string? initialDirectory = null, bool validateNames = true)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                InitialDirectory = !string.IsNullOrWhiteSpace(initialDirectory) ? initialDirectory : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                ValidateNames = validateNames
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }

        /// <summary>
        /// Open's a open file dialog for multiple files.
        /// </summary>
        /// <param name="title">Default: Select File</param>
        /// <param name="filter">All files (*.*)|*.*</param>
        /// <param name="initialDirectory"></param>
        public static string[]? OpenFilesRequest(string title = "Select File", string filter = "All files (*.*)|*.*", string? initialDirectory = null)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                Multiselect = true,
                InitialDirectory = !string.IsNullOrWhiteSpace(initialDirectory) ? initialDirectory : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileNames;
            }

            return null;
        }

        /// <summary>
        /// Opens a folder dialog browser.
        /// </summary>
        public static string? OpenFolderRequest()
        {
            using (var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                // Optional: Set the initial directory, description, etc.
                // folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
                folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyDocuments;
                folderBrowserDialog.Description = "Select a folder";
                folderBrowserDialog.UseDescriptionForTitle = true;

                // Show the FolderBrowserDialog.
                System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                {
                    // The user selected a folder and pressed the OK button.
                    return folderBrowserDialog.SelectedPath;
                }
                else
                {
                    // The user pressed the Cancel button or closed the dialog.
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns a BitmapSource from the icon registered to the current path.
        /// </summary>
        /// <param name="path"></param>
        public static BitmapSource? GetIconFromExePath(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using var icon = Icon.ExtractAssociatedIcon(path);

            return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        /// <summary>
        /// Simulates an action similar to the WinForms DoEvents().
        /// </summary>
        /// <remarks>Ya, I know.</remarks>
        public static void DoEvents()
        {
            _ = Application.Current?.Dispatcher?.Invoke(DispatcherPriority.Background, new Action(delegate
            {
                Task.Yield();
            }));
        }

        /// <summary>
        /// Simulates an action similar to the WinForms DoEvents().
        /// </summary>
        /// <remarks>Ya, I know.</remarks>
        public static async Task DoEventsAsync()
        {
            await Application.Current?.Dispatcher?.InvokeAsync(delegate
            {
                Task.Yield();
            }, DispatcherPriority.Background)!;
        }


        /// <summary>
        /// The number of processes running with the same name as this application.  One means only
        /// this instance is running.  This can be used to detect prevent multiple instances from
        /// running.
        /// </summary>
        public static int InstancesRunning()
        {
            var proc = Process.GetCurrentProcess();
            return Process.GetProcesses().Count(p => p.ProcessName == proc.ProcessName);
        }

        /// <summary>
        /// Executes an action if the user confirms the dialog that is presented.
        /// </summary>
        /// <param name="msg">The message/question to display to the user to confirm.</param>
        /// <param name="action">Action to execute if confirm is 'Yes'.</param>
        public static void Confirm(string msg, Action action)
        {
            var result = MessageBox.Show(msg, "Confirm", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.No || result == MessageBoxResult.Cancel)
            {
                return;
            }

            action.Invoke();
        }

        /// <summary>
        /// Converts a <see cref="Bitmap"/> to a <see cref="BitmapImage"/>
        /// </summary>
        /// <param name="bm"></param>
        public static BitmapImage BitmapToImageSource(Bitmap bm)
        {
            using (var memory = new MemoryStream())
            {
                bm.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = memory;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();

                return image;
            }
        }

        /// <summary>
        /// Attempts to resolve the property to bind based off of a provided bind property and type.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public static DependencyProperty? ResolveBindProperty(Type t, string bindProperty)
        {
            var dpField = t.GetField(bindProperty, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            if (dpField == null || dpField.FieldType != typeof(DependencyProperty))
            {
                throw new ArgumentException($"Could not find a DependencyProperty named '{bindProperty}' on type '{t.Name}'.", nameof(bindProperty));
            }

            return (DependencyProperty?)dpField.GetValue(null);
        }

        /// <summary>
        /// Creates a control of the specified type and binds a property to a bind path.
        /// </summary>
        /// <param name="controlType">The type of the control to create.</param>
        /// <param name="prop">The PropertyInfo of the property on the source object.</param>
        /// <param name="bindPath">The name of the bind property on the control to bind to.</param>
        /// <returns>A control setup to data bind a property.</returns>
        public static Control CreateControl(Type controlType, PropertyInfo prop, string bindPath)
        {
            return CreateControl(controlType, prop.Name, bindPath);
        }

        /// <summary>
        /// Creates a control of the specified type and binds a property to a bind path.
        /// </summary>
        /// <param name="controlType">The type of the control to create.</param>
        /// <param name="propertyName">The name of the property in the source object.</param>
        /// <param name="bindPath">The name of the bind property on the control to bind to.</param>
        /// <returns>A control setup to data bind a property.</returns>
        public static Control CreateControl(Type controlType, string propertyName, string bindPath)
        {
            var template = new ControlTemplate();
            var factory = new FrameworkElementFactory(controlType);
            var dp = ResolveBindProperty(controlType, bindPath);

            template.VisualTree = factory;

            if (dp != null)
            {
                factory.SetBinding(dp, new Binding(propertyName));
            }

            var ctrl = new Control
            {
                Template = template
            };

            return ctrl;
        }

    }
}