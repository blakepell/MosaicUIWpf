/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A TextBlock that displays an assembly version.
    /// </summary>
    public class VersionTextBlock : TextBlock
    {
        static VersionTextBlock()
        {
            // Ensure default style key if styles are added later
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VersionTextBlock), new FrameworkPropertyMetadata(typeof(VersionTextBlock)));
        }

        /// <summary>
        /// A TextBlock that displays an assembly version.
        /// </summary>
        public VersionTextBlock()
        {
            Loaded += (s, e) => UpdateText();
        }

        /// <summary>
        /// Specifies the source of the assembly to retrieve the version from.
        /// </summary>
        public enum AssemblySourceKind
        {
            /// <summary>
            /// Use the assembly that contains this control type (the assembly where
            /// <see cref="VersionTextBlock"/> is defined).
            /// </summary>
            ThisAssembly,

            /// <summary>
            /// Use the assembly that contains the currently executing code.
            /// </summary>
            ExecutingAssembly,

            /// <summary>
            /// Use the assembly of the method that called the current method.
            /// </summary>
            CallingAssembly,

            /// <summary>
            /// Use the process entry assembly (the assembly containing the application entry point).
            /// If the entry assembly is null, the control's assembly will be used as a fallback.
            /// </summary>
            EntryAssembly,

            /// <summary>
            /// Locate the assembly by the value of the <see cref="AssemblyName"/> property.
            /// The value can be either the simple name (e.g. "MyLib") or the full assembly name.
            /// </summary>
            ByName
        }

        /// <summary>
        /// Identifies the <see cref="AssemblySource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AssemblySourceProperty = DependencyProperty.Register(
            nameof(AssemblySource), typeof(AssemblySourceKind), typeof(VersionTextBlock), new PropertyMetadata(AssemblySourceKind.ThisAssembly, OnAssemblySourceChanged));

        /// <summary>
        /// Gets or sets the source of the assembly to retrieve the version from.
        /// </summary>
        public AssemblySourceKind AssemblySource
        {
            get => (AssemblySourceKind)GetValue(AssemblySourceProperty);
            set => SetValue(AssemblySourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AssemblyName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AssemblyNameProperty = DependencyProperty.Register(
            nameof(AssemblyName), typeof(string), typeof(VersionTextBlock), new PropertyMetadata(null, OnAssemblyNameChanged));

        /// <summary>
        /// Gets or sets the assembly name to use when <see cref="AssemblySource"/> is set to <see cref="AssemblySourceKind.ByName"/>.
        /// </summary>
        public string AssemblyName
        {
            get => (string)GetValue(AssemblyNameProperty);
            set => SetValue(AssemblyNameProperty, value);
        }

        /// <summary>
        /// Updates the text when the AssemblySource property changes.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnAssemblySourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VersionTextBlock vb)
            {
                vb.UpdateText();
            }
        }

        /// <summary>
        /// Updates the text when the AssemblyName property changes.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnAssemblyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VersionTextBlock vb)
            {
                vb.UpdateText();
            }
        }

        /// <summary>
        /// Updates the Text property with the version of the specified assembly.
        /// </summary>
        private void UpdateText()
        {
            try
            {
                var asm = ResolveAssembly();
                var version = asm?.GetName().Version?.ToString() ?? string.Empty;
                Text = version;
            }
            catch
            {
                Text = string.Empty;
            }
        }

        /// <summary>
        /// Resolves the assembly based on the current <see cref="AssemblySource"/> and <see cref="AssemblyName"/> properties.
        /// </summary>
        /// <returns></returns>
        private Assembly? ResolveAssembly()
        {
            switch (AssemblySource)
            {
                case AssemblySourceKind.ThisAssembly:
                    return Assembly.GetAssembly(typeof(VersionTextBlock));
                case AssemblySourceKind.ExecutingAssembly:
                    return Assembly.GetExecutingAssembly();
                case AssemblySourceKind.CallingAssembly:
                    return Assembly.GetCallingAssembly();
                case AssemblySourceKind.EntryAssembly:
                    return Assembly.GetEntryAssembly() ?? Assembly.GetAssembly(typeof(VersionTextBlock));
                case AssemblySourceKind.ByName:
                    if (string.IsNullOrWhiteSpace(AssemblyName))
                    {
                        return null;
                    }

                    // Try loaded assemblies first
                    var found = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => string.Equals(a.GetName().Name, AssemblyName, StringComparison.OrdinalIgnoreCase)
                                             || string.Equals(a.GetName().FullName, AssemblyName, StringComparison.OrdinalIgnoreCase));
                    if (found != null)
                    {
                        return found;
                    }

                    // Try to load by name
                    try
                    {
                        return Assembly.Load(new AssemblyName(AssemblyName));
                    }
                    catch
                    {
                        return null;
                    }
                default:
                    return null;
            }
        }
    }
}
