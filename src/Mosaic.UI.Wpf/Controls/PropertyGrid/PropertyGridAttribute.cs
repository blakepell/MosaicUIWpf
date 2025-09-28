namespace Mosaic.UI.Wpf.Controls
{
    public class PropertyGridAttribute : Attribute
    {
        public string Category { get; set; } = "Misc";

        public string? DisplayName { get; set; }

        public string? Description { get; set; }

        public bool Ignore { get; set; } = false;

        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum length of the property value.
        /// Used for string properties to limit input length.
        /// </summary>
        public int MaxLength { get; set; } = 0;
    }
}
