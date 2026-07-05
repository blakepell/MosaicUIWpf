/*
 * ChromaSwap
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ChromaSwap.Common;
using Mosaic.UI.Wpf.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChromaSwap.Dialogs
{
    /// <summary>
    /// Content for the Swap Colors modal dialog: shows the source color, lets the user pick the
    /// replacement color, toggle shading matching and set the tolerance.
    /// </summary>
    public partial class SwapColorsView : UserControl
    {
        /// <summary>
        /// Designer only constructor.
        /// </summary>
        public SwapColorsView() : this(Colors.White, Color.FromRgb(0x3B, 0x82, 0xF6))
        {
        }

        /// <summary>
        /// Content for the Swap Colors modal dialog.
        /// </summary>
        /// <param name="fromColor">The color being replaced.</param>
        /// <param name="initialTarget">The initial replacement color.</param>
        public SwapColorsView(Color fromColor, Color initialTarget)
        {
            InitializeComponent();

            this.FromColor = fromColor;
            FromSwatch.Background = new SolidColorBrush(fromColor);
            FromHexText.Text = ColorUtils.ToHex(fromColor);
            TargetHexBox.SelectedColor = initialTarget;
        }

        /// <summary>
        /// The color being replaced.
        /// </summary>
        public Color FromColor { get; }

        /// <summary>
        /// The replacement color chosen by the user.
        /// </summary>
        public Color TargetColor => Color.FromRgb(TargetHexBox.SelectedColor.R, TargetHexBox.SelectedColor.G, TargetHexBox.SelectedColor.B);

        /// <summary>
        /// The tolerance percentage (0-100) for matching similar shades.
        /// </summary>
        public int Tolerance => (int)ToleranceSlider.Value;

        /// <summary>
        /// Whether shading/gradients should be preserved during the swap.
        /// </summary>
        public bool MatchShading => MatchShadingToggle.IsOn;

        private void ToleranceSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ToleranceValueText != null)
            {
                ToleranceValueText.Text = $"{(int)e.NewValue}%";
            }
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            ModalDialog.FindHost(this)?.Close(false);
        }

        private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
        {
            ModalDialog.FindHost(this)?.Close(true);
        }
    }
}
