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
    /// Border which allows Clipping to its border.  Useful especially when you need to
    /// clip to round corners.
    /// </summary>
    /// <remarks>
    /// https://wpfspark.wordpress.com/2011/06/08/clipborder-a-wpf-border-that-clips/
    /// </remarks>
    public class ClipBorder : Border
    {
        /// <summary>
        /// Represents the geometry used to define the clipping region for rendering.
        /// </summary>
        /// <remarks>This field is used internally to store the clipping geometry. It determines the area
        /// of the visual content that will be rendered, with any content outside the defined geometry being
        /// clipped.</remarks>
        private Geometry? _clipGeometry = null;

        /// <summary>
        /// Stores the previous clip object for internal use.
        /// </summary>
        /// <remarks>
        /// This field is used to retain a reference to the old clip, typically for comparison or
        /// restoration purposes. It is intended for internal use and should not be accessed directly outside of the
        /// containing class.
        /// </remarks>
        private object? _oldClip;

        /// <inheritdoc />
        protected override void OnRender(DrawingContext dc)
        {
            OnApplyChildClip();
            base.OnRender(dc);
        }

        /// <inheritdoc />
        public override UIElement? Child
        {
            get => base.Child;
            set
            {
                if (this.Child != value)
                {
                    if (this.Child != null)
                    {
                        // Restore original clipping of the old child
                        this.Child?.SetValue(UIElement.ClipProperty, _oldClip);
                    }

                    if (value != null)
                    {
                        // Store the current clipping of the new child
                        _oldClip = value.ReadLocalValue(UIElement.ClipProperty);
                    }
                    else
                    {
                        // If we don't set it to null we could leak a Geometry object
                        _oldClip = null;
                    }

                    base.Child = value;
                }
            }
        }

        /// <summary>
        /// Applies a clipping geometry to the child element based on the border thickness and corner radius.
        /// </summary>
        /// <remarks>
        /// This method calculates a rounded rectangle geometry using the current border
        /// thickness and corner radius, and applies it as a clipping region to the child element. If no child element
        /// is present, no action is taken.
        /// </remarks>
        protected virtual void OnApplyChildClip()
        {
            var child = this.Child;

            if (child != null)
            {
                // Get the geometry of a rounded rectangle border based on the BorderThickness and CornerRadius
                _clipGeometry = GetRoundRectangle(new Rect(child.RenderSize), this.BorderThickness, this.CornerRadius);
                child.Clip = _clipGeometry;
            }
        }

        /// <summary>
        /// Creates a <see cref="Geometry"/> representing a rectangle with rounded corners,  taking into account the
        /// specified border thickness and corner radii.
        /// </summary>
        /// <remarks>The method normalizes the corner radii and border thickness to ensure they are
        /// non-negative. If the corner radii or  border thickness values are too small, they are treated as zero. The
        /// resulting geometry is frozen for performance  optimization and can be used directly in rendering
        /// operations.</remarks>
        /// <param name="baseRect">The base rectangle that defines the overall dimensions of the rounded rectangle.</param>
        /// <param name="thickness">The thickness of the border, which affects the size of the rounded corners.</param>
        /// <param name="cornerRadius">The radii of the corners, specifying how rounded each corner should be.</param>
        /// <returns>A <see cref="Geometry"/> object representing the rounded rectangle. The geometry includes arcs for the
        /// rounded corners and straight lines for the edges between the corners.</returns>
        public static Geometry GetRoundRectangle(Rect baseRect, Thickness thickness, CornerRadius cornerRadius)
        {
            // Normalizing the corner radius
            if (cornerRadius.TopLeft < double.Epsilon)
            {
                cornerRadius.TopLeft = 0.0;
            }

            if (cornerRadius.TopRight < double.Epsilon)
            {
                cornerRadius.TopRight = 0.0;
            }

            if (cornerRadius.BottomLeft < double.Epsilon)
            {
                cornerRadius.BottomLeft = 0.0;
            }

            if (cornerRadius.BottomRight < double.Epsilon)
            {
                cornerRadius.BottomRight = 0.0;
            }

            // Taking the border thickness into account
            double leftHalf = thickness.Left * 0.5;
            if (leftHalf < double.Epsilon)
            {
                leftHalf = 0.0;
            }

            double topHalf = thickness.Top * 0.5;
            if (topHalf < double.Epsilon)
            {
                topHalf = 0.0;
            }

            double rightHalf = thickness.Right * 0.5;
            if (rightHalf < double.Epsilon)
            {
                rightHalf = 0.0;
            }

            double bottomHalf = thickness.Bottom * 0.5;
            if (bottomHalf < double.Epsilon)
            {
                bottomHalf = 0.0;
            }

            // Create the rectangles for the corners that needs to be curved in the base rectangle 
            // TopLeft Rectangle 
            var topLeftRect = new Rect(baseRect.Location.X,
                                        baseRect.Location.Y,
                                        Math.Max(0.0, cornerRadius.TopLeft - leftHalf),
                                        Math.Max(0.0, cornerRadius.TopLeft - rightHalf));
            // TopRight Rectangle 
            var topRightRect = new Rect(baseRect.Location.X + baseRect.Width - cornerRadius.TopRight + rightHalf,
                                         baseRect.Location.Y,
                                         Math.Max(0.0, cornerRadius.TopRight - rightHalf),
                                         Math.Max(0.0, cornerRadius.TopRight - topHalf));
            // BottomRight Rectangle
            var bottomRightRect = new Rect(baseRect.Location.X + baseRect.Width - cornerRadius.BottomRight + rightHalf,
                                            baseRect.Location.Y + baseRect.Height - cornerRadius.BottomRight + bottomHalf,
                                            Math.Max(0.0, cornerRadius.BottomRight - rightHalf),
                                            Math.Max(0.0, cornerRadius.BottomRight - bottomHalf));
            // BottomLeft Rectangle 
            var bottomLeftRect = new Rect(baseRect.Location.X,
                                           baseRect.Location.Y + baseRect.Height - cornerRadius.BottomLeft + bottomHalf,
                                           Math.Max(0.0, cornerRadius.BottomLeft - leftHalf),
                                           Math.Max(0.0, cornerRadius.BottomLeft - bottomHalf));

            // Adjust the width of the TopLeft and TopRight rectangles so that they are proportional to the width of the baseRect 
            if (topLeftRect.Right > topRightRect.Left)
            {
                var newWidth = (topLeftRect.Width / (topLeftRect.Width + topRightRect.Width)) * baseRect.Width;
                topLeftRect = new Rect(topLeftRect.Location.X, topLeftRect.Location.Y, newWidth, topLeftRect.Height);
                topRightRect = new Rect(baseRect.Left + newWidth, topRightRect.Location.Y, Math.Max(0.0, baseRect.Width - newWidth), topRightRect.Height);
            }

            // Adjust the height of the TopRight and BottomRight rectangles so that they are proportional to the height of the baseRect
            if (topRightRect.Bottom > bottomRightRect.Top)
            {
                var newHeight = (topRightRect.Height / (topRightRect.Height + bottomRightRect.Height)) * baseRect.Height;
                topRightRect = new Rect(topRightRect.Location.X, topRightRect.Location.Y, topRightRect.Width, newHeight);
                bottomRightRect = new Rect(bottomRightRect.Location.X, baseRect.Top + newHeight, bottomRightRect.Width, Math.Max(0.0, baseRect.Height - newHeight));
            }

            // Adjust the width of the BottomLeft and BottomRight rectangles so that they are proportional to the width of the baseRect
            if (bottomRightRect.Left < bottomLeftRect.Right)
            {
                var newWidth = (bottomLeftRect.Width / (bottomLeftRect.Width + bottomRightRect.Width)) * baseRect.Width;
                bottomLeftRect = new Rect(bottomLeftRect.Location.X, bottomLeftRect.Location.Y, newWidth, bottomLeftRect.Height);
                bottomRightRect = new Rect(baseRect.Left + newWidth, bottomRightRect.Location.Y, Math.Max(0.0, baseRect.Width - newWidth), bottomRightRect.Height);
            }

            // Adjust the height of the TopLeft and BottomLeft rectangles so that they are proportional to the height of the baseRect
            if (bottomLeftRect.Top < topLeftRect.Bottom)
            {
                var newHeight = (topLeftRect.Height / (topLeftRect.Height + bottomLeftRect.Height)) * baseRect.Height;
                topLeftRect = new Rect(topLeftRect.Location.X, topLeftRect.Location.Y, topLeftRect.Width, newHeight);
                bottomLeftRect = new Rect(bottomLeftRect.Location.X, baseRect.Top + newHeight, bottomLeftRect.Width, Math.Max(0.0, baseRect.Height - newHeight));
            }

            var roundedRectGeometry = new StreamGeometry();

            using (var context = roundedRectGeometry.Open())
            {
                // Begin from the Bottom of the TopLeft Arc and proceed clockwise
                context.BeginFigure(topLeftRect.BottomLeft, true, true);
                // TopLeft Arc
                context.ArcTo(topLeftRect.TopRight, topLeftRect.Size, 0, false, SweepDirection.Clockwise, true, true);
                // Top Line
                context.LineTo(topRightRect.TopLeft, true, true);
                // TopRight Arc
                context.ArcTo(topRightRect.BottomRight, topRightRect.Size, 0, false, SweepDirection.Clockwise, true, true);
                // Right Line
                context.LineTo(bottomRightRect.TopRight, true, true);
                // BottomRight Arc
                context.ArcTo(bottomRightRect.BottomLeft, bottomRightRect.Size, 0, false, SweepDirection.Clockwise, true, true);
                // Bottom Line
                context.LineTo(bottomLeftRect.BottomRight, true, true);
                // BottomLeft Arc
                context.ArcTo(bottomLeftRect.TopLeft, bottomLeftRect.Size, 0, false, SweepDirection.Clockwise, true, true);
            }

            roundedRectGeometry.Freeze();
            return roundedRectGeometry;
        }
    }
}
