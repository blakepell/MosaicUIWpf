namespace Mosaic.UI.Wpf.Controls
{
    public class PropertyGridAttribute : Attribute
    {
        public bool Ignore { get; set; } = false;

        public string Category { get; set; } = "Misc";

        public string? DisplayName { get; set; }

        public string? Description { get; set; }

    }
}
