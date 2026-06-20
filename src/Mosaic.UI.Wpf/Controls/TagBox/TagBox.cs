/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Automation.Peers;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A specialized input control that turns typed text into removable, vibrantly-colored "tags". Pressing
    /// <see cref="Key.Enter"/> commits the current text as a new tag, each tag carries an "✕" button to remove it, and
    /// pressing <see cref="Key.Back"/> while the caret sits just to the right of the tags removes the last one. Tags are
    /// surfaced through the bindable <see cref="Tags"/> collection, and the <see cref="TagChanging"/> /
    /// <see cref="TagChanged"/> events allow callers to veto or observe every change.
    /// </summary>
    [TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartTagPanel, Type = typeof(Panel))]
    [DefaultProperty(nameof(Tags))]
    [DefaultEvent(nameof(TagChanged))]
    public class TagBox : Control
    {
        private const string PartTextBox = "PART_TextBox";
        private const string PartTagPanel = "PART_TagPanel";

        /// <summary>
        /// The text entry portion of the control where new tags are typed.
        /// </summary>
        private TextBox? _textBox;

        /// <summary>
        /// The panel that hosts the tag chips followed by the input <see cref="_textBox"/>.
        /// </summary>
        private Panel? _tagPanel;

        /// <summary>
        /// Raised after a tag has been added to or removed from the <see cref="Tags"/> collection.
        /// </summary>
        public event EventHandler<TagChangedEventArgs>? TagChanged;

        /// <summary>
        /// Raised before a tag is added to or removed from the <see cref="Tags"/> collection. Set
        /// <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to <see langword="true"/> to veto the change.
        /// </summary>
        public event EventHandler<TagChangingEventArgs>? TagChanging;

        /// <summary>
        /// The command used by a tag chip's delete button to remove that tag.
        /// </summary>
        public ICommand DeleteTagCommand { get; }

        /// <summary>
        /// Identifies the <see cref="Tags"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TagsProperty = DependencyProperty.Register(
            nameof(Tags), typeof(ObservableCollection<string>), typeof(TagBox),
            new FrameworkPropertyMetadata(null, OnTagsChanged));

        /// <summary>
        /// Gets or sets the collection of tags. Adding or removing items from this collection (whether through the UI
        /// or directly in code) raises <see cref="TagChanged"/> and keeps the displayed chips in sync.
        /// </summary>
        [Category("Common")]
        [Description("The collection of tags displayed by the control.")]
        public ObservableCollection<string> Tags
        {
            get => (ObservableCollection<string>)GetValue(TagsProperty);
            set => SetValue(TagsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Watermark"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(
            nameof(Watermark), typeof(string), typeof(TagBox), new PropertyMetadata("Add a tag..."));

        /// <summary>
        /// Gets or sets the placeholder text shown when there are no tags and no text has been entered.
        /// </summary>
        [Category("Common")]
        [Description("The placeholder text shown when the control is empty.")]
        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AllowDuplicates"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowDuplicatesProperty = DependencyProperty.Register(
            nameof(AllowDuplicates), typeof(bool), typeof(TagBox), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether duplicate tags are allowed. When <see langword="false"/> (the
        /// default), duplicates are rejected using a case-insensitive comparison.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether duplicate tags are allowed.")]
        public bool AllowDuplicates
        {
            get => (bool)GetValue(AllowDuplicatesProperty);
            set => SetValue(AllowDuplicatesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TagBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TagBackgroundProperty = DependencyProperty.Register(
            nameof(TagBackground), typeof(Brush), typeof(TagBox), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the fill brush used for every tag chip.
        /// </summary>
        [Category("Brushes")]
        [Description("The fill brush used for every tag chip.")]
        public Brush? TagBackground
        {
            get => (Brush?)GetValue(TagBackgroundProperty);
            set => SetValue(TagBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TagForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TagForegroundProperty = DependencyProperty.Register(
            nameof(TagForeground), typeof(Brush), typeof(TagBox), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the text/glyph brush used inside every tag chip.
        /// </summary>
        [Category("Brushes")]
        [Description("The text and delete-glyph brush used inside every tag chip.")]
        public Brush? TagForeground
        {
            get => (Brush?)GetValue(TagForegroundProperty);
            set => SetValue(TagForegroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TagBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TagBorderBrushProperty = DependencyProperty.Register(
            nameof(TagBorderBrush), typeof(Brush), typeof(TagBox), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the border brush used for every tag chip.
        /// </summary>
        [Category("Brushes")]
        [Description("The border brush used for every tag chip.")]
        public Brush? TagBorderBrush
        {
            get => (Brush?)GetValue(TagBorderBrushProperty);
            set => SetValue(TagBorderBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TagDeleteHoverBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TagDeleteHoverBackgroundProperty = DependencyProperty.Register(
            nameof(TagDeleteHoverBackground), typeof(Brush), typeof(TagBox), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the background brush shown behind a chip's delete glyph while the pointer is over it.
        /// </summary>
        [Category("Brushes")]
        [Description("The background brush shown behind a chip's delete glyph on hover.")]
        public Brush? TagDeleteHoverBackground
        {
            get => (Brush?)GetValue(TagDeleteHoverBackgroundProperty);
            set => SetValue(TagDeleteHoverBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TagCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TagCornerRadiusProperty = DependencyProperty.Register(
            nameof(TagCornerRadius), typeof(CornerRadius), typeof(TagBox), new PropertyMetadata(new CornerRadius(10)));

        /// <summary>
        /// Gets or sets the corner radius applied to each tag chip. Defaults to a rounded "pill" shape.
        /// </summary>
        [Category("Appearance")]
        [Description("The corner radius applied to each tag chip.")]
        public CornerRadius TagCornerRadius
        {
            get => (CornerRadius)GetValue(TagCornerRadiusProperty);
            set => SetValue(TagCornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(TagBox), new PropertyMetadata(new CornerRadius(0)));

        /// <summary>
        /// Gets or sets the corner radius applied to each tag chip. Defaults to a rounded "pill" shape.
        /// </summary>
        [Category("Appearance")]
        [Description("The corner radius applied the tag box itself.")]
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        private static readonly DependencyPropertyKey ShowWatermarkPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(ShowWatermark), typeof(bool), typeof(TagBox), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="ShowWatermark"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowWatermarkProperty = ShowWatermarkPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether the watermark should currently be displayed (used by the control template).
        /// </summary>
        [Browsable(false)]
        public bool ShowWatermark
        {
            get => (bool)GetValue(ShowWatermarkProperty);
            private set => SetValue(ShowWatermarkPropertyKey, value);
        }

        /// <summary>
        /// Initializes static members of the <see cref="TagBox"/> class.
        /// </summary>
        static TagBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TagBox), new FrameworkPropertyMetadata(typeof(TagBox)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TagBox"/> class.
        /// </summary>
        public TagBox()
        {
            this.DeleteTagCommand = new RelayCommand<string>(tag => this.RemoveTag(tag));
            this.SetCurrentValue(TagsProperty, new ObservableCollection<string>());
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_textBox != null)
            {
                _textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                _textBox.TextChanged -= TextBox_TextChanged;
            }

            _tagPanel = this.GetTemplateChild(PartTagPanel) as Panel;
            _textBox = this.GetTemplateChild(PartTextBox) as TextBox;

            if (_textBox != null)
            {
                _textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                _textBox.TextChanged += TextBox_TextChanged;
            }

            this.RebuildTags();
            this.UpdateWatermark();
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TagBoxAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            // Clicking anywhere within the control (empty space, between chips, etc.) puts focus on the input box.
            if (_textBox != null && !_textBox.IsKeyboardFocusWithin)
            {
                _textBox.Focus();
                _textBox.CaretIndex = _textBox.Text.Length;
            }
        }

        /// <summary>
        /// Attempts to add a tag, raising <see cref="TagChanging"/> (which may cancel the operation) followed by
        /// <see cref="TagChanged"/>.
        /// </summary>
        /// <param name="tag">The tag text to add. Surrounding whitespace is trimmed.</param>
        /// <returns><see langword="true"/> if the tag was added; otherwise <see langword="false"/>.</returns>
        public bool AddTag(string? tag)
        {
            tag = tag?.Trim();

            if (string.IsNullOrEmpty(tag) || this.Tags == null)
            {
                return false;
            }

            if (!this.AllowDuplicates && this.ContainsTag(tag))
            {
                return false;
            }

            var changing = new TagChangingEventArgs(tag, TagChangeAction.Add);
            this.TagChanging?.Invoke(this, changing);

            if (changing.Cancel)
            {
                return false;
            }

            // The collection-changed handler raises TagChanged and refreshes the chips.
            this.Tags.Add(tag);
            return true;
        }

        /// <summary>
        /// Attempts to remove a tag, raising <see cref="TagChanging"/> (which may cancel the operation) followed by
        /// <see cref="TagChanged"/>.
        /// </summary>
        /// <param name="tag">The tag to remove.</param>
        /// <returns><see langword="true"/> if the tag was removed; otherwise <see langword="false"/>.</returns>
        public bool RemoveTag(string? tag)
        {
            if (tag == null || this.Tags == null || !this.Tags.Contains(tag))
            {
                return false;
            }

            var changing = new TagChangingEventArgs(tag, TagChangeAction.Remove);
            this.TagChanging?.Invoke(this, changing);

            if (changing.Cancel)
            {
                return false;
            }

            this.Tags.Remove(tag);
            return true;
        }

        /// <summary>
        /// Determines whether the specified tag already exists, using a case-insensitive comparison.
        /// </summary>
        /// <param name="tag">The candidate tag.</param>
        private bool ContainsTag(string tag)
        {
            return this.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Handles the addition or removal of the <see cref="Tags"/> collection instance, rewiring change notifications.
        /// </summary>
        private static void OnTagsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TagBox)d;

            if (e.OldValue is ObservableCollection<string> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnTagsCollectionChanged;
            }

            if (e.NewValue is ObservableCollection<string> newCollection)
            {
                newCollection.CollectionChanged += control.OnTagsCollectionChanged;
            }

            control.RebuildTags();
            control.UpdateWatermark();
        }

        /// <summary>
        /// Keeps the displayed chips in sync with the backing collection and raises <see cref="TagChanged"/> for each
        /// added or removed item.
        /// </summary>
        private void OnTagsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.RaiseTagChanged(e.NewItems, TagChangeAction.Add);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    this.RaiseTagChanged(e.OldItems, TagChangeAction.Remove);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    this.RaiseTagChanged(e.OldItems, TagChangeAction.Remove);
                    this.RaiseTagChanged(e.NewItems, TagChangeAction.Add);
                    break;
            }

            this.RebuildTags();
            this.UpdateWatermark();
        }

        /// <summary>
        /// Raises <see cref="TagChanged"/> for each item in the supplied list.
        /// </summary>
        private void RaiseTagChanged(System.Collections.IList? items, TagChangeAction action)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                if (item is string tag)
                {
                    this.TagChanged?.Invoke(this, new TagChangedEventArgs(tag, action));
                }
            }
        }

        /// <summary>
        /// Rebuilds the visual chips, leaving the input <see cref="_textBox"/> in place as the final child so that
        /// keyboard focus and caret position are preserved.
        /// </summary>
        private void RebuildTags()
        {
            if (_tagPanel == null || _textBox == null)
            {
                return;
            }

            for (int i = _tagPanel.Children.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_tagPanel.Children[i], _textBox))
                {
                    _tagPanel.Children.RemoveAt(i);
                }
            }

            if (this.Tags == null)
            {
                return;
            }

            int index = 0;

            foreach (var tag in this.Tags)
            {
                _tagPanel.Children.Insert(index++, this.CreateChip(tag));
            }
        }

        /// <summary>
        /// Creates a single tag chip (a rounded border containing the tag text and a delete glyph). Colors are bound to
        /// the control's tag brush properties so they track theme and runtime changes.
        /// </summary>
        /// <param name="tag">The tag text the chip represents.</param>
        private UIElement CreateChip(string tag)
        {
            var chip = new Border
            {
                Margin = new Thickness(0, 2, 5, 2),
                Padding = new Thickness(9, 2, 4, 2),
                BorderThickness = new Thickness(1),
                SnapsToDevicePixels = true,
                VerticalAlignment = VerticalAlignment.Center
            };

            chip.SetBinding(Border.BackgroundProperty, this.SelfBinding(nameof(TagBackground)));
            chip.SetBinding(Border.BorderBrushProperty, this.SelfBinding(nameof(TagBorderBrush)));
            chip.SetBinding(Border.CornerRadiusProperty, this.SelfBinding(nameof(TagCornerRadius)));

            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var text = new TextBlock
            {
                Text = tag,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeights.Medium
            };
            text.SetBinding(TextBlock.ForegroundProperty, this.SelfBinding(nameof(TagForeground)));

            var deleteHost = new Border
            {
                Width = 16,
                Height = 16,
                Margin = new Thickness(5, 0, 0, 0),
                CornerRadius = new CornerRadius(8),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = $"Remove \"{tag}\""
            };

            var glyph = new TextBlock
            {
                Text = "✕",
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            glyph.SetBinding(TextBlock.ForegroundProperty, this.SelfBinding(nameof(TagForeground)));
            deleteHost.Child = glyph;

            deleteHost.MouseEnter += (_, _) => deleteHost.SetBinding(Border.BackgroundProperty, this.SelfBinding(nameof(TagDeleteHoverBackground)));
            deleteHost.MouseLeave += (_, _) =>
            {
                System.Windows.Data.BindingOperations.ClearBinding(deleteHost, Border.BackgroundProperty);
                deleteHost.Background = Brushes.Transparent;
            };
            deleteHost.MouseLeftButtonUp += (_, e) =>
            {
                e.Handled = true;
                this.RemoveTag(tag);
            };

            panel.Children.Add(text);
            panel.Children.Add(deleteHost);
            chip.Child = panel;

            return chip;
        }

        /// <summary>
        /// Creates a one-way binding to a property on this control instance.
        /// </summary>
        private System.Windows.Data.Binding SelfBinding(string path)
        {
            return new System.Windows.Data.Binding(path) { Source = this };
        }

        /// <summary>
        /// Updates <see cref="ShowWatermark"/> based on whether there are any tags or pending input text.
        /// </summary>
        private void UpdateWatermark()
        {
            this.ShowWatermark = (this.Tags?.Count ?? 0) == 0 && string.IsNullOrEmpty(_textBox?.Text);
        }

        /// <summary>
        /// Handles Enter (commit current text as a tag) and Backspace (remove the last tag when the caret is at the
        /// start of an empty input, i.e. just to the right of the tags).
        /// </summary>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_textBox == null)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                    if (this.AddTag(_textBox.Text))
                    {
                        _textBox.Clear();
                    }

                    e.Handled = true;
                    break;

                case Key.Back:
                    if (_textBox.CaretIndex == 0 && _textBox.SelectionLength == 0 && this.Tags is { Count: > 0 })
                    {
                        this.RemoveTag(this.Tags[^1]);
                        e.Handled = true;
                    }

                    break;
            }
        }

        /// <summary>
        /// Keeps the watermark state current as the user types.
        /// </summary>
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateWatermark();
        }
    }
}
