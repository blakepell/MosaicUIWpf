using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mosaic.UI.Wpf.AvalonDock.Layout;

namespace Mosaic.UI.Wpf.AvalonDock.Controls
{
	/// <summary>
	/// Represents the anchorable Pane Tab Panel.
	/// </summary>
	public class AnchorablePaneTabPanel : Panel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AnchorablePaneTabPanel"/> class.
		/// </summary>
		public AnchorablePaneTabPanel()
		{
			FlowDirection = FlowDirection.LeftToRight;
		}

		/// <inheritdoc/>
		protected override Size MeasureOverride(Size availableSize)
		{
			double totWidth = 0;
			double maxHeight = 0;
			var visibleChildren = Children.Cast<FrameworkElement>()
				.Where(child => child.Visibility != Visibility.Collapsed)
				.ToArray();
			foreach (FrameworkElement child in visibleChildren)
			{
				child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
				totWidth += child.DesiredSize.Width;
				maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
			}

			if (totWidth > availableSize.Width)
			{
				double childFinalDesiredWidth = availableSize.Width / visibleChildren.Length;
				foreach (FrameworkElement child in visibleChildren)
				{
					child.Measure(new Size(childFinalDesiredWidth, availableSize.Height));
				}
			}

			return new Size(Math.Min(availableSize.Width, totWidth), maxHeight);
		}

		/// <inheritdoc/>
		protected override Size ArrangeOverride(Size finalSize)
		{
			var visibleChildren = Children.Cast<FrameworkElement>()
				.Where(child => child.Visibility != Visibility.Collapsed)
				.ToArray();

			double finalWidth = finalSize.Width;
			double desiredWidth = visibleChildren.Sum(ch => ch.DesiredSize.Width);
			double offsetX = 0.0;

			if (finalWidth > desiredWidth)
			{
				foreach (FrameworkElement child in visibleChildren)
				{
					double childFinalWidth = child.DesiredSize.Width;
					child.Arrange(new Rect(offsetX, 0, childFinalWidth, finalSize.Height));

					offsetX += childFinalWidth;
				}
			}
			else
			{
				double childFinalWidth = visibleChildren.Length == 0
					? 0
					: finalWidth / visibleChildren.Length;
				foreach (FrameworkElement child in visibleChildren)
				{
					child.Arrange(new Rect(offsetX, 0, childFinalWidth, finalSize.Height));

					offsetX += childFinalWidth;
				}
			}

			return finalSize;
		}

		/// <inheritdoc/>
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed &&
				LayoutAnchorableTabItem.IsDraggingItem())
			{
				var contentModel = LayoutAnchorableTabItem.GetDraggingItem().Model as LayoutAnchorable;
				var manager = contentModel.Root.Manager;
				LayoutAnchorableTabItem.ResetDraggingItem();

				manager.StartDraggingFloatingWindowForContent(contentModel);
			}

			base.OnMouseLeave(e);
		}
	}
}
