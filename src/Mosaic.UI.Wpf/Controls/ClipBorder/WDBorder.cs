using Mosaic.UI.Wpf.Common;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides a border that exposes a content clipping geometry.
    /// </summary>
    public class WDBorder : Border
    {
        public static readonly DependencyPropertyKey ContentClipPropertyKey =
            DependencyProperty.RegisterReadOnly("ContentClip", typeof(Geometry), typeof(WDBorder),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ContentClipProperty = ContentClipPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets or sets the geometry used to clip the border content.
        /// </summary>
        public Geometry ContentClip
        {
            get => (Geometry)GetValue(ContentClipProperty);
            set => SetValue(ContentClipProperty, value);
        }

        /// <summary>
        /// Calculates the clipping geometry for the current border layout.
        /// </summary>
        /// <returns>The clipping geometry, or <see langword="null" /> when clipping is not required.</returns>
        private Geometry CalculateContentClip()
        {
            var borderThickness = BorderThickness;
            var cornerRadius = CornerRadius;
            var renderSize = RenderSize;
            var width = renderSize.Width - borderThickness.Left - borderThickness.Right;
            var height = renderSize.Height - borderThickness.Top - borderThickness.Bottom;
            if (width > 0.0 && height > 0.0)
            {
                var rect = new Rect(0.0, 0.0, width, height);
                var radii = new GeometryHelper.Radii(cornerRadius, borderThickness, false);
                var streamGeometry = new StreamGeometry();
                using (var streamGeometryContext = streamGeometry.Open())
                {
                    GeometryHelper.GenerateGeometry(streamGeometryContext, rect, radii);
                    streamGeometry.Freeze();
                    return streamGeometry;
                }
            }

            return null;
        }

        /// <summary>
        /// Renders the border and updates the content clipping geometry.
        /// </summary>
        /// <param name="dc">The drawing context used to render the border.</param>
        protected override void OnRender(DrawingContext dc)
        {
            SetValue(ContentClipPropertyKey, CalculateContentClip());
            base.OnRender(dc);
        }
    }
}