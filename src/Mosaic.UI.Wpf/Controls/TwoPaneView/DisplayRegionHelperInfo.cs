namespace Mosaic.UI.Wpf.Controls
{
    internal struct DisplayRegionHelperInfo
    {
        private const int _maxRegions = 2;

        public DisplayRegionHelperInfo(TwoPaneViewMode mode = TwoPaneViewMode.SinglePane)
        {
            Regions = new Rect[_maxRegions];
            Mode = mode;
        }

        public TwoPaneViewMode Mode { get; set; }
        public Rect[] Regions { get; set; }
    }
}