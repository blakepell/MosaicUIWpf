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

using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Data;
using System.Windows.Input;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides a single, themed editor for a four-segment IPv4 address.
    /// </summary>
    [TemplatePart(Name = PartSegment1, Type = typeof(NumericTextBox))]
    [TemplatePart(Name = PartSegment2, Type = typeof(NumericTextBox))]
    [TemplatePart(Name = PartSegment3, Type = typeof(NumericTextBox))]
    [TemplatePart(Name = PartSegment4, Type = typeof(NumericTextBox))]
    [DefaultProperty(nameof(Text))]
    public class IPv4TextBox : Control
    {
        /// <summary>
        /// Specifies the template-part name for the first IPv4 segment editor.
        /// </summary>
        private const string PartSegment1 = "PART_Segment1";

        /// <summary>
        /// Specifies the template-part name for the second IPv4 segment editor.
        /// </summary>
        private const string PartSegment2 = "PART_Segment2";

        /// <summary>
        /// Specifies the template-part name for the third IPv4 segment editor.
        /// </summary>
        private const string PartSegment3 = "PART_Segment3";

        /// <summary>
        /// Specifies the template-part name for the fourth IPv4 segment editor.
        /// </summary>
        private const string PartSegment4 = "PART_Segment4";

        /// <summary>
        /// Stores the four segment editors supplied by the active control template.
        /// </summary>
        private readonly NumericTextBox?[] _segments = new NumericTextBox?[4];

        /// <summary>
        /// Indicates whether the control is currently synchronizing its aggregate and segment values.
        /// </summary>
        private bool _isSynchronizing;

        /// <summary>
        /// Stores the most recent complete IPv4 address accepted by the <see cref="Text"/> property.
        /// </summary>
        private string _lastValidText = string.Empty;

        /// <summary>
        /// Gets the command that copies the complete value of <see cref="Text"/> to the clipboard.
        /// </summary>
        /// <value>The routed command used by the control's context menu to copy the complete IPv4 address.</value>
        public static RoutedUICommand CopyAddressCommand { get; } = new(
            "Copy IP Address",
            nameof(CopyAddressCommand),
            typeof(IPv4TextBox));

        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(IPv4TextBox),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnTextChanged,
                CoerceText,
                false,
                UpdateSourceTrigger.PropertyChanged));

        /// <summary>
        /// Gets or sets the last complete, valid IPv4 address. Invalid assignments are ignored.
        /// </summary>
        /// <value>
        /// The most recent address containing exactly four decimal segments between <c>0</c> and <c>255</c>.
        /// The default is <see cref="string.Empty"/>.
        /// </value>
        /// <example>
        /// <code language="xaml">&lt;mosaic:IPv4TextBox Text=&quot;192.168.1.25&quot; /&gt;</code>
        /// </example>
        [Category("Common")]
        [Description("The last complete, valid IPv4 address entered in the control.")]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Initializes static members of the <see cref="IPv4TextBox"/> class.
        /// </summary>
        static IPv4TextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(IPv4TextBox),
                new FrameworkPropertyMetadata(typeof(IPv4TextBox)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPv4TextBox"/> class.
        /// </summary>
        public IPv4TextBox()
        {
            Focusable = true;
            DataObject.AddPastingHandler(this, OnPaste);
            CommandBindings.Add(new CommandBinding(CopyAddressCommand, OnCopyAddressExecuted, OnCopyAddressCanExecute));
        }

        /// <summary>
        /// Applies the active template, discovers the four segment editors, and synchronizes them with <see cref="Text"/>.
        /// </summary>
        /// <remarks>
        /// Detaches handlers from template parts supplied by the previous template before attaching handlers to the new parts.
        /// </remarks>
        public override void OnApplyTemplate()
        {
            DetachSegmentHandlers();
            base.OnApplyTemplate();

            _segments[0] = GetTemplateChild(PartSegment1) as NumericTextBox;
            _segments[1] = GetTemplateChild(PartSegment2) as NumericTextBox;
            _segments[2] = GetTemplateChild(PartSegment3) as NumericTextBox;
            _segments[3] = GetTemplateChild(PartSegment4) as NumericTextBox;

            AttachSegmentHandlers();

            if (TryParseIPv4(Text, out string[] values))
            {
                PopulateSegments(values);
            }
        }

        /// <summary>
        /// Creates the automation peer that exposes the control as an editable value.
        /// </summary>
        /// <returns>An automation peer that implements the UI Automation Value pattern.</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new IPv4TextBoxAutomationPeer(this);
        }

        /// <summary>
        /// Redirects focus received by the composite control to the first appropriate segment editor.
        /// </summary>
        /// <param name="e">The keyboard-focus change data.</param>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            if (ReferenceEquals(e.NewFocus, this))
            {
                FocusInitialSegment();
            }
        }

        /// <summary>
        /// Focuses the composite editor when the user clicks its non-segment chrome.
        /// </summary>
        /// <param name="e">The mouse-button event data.</param>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            if (!IsEventFromSegment(e.OriginalSource as DependencyObject))
            {
                Focus();
            }
        }

        /// <summary>
        /// Handles period and boundary-navigation keys before the focused segment processes them.
        /// </summary>
        /// <param name="e">The keyboard event data.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            int index = IndexOfSegment(e.OriginalSource as DependencyObject);
            if (index < 0 || _segments[index] is not { } segment)
            {
                return;
            }

            if (e.Key is Key.OemPeriod or Key.Decimal)
            {
                if (index < _segments.Length - 1 && IsValidSegment(segment.Text))
                {
                    FocusSegment(index + 1, selectAll: true);
                }

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Left && segment.SelectionLength == 0 && segment.CaretIndex == 0 && index > 0)
            {
                FocusSegment(index - 1, selectAll: false, placeCaretAtEnd: true);
                e.Handled = true;
            }
            else if (e.Key == Key.Right && segment.SelectionLength == 0 &&
                     segment.CaretIndex == segment.Text.Length && index < _segments.Length - 1)
            {
                FocusSegment(index + 1, selectAll: false);
                e.Handled = true;
            }
            else if (e.Key == Key.Back && string.IsNullOrEmpty(segment.Text) && index > 0)
            {
                FocusSegment(index - 1, selectAll: false, placeCaretAtEnd: true);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Synchronizes externally assigned aggregate text with the four segment editors.
        /// </summary>
        /// <param name="dependencyObject">The control whose aggregate text changed.</param>
        /// <param name="e">The dependency-property change data.</param>
        private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var control = (IPv4TextBox)dependencyObject;
            string text = (string)e.NewValue;
            control._lastValidText = text;

            if (control._isSynchronizing)
            {
                control.RaiseAutomationValueChanged(e.OldValue as string, text);
                return;
            }

            if (TryParseIPv4(text, out string[] values))
            {
                control.PopulateSegments(values);
                control.RaiseAutomationValueChanged(e.OldValue as string, text);
            }
        }

        /// <summary>
        /// Rejects incomplete or invalid aggregate text by returning the last accepted IPv4 address.
        /// </summary>
        /// <param name="dependencyObject">The control receiving the proposed value.</param>
        /// <param name="baseValue">The proposed aggregate text.</param>
        /// <returns>The proposed valid address, or the control's last accepted address when the proposal is invalid.</returns>
        private static object CoerceText(DependencyObject dependencyObject, object baseValue)
        {
            var control = (IPv4TextBox)dependencyObject;
            return baseValue is string text && TryParseIPv4(text, out _)
                ? text
                : control._lastValidText;
        }

        /// <summary>
        /// Attaches change handlers to each segment editor supplied by the active template.
        /// </summary>
        private void AttachSegmentHandlers()
        {
            foreach (NumericTextBox? segment in _segments)
            {
                if (segment == null)
                {
                    continue;
                }

                segment.TextChanged += OnSegmentTextChanged;
            }
        }

        /// <summary>
        /// Detaches change handlers from the current segment editors and clears the cached template parts.
        /// </summary>
        private void DetachSegmentHandlers()
        {
            for (int index = 0; index < _segments.Length; index++)
            {
                if (_segments[index] is { } segment)
                {
                    segment.TextChanged -= OnSegmentTextChanged;
                    _segments[index] = null;
                }
            }
        }

        /// <summary>
        /// Updates the aggregate address after a segment edit and advances focus after a valid third digit.
        /// </summary>
        /// <param name="sender">The segment editor whose text changed.</param>
        /// <param name="e">The text-change event data.</param>
        private void OnSegmentTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSynchronizing || sender is not NumericTextBox segment)
            {
                return;
            }

            UpdateTextFromSegments();

            int index = Array.IndexOf(_segments, segment);
            if (index >= 0 && index < _segments.Length - 1 &&
                segment.Text.Length == 3 && IsValidSegment(segment.Text))
            {
                FocusSegment(index + 1, selectAll: true);
            }
        }

        /// <summary>
        /// Updates <see cref="Text"/> when all four segment editors contain valid values.
        /// </summary>
        /// <remarks>
        /// Leaves <see cref="Text"/> unchanged while any segment is empty or invalid so bindings retain the last complete address.
        /// </remarks>
        private void UpdateTextFromSegments()
        {
            string first = _segments[0]?.Text ?? string.Empty;
            string second = _segments[1]?.Text ?? string.Empty;
            string third = _segments[2]?.Text ?? string.Empty;
            string fourth = _segments[3]?.Text ?? string.Empty;

            if (!IsValidSegment(first) || !IsValidSegment(second) ||
                !IsValidSegment(third) || !IsValidSegment(fourth))
            {
                return;
            }

            SetTextFromSegments($"{first}.{second}.{third}.{fourth}");
        }

        /// <summary>
        /// Assigns a complete address to <see cref="Text"/> without repopulating the segment editors recursively.
        /// </summary>
        /// <param name="value">The complete address assembled from the segment editors.</param>
        private void SetTextFromSegments(string value)
        {
            _isSynchronizing = true;
            try
            {
                SetCurrentValue(TextProperty, value);
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        /// <summary>
        /// Populates all available segment editors while suppressing aggregate-value synchronization.
        /// </summary>
        /// <param name="values">The four validated decimal segment strings.</param>
        private void PopulateSegments(IReadOnlyList<string> values)
        {
            _isSynchronizing = true;
            try
            {
                for (int index = 0; index < _segments.Length; index++)
                {
                    _segments[index]?.SetCurrentValue(TextBox.TextProperty, values[index]);
                }
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        /// <summary>
        /// Focuses the first empty segment, or selects the first segment when every segment has a value.
        /// </summary>
        private void FocusInitialSegment()
        {
            for (int index = 0; index < _segments.Length; index++)
            {
                if (string.IsNullOrEmpty(_segments[index]?.Text))
                {
                    FocusSegment(index, selectAll: false);
                    return;
                }
            }

            FocusSegment(0, selectAll: true);
        }

        /// <summary>
        /// Focuses a segment and configures its selection or caret for the requested navigation behavior.
        /// </summary>
        /// <param name="index">The zero-based segment index.</param>
        /// <param name="selectAll"><see langword="true"/> to select existing text; otherwise, <see langword="false"/>.</param>
        /// <param name="placeCaretAtEnd"><see langword="true"/> to place the caret after the final character; otherwise, <see langword="false"/> to place it before the first character.</param>
        private void FocusSegment(int index, bool selectAll, bool placeCaretAtEnd = false)
        {
            if (index < 0 || index >= _segments.Length || _segments[index] is not { } segment)
            {
                return;
            }

            segment.Focus();

            if (selectAll && !string.IsNullOrEmpty(segment.Text))
            {
                segment.SelectAll();
            }
            else
            {
                segment.CaretIndex = placeCaretAtEnd ? segment.Text.Length : 0;
            }
        }

        /// <summary>
        /// Applies a complete valid pasted address transactionally and rejects all other pasted content.
        /// </summary>
        /// <param name="sender">The element that registered the paste handler.</param>
        /// <param name="e">The data-object paste event data.</param>
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText) &&
                e.SourceDataObject.GetData(DataFormats.UnicodeText) is string text &&
                TryParseIPv4(text, out string[] values))
            {
                PopulateSegments(values);
                SetTextFromSegments(text);

                int sourceIndex = IndexOfSegment(e.OriginalSource as DependencyObject);
                FocusSegment(sourceIndex >= 0 ? sourceIndex : 3, selectAll: false, placeCaretAtEnd: true);
            }

            // Complete-address paste is transactional; never allow a child TextBox to perform a partial paste.
            e.CancelCommand();
            e.Handled = true;
        }

        /// <summary>
        /// Determines whether the complete address can be copied to the clipboard.
        /// </summary>
        /// <param name="sender">The command-binding owner.</param>
        /// <param name="e">The command-query event data.</param>
        private void OnCopyAddressCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsEnabled && TryParseIPv4(Text, out _);
            e.Handled = true;
        }

        /// <summary>
        /// Copies the complete valid address to the clipboard.
        /// </summary>
        /// <param name="sender">The command-binding owner.</param>
        /// <param name="e">The command-execution event data.</param>
        private void OnCopyAddressExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(Text);
            }
            catch (ExternalException)
            {
                // Another process can temporarily hold the Windows clipboard open; leave its contents unchanged.
            }

            e.Handled = true;
        }

        /// <summary>
        /// Parses a strict four-part decimal IPv4 address without accepting legacy abbreviated forms.
        /// </summary>
        /// <param name="text">The candidate aggregate address.</param>
        /// <param name="segments">When this method returns, contains the four validated segment strings. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true"/> if <paramref name="text"/> is a valid strict IPv4 address; otherwise, <see langword="false"/>.</returns>
        private static bool TryParseIPv4(string? text, out string[] segments)
        {
            segments = Array.Empty<string>();
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            string[] candidates = text.Split('.');
            if (candidates.Length != 4)
            {
                return false;
            }

            foreach (string candidate in candidates)
            {
                if (!IsValidSegment(candidate))
                {
                    return false;
                }
            }

            segments = candidates;
            return true;
        }

        /// <summary>
        /// Determines whether text contains one to three decimal digits representing a value from <c>0</c> through <c>255</c>.
        /// </summary>
        /// <param name="text">The candidate segment text.</param>
        /// <returns><see langword="true"/> if <paramref name="text"/> is a valid IPv4 segment; otherwise, <see langword="false"/>.</returns>
        private static bool IsValidSegment(string? text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 3)
            {
                return false;
            }

            foreach (char character in text)
            {
                if (character is < '0' or > '9')
                {
                    return false;
                }
            }

            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value) && value <= 255;
        }

        /// <summary>
        /// Locates the segment editor that contains an input-event source.
        /// </summary>
        /// <param name="source">The original dependency-object event source.</param>
        /// <returns>The zero-based segment index, or <c>-1</c> when the source is outside every segment editor.</returns>
        private int IndexOfSegment(DependencyObject? source)
        {
            while (source != null && !ReferenceEquals(source, this))
            {
                for (int index = 0; index < _segments.Length; index++)
                {
                    if (ReferenceEquals(source, _segments[index]))
                    {
                        return index;
                    }
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return -1;
        }

        /// <summary>
        /// Determines whether an input event originated within one of the segment editors.
        /// </summary>
        /// <param name="source">The original dependency-object event source.</param>
        /// <returns><see langword="true"/> if the source belongs to a segment editor; otherwise, <see langword="false"/>.</returns>
        private bool IsEventFromSegment(DependencyObject? source)
        {
            return IndexOfSegment(source) >= 0;
        }

        /// <summary>
        /// Notifies an existing automation peer that the aggregate address changed.
        /// </summary>
        /// <param name="oldValue">The address value before the change.</param>
        /// <param name="newValue">The address value after the change.</param>
        private void RaiseAutomationValueChanged(string? oldValue, string? newValue)
        {
            if (UIElementAutomationPeer.FromElement(this) is IPv4TextBoxAutomationPeer peer)
            {
                peer.RaiseValueChanged(oldValue ?? string.Empty, newValue ?? string.Empty);
            }
        }

        /// <summary>
        /// Exposes an <see cref="IPv4TextBox"/> to UI Automation clients as a single editable value.
        /// </summary>
        private sealed class IPv4TextBoxAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
        {
            /// <summary>
            /// Stores the control represented by this automation peer.
            /// </summary>
            private readonly IPv4TextBox _owner;

            /// <summary>
            /// Initializes a new instance of the <see cref="IPv4TextBoxAutomationPeer"/> class.
            /// </summary>
            /// <param name="owner">The IPv4 editor represented by the automation peer.</param>
            internal IPv4TextBoxAutomationPeer(IPv4TextBox owner) : base(owner)
            {
                _owner = owner;
            }

            /// <inheritdoc />
            bool IValueProvider.IsReadOnly => !_owner.IsEnabled;

            /// <inheritdoc />
            string IValueProvider.Value => _owner.Text;

            /// <inheritdoc />
            protected override string GetClassNameCore() => nameof(IPv4TextBox);

            /// <inheritdoc />
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

            /// <summary>
            /// Retrieves the provider for the requested UI Automation control pattern.
            /// </summary>
            /// <param name="patternInterface">One of the enumeration values that specifies the requested control pattern.</param>
            /// <returns>This peer for the Value pattern; otherwise, the provider returned by the base peer.</returns>
            public override object? GetPattern(PatternInterface patternInterface)
            {
                return patternInterface == PatternInterface.Value ? this : base.GetPattern(patternInterface);
            }

            /// <summary>
            /// Assigns a complete IPv4 address on behalf of a UI Automation client.
            /// </summary>
            /// <param name="value">The proposed aggregate IPv4 address.</param>
            /// <exception cref="ElementNotEnabledException">The owning control is disabled.</exception>
            void IValueProvider.SetValue(string value)
            {
                if (!_owner.IsEnabled)
                {
                    throw new ElementNotEnabledException();
                }

                _owner.SetCurrentValue(TextProperty, value);
            }

            /// <summary>
            /// Raises the UI Automation property-change event for the Value pattern.
            /// </summary>
            /// <param name="oldValue">The aggregate address before the change.</param>
            /// <param name="newValue">The aggregate address after the change.</param>
            internal void RaiseValueChanged(string oldValue, string newValue)
            {
                RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValue, newValue);
            }
        }
    }
}
