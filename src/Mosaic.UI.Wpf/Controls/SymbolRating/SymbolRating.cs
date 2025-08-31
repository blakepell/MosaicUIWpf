/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A symbol rating component.
    /// </summary>
    public class SymbolRating : Control
    {
        #region Private Fields

        private Button? _deselectButton;
        private ItemsControl? _symbolContainer;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Count"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CountProperty = DependencyProperty.Register(
            nameof(Count), typeof(int), typeof(SymbolRating), new PropertyMetadata(5, OnCountChanged));

        /// <summary>
        /// Gets or sets the count value associated with this instance.
        /// </summary>
        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedCountProperty = DependencyProperty.Register(
            nameof(SelectedCount), typeof(int), typeof(SymbolRating), new PropertyMetadata(0, OnSelectedCountChanged));

        /// <summary>
        /// The number of selected symbols.
        /// </summary>
        public int SelectedCount
        {
            get => (int)GetValue(SelectedCountProperty);
            set => SetValue(SelectedCountProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Symbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(
            nameof(Symbol), typeof(string), typeof(SymbolRating), new PropertyMetadata("★"));

        /// <summary>
        /// Gets or sets the symbol character to display.
        /// </summary>
        public string Symbol
        {
            get => (string)GetValue(SymbolProperty);
            set => SetValue(SymbolProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SymbolFont"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SymbolFontProperty = DependencyProperty.Register(
            nameof(SymbolFont), typeof(FontFamily), typeof(SymbolRating), new PropertyMetadata(new FontFamily("Segoe UI Symbol")));

        /// <summary>
        /// Gets or sets the font family for the symbol.
        /// </summary>
        public FontFamily SymbolFont
        {
            get => (FontFamily)GetValue(SymbolFontProperty);
            set => SetValue(SymbolFontProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SymbolSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SymbolSizeProperty = DependencyProperty.Register(
            nameof(SymbolSize), typeof(double), typeof(SymbolRating), new PropertyMetadata(20.0));

        /// <summary>
        /// Gets or sets the size of the symbols.
        /// </summary>
        public double SymbolSize
        {
            get => (double)GetValue(SymbolSizeProperty);
            set => SetValue(SymbolSizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register(
            nameof(SelectedBrush), typeof(Brush), typeof(SymbolRating), new PropertyMetadata(new SolidColorBrush(Colors.Gold)));

        /// <summary>
        /// Gets or sets the brush for selected symbols.
        /// </summary>
        public Brush SelectedBrush
        {
            get => (Brush)GetValue(SelectedBrushProperty);
            set => SetValue(SelectedBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="UnselectedBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UnselectedBrushProperty = DependencyProperty.Register(
            nameof(UnselectedBrush), typeof(Brush), typeof(SymbolRating), new PropertyMetadata(new SolidColorBrush(Colors.LightGray)));

        /// <summary>
        /// Gets or sets the brush for unselected symbols.
        /// </summary>
        public Brush UnselectedBrush
        {
            get => (Brush)GetValue(UnselectedBrushProperty);
            set => SetValue(UnselectedBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HoverBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverBrushProperty = DependencyProperty.Register(
            nameof(HoverBrush), typeof(Brush), typeof(SymbolRating), new PropertyMetadata(new SolidColorBrush(Colors.Orange)));

        /// <summary>
        /// Gets or sets the brush for symbols when hovering.
        /// </summary>
        public Brush HoverBrush
        {
            get => (Brush)GetValue(HoverBrushProperty);
            set => SetValue(HoverBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowDeselectOption"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowDeselectOptionProperty = DependencyProperty.Register(
            nameof(ShowDeselectOption), typeof(bool), typeof(SymbolRating), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether to show the deselect option.
        /// </summary>
        public bool ShowDeselectOption
        {
            get => (bool)GetValue(ShowDeselectOptionProperty);
            set => SetValue(ShowDeselectOptionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsHoverPreviewEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsHoverPreviewEnabledProperty = DependencyProperty.Register(
            nameof(IsHoverPreviewEnabled), typeof(bool), typeof(SymbolRating), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether hover preview is enabled (shows preview of rating when hovering).
        /// </summary>
        public bool IsHoverPreviewEnabled
        {
            get => (bool)GetValue(IsHoverPreviewEnabledProperty);
            set => SetValue(IsHoverPreviewEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HoveredIndex"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoveredIndexProperty = DependencyProperty.Register(
            nameof(HoveredIndex), typeof(int), typeof(SymbolRating), new PropertyMetadata(-1));

        /// <summary>
        /// Gets or sets the index of the currently hovered symbol.
        /// </summary>
        public int HoveredIndex
        {
            get => (int)GetValue(HoveredIndexProperty);
            set => SetValue(HoveredIndexProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SymbolIndices"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SymbolIndicesProperty = DependencyProperty.Register(
            nameof(SymbolIndices), typeof(ObservableCollection<int>), typeof(SymbolRating), new PropertyMetadata(null));

        /// <summary>
        /// Gets the collection of symbol indices for binding.
        /// </summary>
        public ObservableCollection<int> SymbolIndices
        {
            get => (ObservableCollection<int>)GetValue(SymbolIndicesProperty);
            private set => SetValue(SymbolIndicesProperty, value);
        }

        #endregion

        #region Routed Properties

        /// <summary>
        /// Routed event for when the rating changes.
        /// </summary>
        public static readonly RoutedEvent RatingChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(RatingChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<int>), typeof(SymbolRating));

        /// <summary>
        /// Occurs when the rating changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<int> RatingChanged
        {
            add => AddHandler(RatingChangedEvent, value);
            remove => RemoveHandler(RatingChangedEvent, value);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Command for handling symbol clicks.
        /// </summary>
        public ICommand SymbolClickCommand { get; private set; }

        #endregion

        /// <summary>
        /// Initializes static metadata for the <see cref="SymbolRating"/> class.
        /// </summary>
        static SymbolRating()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SymbolRating), new FrameworkPropertyMetadata(typeof(SymbolRating)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolRating"/> class.
        /// </summary>
        public SymbolRating()
        {
            SymbolIndices = new ObservableCollection<int>();
            SymbolClickCommand = new RelayCommand<int>(OnSymbolClick);
            UpdateSymbolIndices();
        }

        /// <summary>
        /// Called when the template is applied.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Unsubscribe from previous events
            if (_deselectButton != null)
            {
                _deselectButton.Click -= OnDeselectButtonClick;
            }

            if (_symbolContainer != null)
            {
                _symbolContainer.MouseLeave -= OnContainerMouseLeave;
                _symbolContainer.MouseMove -= OnContainerMouseMove;
            }

            // Get template parts
            _deselectButton = GetTemplateChild("PART_DeselectButton") as Button;
            _symbolContainer = GetTemplateChild("PART_SymbolContainer") as ItemsControl;

            // Subscribe to events
            if (_deselectButton != null)
            {
                _deselectButton.Click += OnDeselectButtonClick;
            }

            if (_symbolContainer != null)
            {
                _symbolContainer.MouseLeave += OnContainerMouseLeave;
                _symbolContainer.MouseMove += OnContainerMouseMove;
            }

            UpdateSymbolIndices();
        }

        /// <summary>
        /// Called when the Count property changes.
        /// </summary>
        private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SymbolRating rating)
            {
                rating.UpdateSymbolIndices();
                rating.EnsureSelectedCountInRange();
            }
        }

        /// <summary>
        /// Called when the SelectedCount property changes.
        /// </summary>
        private static void OnSelectedCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SymbolRating rating)
            {
                var oldValue = (int)e.OldValue;
                var newValue = (int)e.NewValue;

                rating.EnsureSelectedCountInRange();

                var args = new RoutedPropertyChangedEventArgs<int>(oldValue, newValue, RatingChangedEvent);
                rating.RaiseEvent(args);
            }
        }

        /// <summary>
        /// Updates the symbol indices collection.
        /// </summary>
        private void UpdateSymbolIndices()
        {
            SymbolIndices.Clear();

            for (int i = 1; i <= Count; i++)
            {
                SymbolIndices.Add(i);
            }
        }

        /// <summary>
        /// Ensures the SelectedCount is within the valid range.
        /// </summary>
        private void EnsureSelectedCountInRange()
        {
            if (SelectedCount > Count)
            {
                SelectedCount = Count;
            }
            else if (SelectedCount < 0)
            {
                SelectedCount = 0;
            }
        }

        /// <summary>
        /// Handles container mouse move for hover detection.
        /// </summary>
        private void OnContainerMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsHoverPreviewEnabled || _symbolContainer == null)
            {
                return;
            }

            var position = e.GetPosition(_symbolContainer);
            var hitResult = VisualTreeHelper.HitTest(_symbolContainer, position);

            // Walk up the visual tree to find a button with a tag
            var element = hitResult?.VisualHit as FrameworkElement;

            while (element != null)
            {
                if (element is Button button && button.Tag is int index)
                {
                    HoveredIndex = index;
                    return;
                }
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }
        }

        /// <summary>
        /// Handles container mouse leave.
        /// </summary>
        private void OnContainerMouseLeave(object sender, MouseEventArgs e)
        {
            HoveredIndex = -1;
        }

        /// <summary>
        /// Handles click events for symbol buttons.
        /// </summary>
        private void OnSymbolClick(int index)
        {
            SelectedCount = index;
            HoveredIndex = -1;
        }

        /// <summary>
        /// Handles the deselect button click.
        /// </summary>
        private void OnDeselectButtonClick(object sender, RoutedEventArgs e)
        {
            ClearRating();
        }

        /// <summary>
        /// Sets the rating to the specified value.
        /// </summary>
        /// <param name="rating">The rating value (1-based).</param>
        public void SetRating(int rating)
        {
            if (rating < 0 || rating > Count)
            {
                return;
            }

            SelectedCount = rating;
        }

        /// <summary>
        /// Clears the rating (sets SelectedCount to 0).
        /// </summary>
        public void ClearRating()
        {
            SelectedCount = 0;
        }
    }
}