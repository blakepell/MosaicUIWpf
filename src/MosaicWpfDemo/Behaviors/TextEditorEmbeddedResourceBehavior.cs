/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.IO;
using System.Windows;
using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;

namespace Mosaic.UI.Wpf.Controls.Behaviors
{
    /// <summary>
    /// A behavior that loads the contents of an embedded resource into a <see cref="System.Windows.Controls.TextBox"/>.
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Example usage in XAML:
    ///
    /// <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    ///         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    ///         xmlns:i="http://schemas.microsoft.com/xaml/behaviors"
    ///         xmlns:behaviors="clr-namespace:Mosaic.UI.Wpf.Controls.Behaviors">
    ///     <TextBox>
    ///         <i:Interaction.Behaviors>
    ///             <behaviors:TextBoxEmbeddedResourceBehavior ResourcePath="YourNamespace.Resources.YourFile.txt" />
    ///         </i:Interaction.Behaviors>
    ///     </TextBox>
    /// </Window>
    /// ]]>
    /// </remarks>
    public class TextEditorEmbeddedResourceBehavior : Behavior<TextEditor>
    {
        public static readonly DependencyProperty ResourcePathProperty =
            DependencyProperty.Register(
                nameof(ResourcePath),
                typeof(string),
                typeof(TextEditorEmbeddedResourceBehavior),
                new PropertyMetadata(null, OnResourcePathChanged));

        public string ResourcePath
        {
            get => (string)GetValue(ResourcePathProperty);
            set => SetValue(ResourcePathProperty, value);
        }

        private static void OnResourcePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextEditorEmbeddedResourceBehavior behavior && behavior.AssociatedObject != null)
            {
                behavior.SetTextFromResource();
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            SetTextFromResource();
        }

        private void SetTextFromResource()
        {
            if (AssociatedObject == null || string.IsNullOrWhiteSpace(ResourcePath))
            {
                return;
            }

            try
            {
                Stream? stream = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    stream = assembly.GetManifestResourceStream(ResourcePath);
                    if (stream != null)
                    {
                        break;
                    }
                }

                if (stream == null)
                {
                    AssociatedObject.Text = $"Resource not found: {ResourcePath}";
                    return;
                }

                using (stream)
                using (var reader = new StreamReader(stream))
                {
                    AssociatedObject.Text = reader.ReadToEnd();
                }

                // Syntax highlighting logic
                string? highlightResource = null;
                if (ResourcePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    highlightResource = "MosaicWpfDemo.Assets.CSharp.Dark.xshd";
                }
                else if (ResourcePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                {
                    highlightResource = "MosaicWpfDemo.Assets.Xml.Dark.xshd";
                }

                if (highlightResource != null)
                {
                    Stream? highlightStream = null;
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        highlightStream = assembly.GetManifestResourceStream(highlightResource);
                        if (highlightStream != null)
                        {
                            break;
                        }
                    }

                    if (highlightStream != null)
                    {
                        using (highlightStream)
                        using (var xmlReader = System.Xml.XmlReader.Create(highlightStream))
                        {
                            var highlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(
                                xmlReader,
                                ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance
                            );
                            AssociatedObject.SyntaxHighlighting = highlighting;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AssociatedObject.Text = $"Error loading resource: {ex.Message}";
            }
        }
    }
}
