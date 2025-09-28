namespace Mosaic.UI.Wpf.Controls
{
    public class PropertyGridAttribute : Attribute
    {
        public string Category { get; set; } = "Misc";

        public string? DisplayName { get; set; }

        public string? Description { get; set; }

        public bool Ignore { get; set; } = false;

        public bool IsReadOnly { get; set; } = false;

    }
}
