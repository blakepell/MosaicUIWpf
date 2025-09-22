namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides utility methods and attached properties for dynamically defining row and column definitions in a <see
    /// cref="Grid"/> control.
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Example usage in XAML:
    /// <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    ///       xmlns:utils="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"
    ///       utils:GridUtils.RowDefinitions="Auto,*,2*"
    ///       utils:GridUtils.ColumnDefinitions="100,*,Auto">
    ///     <!-- Child elements placed using Grid.Row and Grid.Column -->
    ///     <TextBlock Grid.Row="0" Grid.Column="0" Text="Header" />
    ///     <ListBox Grid.Row="1" Grid.Column="1" />
    /// </Grid>
    /// 
    /// Notes:
    /// - Row/column sizes are comma-separated. Empty entry yields default (Auto).
    /// - Use '*' suffix for star sizing (e.g. "2*" or "*" for 1*).
    /// - Use "Auto" for Auto sizing or a numeric pixel value (e.g. "100") for fixed pixels.
    /// ]]>
    /// </remarks>
    public class GridUtils
    {
        #region RowDefinitions attached property

        /// <summary>
        /// Identified the RowDefinitions attached property
        /// </summary>
        public static readonly DependencyProperty RowDefinitionsProperty =
            DependencyProperty.RegisterAttached("RowDefinitions", typeof(string), typeof(GridUtils),
                new PropertyMetadata("", new PropertyChangedCallback(OnRowDefinitionsPropertyChanged)));

        /// <summary>
        /// Gets the value of the RowDefinitions property
        /// </summary>
        public static string GetRowDefinitions(DependencyObject d)
        {
            return (string)d.GetValue(RowDefinitionsProperty);
        }

        /// <summary>
        /// Sets the value of the RowDefinitions property
        /// </summary>
        public static void SetRowDefinitions(DependencyObject d, string value)
        {
            d.SetValue(RowDefinitionsProperty, value);
        }

        /// <summary>
        /// Handles property changed event for the RowDefinitions property, constructing
        /// the required RowDefinitions elements on the grid which this property is attached to.
        /// </summary>
        private static void OnRowDefinitionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var targetGrid = d as Grid;

            // construct the required row definitions
            targetGrid.RowDefinitions.Clear();
            string? rowDefs = e.NewValue as string;
            var rowDefArray = rowDefs?.Split(',');

            if (rowDefArray == null)
            {
                return;
            }

            foreach (string rowDefinition in rowDefArray)
            {
                if (rowDefinition.Trim() == "")
                {
                    targetGrid.RowDefinitions.Add(new RowDefinition());
                }
                else
                {
                    targetGrid.RowDefinitions.Add(new RowDefinition()
                    {
                        Height = ParseLength(rowDefinition)
                    });
                }
            }
        }

        #endregion


        #region ColumnDefinitions attached property

        /// <summary>
        /// Identifies the ColumnDefinitions attached property
        /// </summary>
        public static readonly DependencyProperty ColumnDefinitionsProperty =
            DependencyProperty.RegisterAttached("ColumnDefinitions", typeof(string), typeof(GridUtils),
                new PropertyMetadata("", OnColumnDefinitionsPropertyChanged));

        /// <summary>
        /// Gets the value of the ColumnDefinitions property
        /// </summary>
        public static string GetColumnDefinitions(DependencyObject d)
        {
            return (string)d.GetValue(ColumnDefinitionsProperty);
        }

        /// <summary>
        /// Sets the value of the ColumnDefinitions property
        /// </summary>
        public static void SetColumnDefinitions(DependencyObject d, string value)
        {
            d.SetValue(ColumnDefinitionsProperty, value);
        }

        /// <summary>
        /// Handles property changed event for the ColumnDefinitions property, constructing
        /// the required ColumnDefinitions elements on the grid which this property is attached to.
        /// </summary>
        private static void OnColumnDefinitionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var targetGrid = d as Grid;

            if (targetGrid == null)
            {
                return;
            }

            // construct the required column definitions
            targetGrid.ColumnDefinitions.Clear();
            string? columnDefs = e.NewValue as string;


            if (columnDefs == null)
            {
                return;
            }

            var columnDefArray = columnDefs.Split(',');

            foreach (string columnDefinition in columnDefArray)
            {
                if (columnDefinition.Trim() == "")
                {
                    targetGrid.ColumnDefinitions.Add(new ColumnDefinition());
                }
                else
                {
                    targetGrid.ColumnDefinitions.Add(new ColumnDefinition()
                    {
                        Width = ParseLength(columnDefinition)
                    });
                }
            }
        }

        #endregion

        /// <summary>
        /// Parses a string to create a GridLength
        /// </summary>
        private static GridLength ParseLength(string length)
        {
            length = length.Trim();

            if (length.ToLowerInvariant().Equals("auto"))
            {
                return new GridLength(0, GridUnitType.Auto);
            }

            if (length.Contains('*'))
            {
                length = length.Replace("*", "");

                if (string.IsNullOrEmpty(length))
                {
                    length = "1";
                }

                return new GridLength(double.Parse(length), GridUnitType.Star);
            }

            return new GridLength(double.Parse(length), GridUnitType.Pixel);
        }
    }
}
