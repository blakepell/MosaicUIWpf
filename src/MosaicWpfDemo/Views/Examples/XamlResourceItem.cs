using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// Represents an editable resource item from a XAML ResourceDictionary.
    /// It tracks the original and current values and provides validation.
    /// </summary>
    public partial class XamlResourceItem : ObservableObject, IDataErrorInfo
    {
        [ObservableProperty]
        private string _currentValue;

        [ObservableProperty]
        private Color _previewColor;

        [ObservableProperty]
        private string _key;

        [ObservableProperty]
        private string _resourceType;

        [ObservableProperty]
        private string _originalValue;

        public XObject SourceObject { get; } // The XAttribute or XText node

        public bool IsModified => OriginalValue != CurrentValue;

        public XamlResourceItem(string key, string resourceType, string value, XObject sourceObject)
        {
            SourceObject = sourceObject;
            
            // Use properties instead of backing fields to ensure change notifications
            Key = key;
            ResourceType = resourceType;
            OriginalValue = value;
            CurrentValue = value;
            UpdatePreviewColor();
        }

        partial void OnCurrentValueChanged(string value)
        {
            UpdatePreviewColor();
        }

        public void UpdatePreviewColor()
        {
            try
            {
                PreviewColor = (Color)ColorConverter.ConvertFromString(CurrentValue ?? "#FF000000");
            }
            catch
            {
                // If conversion fails, use a default color to indicate an error
                PreviewColor = Colors.Transparent;
            }
        }

        // Basic validation for the hex color string
        public string Error
        {
            get
            {
                try
                {
                    if (CurrentValue != null)
                    {
                        ColorConverter.ConvertFromString(CurrentValue);
                    }
                    return null;
                }
                catch
                {
                    return "Invalid color format. Use #RGB, #RRGGBB, or #AARRGGBB.";
                }
            }
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(CurrentValue))
                {
                    return Error;
                }
                return null;
            }
        }
    }
}