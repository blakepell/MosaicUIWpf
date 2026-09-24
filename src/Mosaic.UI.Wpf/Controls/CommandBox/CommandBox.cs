/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using Mosaic.UI.Wpf.Behaviors;
using Mosaic.UI.Wpf.Themes;
using System.Windows.Automation.Peers;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A single line command input built on AvalonEdit's <see cref="TextEditor"/>. Pressing
    /// <c>Enter</c> raises <see cref="CommandExecuted"/>, <c>Up</c> and <c>Down</c> walk a
    /// persistent command history and an optional block caret gives the box a terminal feel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The control is deliberately general purpose: it dispatches the command and gets out of the
    /// way. What the command means is entirely up to the host, which handles
    /// <see cref="CommandExecuted"/> (or binds <see cref="Command"/>). There is no completion
    /// window, no syntax highlighting and no persistence; <see cref="History"/> is exposed so a
    /// host that wants to save and restore commands can do so with
    /// <see cref="ImportHistory(IEnumerable{string}?)"/>.
    /// </para>
    /// <para>
    /// <b>Single line.</b> The document is kept to one line at all times. Word wrap and the scroll
    /// bars are off, the editor's own newline commands are removed, and text arriving from a paste,
    /// an IME or a programmatic assignment has its line breaks collapsed to spaces rather than
    /// being rejected.
    /// </para>
    /// <para>
    /// <b>Dispatch order.</b> On <c>Enter</c> the text is normalized, the tunneling
    /// <see cref="PreviewCommandExecuted"/> event is raised (a handler may rewrite
    /// <see cref="CommandExecutedEventArgs.Command"/> or veto the command by setting
    /// <see cref="RoutedEventArgs.Handled"/>), <see cref="CommitBehavior"/> is applied to the box,
    /// <see cref="CommandExecuted"/> is raised, the command is recorded in <see cref="History"/>
    /// unless a handler cleared <see cref="CommandExecutedEventArgs.AddToHistory"/>, and finally
    /// <see cref="Command"/> is executed. The commit behavior runs before the public event so a
    /// handler that pushes new text into the box wins.
    /// </para>
    /// <para>
    /// This control subclasses <see cref="TextEditor"/> rather than following the library's usual
    /// lookless <c>CustomControl</c> pattern because AvalonEdit's editing pipeline is not
    /// template-friendly, the same reason <see cref="SyntaxEditor"/> and
    /// <see cref="Mosaic.UI.Wpf.Controls.VT52Terminal.VT52Terminal"/> do the same. Theme aware
    /// defaults are therefore applied as <c>DynamicResource</c> references in the constructor
    /// instead of coming from a control template.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code lang="XAML"><![CDATA[
    /// <mosaic:CommandBox Watermark="Enter a command..."
    ///                    UseBlockCaret="True"
    ///                    CommitBehavior="SelectAll"
    ///                    CommandExecuted="OnCommandExecuted" />
    /// ]]></code>
    /// </example>
    [DefaultEvent(nameof(CommandExecuted))]
    [DefaultProperty(nameof(Watermark))]
    public class CommandBox : TextEditor, IBlockCaretOptions
    {
        /// <summary>
        /// The command history and the Up/Down cursor that walks it.
        /// </summary>
        private readonly CommandBoxHistory _history = new();

        /// <summary>
        /// Paints <see cref="Watermark"/> behind an empty document.
        /// </summary>
        private readonly CommandBoxWatermarkRenderer _watermarkRenderer;

        /// <summary>
        /// The installed prompt margin, or <see langword="null"/> when the prompt is off.
        /// </summary>
        private CommandBoxPromptMargin? _promptMargin;

        /// <summary>
        /// The installed block caret renderer, or <see langword="null"/> when the block caret is off.
        /// </summary>
        private AvalonEditBlockCaretRenderer? _blockCaretRenderer;

        /// <summary>
        /// Set while the control is itself replacing the text, so the history cursor and the draft
        /// are not clobbered by the resulting text-changed notification.
        /// </summary>
        private bool _suppressHistoryReset;

        /// <summary>
        /// Guards the re-entrant assignment that collapses line breaks out of the document.
        /// </summary>
        private bool _sanitizing;

        /// <summary>
        /// The document the line break watch is currently subscribed to.
        /// </summary>
        private TextDocument? _trackedDocument;

        /// <summary>
        /// The text as it was the last time it changed, used to report the old value to automation.
        /// </summary>
        private string _lastReportedText = string.Empty;

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="PreviewCommandExecuted"/> routed event.
        /// </summary>
        public static readonly RoutedEvent PreviewCommandExecutedEvent = EventManager.RegisterRoutedEvent(
            nameof(PreviewCommandExecuted),
            RoutingStrategy.Tunnel,
            typeof(EventHandler<CommandExecutedEventArgs>),
            typeof(CommandBox));

        /// <summary>
        /// Identifies the <see cref="CommandExecuted"/> routed event.
        /// </summary>
        public static readonly RoutedEvent CommandExecutedEvent = EventManager.RegisterRoutedEvent(
            nameof(CommandExecuted),
            RoutingStrategy.Bubble,
            typeof(EventHandler<CommandExecutedEventArgs>),
            typeof(CommandBox));

        /// <summary>
        /// Identifies the <see cref="EscapePressed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent EscapePressedEvent = EventManager.RegisterRoutedEvent(
            nameof(EscapePressed),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(CommandBox));

        /// <summary>
        /// Raised before a command is dispatched. A handler may rewrite
        /// <see cref="CommandExecutedEventArgs.Command"/>, or veto the command outright by setting
        /// <see cref="RoutedEventArgs.Handled"/> to <see langword="true"/>, in which case the commit
        /// behavior is not applied and nothing is recorded in the history.
        /// </summary>
        public event EventHandler<CommandExecutedEventArgs> PreviewCommandExecuted
        {
            add => this.AddHandler(PreviewCommandExecutedEvent, value);
            remove => this.RemoveHandler(PreviewCommandExecutedEvent, value);
        }

        /// <summary>
        /// Raised when the user presses <c>Enter</c> on a command. The commit behavior has already
        /// been applied to the box by the time this fires, so a handler is free to push new text
        /// into it.
        /// </summary>
        public event EventHandler<CommandExecutedEventArgs> CommandExecuted
        {
            add => this.AddHandler(CommandExecutedEvent, value);
            remove => this.RemoveHandler(CommandExecutedEvent, value);
        }

        /// <summary>
        /// Raised when the user presses <c>Escape</c>, whether or not <see cref="ClearOnEscape"/>
        /// cleared the box. A host typically uses this to move focus somewhere else.
        /// </summary>
        public event RoutedEventHandler EscapePressed
        {
            add => this.AddHandler(EscapePressedEvent, value);
            remove => this.RemoveHandler(EscapePressedEvent, value);
        }

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="CommitBehavior"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommitBehaviorProperty = DependencyProperty.Register(
            nameof(CommitBehavior),
            typeof(CommandBoxCommitBehavior),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(CommandBoxCommitBehavior.SelectAll));

        /// <summary>
        /// Identifies the <see cref="IsHistoryEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsHistoryEnabledProperty = DependencyProperty.Register(
            nameof(IsHistoryEnabled),
            typeof(bool),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="MaxHistoryItems"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxHistoryItemsProperty = DependencyProperty.Register(
            nameof(MaxHistoryItems),
            typeof(int),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(100, static (d, e) => ((CommandBox)d)._history.MaxItems = (int)e.NewValue));

        /// <summary>
        /// Identifies the <see cref="HistoryDuplicatePolicy"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HistoryDuplicatePolicyProperty = DependencyProperty.Register(
            nameof(HistoryDuplicatePolicy),
            typeof(HistoryDuplicatePolicy),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(
                HistoryDuplicatePolicy.SkipConsecutive,
                static (d, e) => ((CommandBox)d)._history.DuplicatePolicy = (HistoryDuplicatePolicy)e.NewValue));

        /// <summary>
        /// Identifies the <see cref="UseBlockCaret"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UseBlockCaretProperty = DependencyProperty.Register(
            nameof(UseBlockCaret),
            typeof(bool),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(false, static (d, e) => ((CommandBox)d).ApplyBlockCaret((bool)e.NewValue)));

        /// <summary>
        /// Identifies the <see cref="BlockCaretBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BlockCaretBrushProperty = DependencyProperty.Register(
            nameof(BlockCaretBrush),
            typeof(Brush),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(null, static (d, _) => ((CommandBox)d).InvalidateCaretLayer()));

        /// <summary>
        /// Identifies the <see cref="BlockCaretTextBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BlockCaretTextBrushProperty = DependencyProperty.Register(
            nameof(BlockCaretTextBrush),
            typeof(Brush),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(null, static (d, _) => ((CommandBox)d).InvalidateCaretLayer()));

        /// <summary>
        /// Identifies the <see cref="BlockCaretOpacity"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BlockCaretOpacityProperty = DependencyProperty.Register(
            nameof(BlockCaretOpacity),
            typeof(double),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(0.8, static (d, _) => ((CommandBox)d).InvalidateCaretLayer()));

        /// <summary>
        /// Identifies the <see cref="ClearOnEscape"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClearOnEscapeProperty = DependencyProperty.Register(
            nameof(ClearOnEscape),
            typeof(bool),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="SelectAllOnFocus"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectAllOnFocusProperty = DependencyProperty.Register(
            nameof(SelectAllOnFocus),
            typeof(bool),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="SelectAllOnMouseFocus"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectAllOnMouseFocusProperty = DependencyProperty.Register(
            nameof(SelectAllOnMouseFocus),
            typeof(bool),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="IsTabCompletionEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsTabCompletionEnabledProperty = DependencyProperty.Register(
            nameof(IsTabCompletionEnabled),
            typeof(bool),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="TrimCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrimCommandProperty = DependencyProperty.Register(
            nameof(TrimCommand),
            typeof(bool),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="AllowEmptyCommands"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowEmptyCommandsProperty = DependencyProperty.Register(
            nameof(AllowEmptyCommands),
            typeof(bool),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="ShowPrompt"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowPromptProperty = DependencyProperty.Register(
            nameof(ShowPrompt),
            typeof(bool),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(false, static (d, e) => ((CommandBox)d).ApplyPrompt((bool)e.NewValue)));

        /// <summary>
        /// Identifies the <see cref="Prompt"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PromptProperty = DependencyProperty.Register(
            nameof(Prompt),
            typeof(string),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(">", static (d, _) => ((CommandBox)d).RefreshPrompt()));

        /// <summary>
        /// Identifies the <see cref="PromptBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PromptBrushProperty = DependencyProperty.Register(
            nameof(PromptBrush),
            typeof(Brush),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(null, static (d, _) => ((CommandBox)d).RefreshPrompt()));

        /// <summary>
        /// Identifies the <see cref="PromptPadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PromptPaddingProperty = DependencyProperty.Register(
            nameof(PromptPadding),
            typeof(Thickness),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(new Thickness(2, 0, 6, 0), static (d, _) => ((CommandBox)d).RefreshPrompt()));

        /// <summary>
        /// Identifies the <see cref="Watermark"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(
            nameof(Watermark),
            typeof(string),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(string.Empty, static (d, _) => ((CommandBox)d).InvalidateBackgroundLayer()));

        /// <summary>
        /// Identifies the <see cref="WatermarkBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WatermarkBrushProperty = DependencyProperty.Register(
            nameof(WatermarkBrush),
            typeof(Brush),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(null, static (d, _) => ((CommandBox)d).InvalidateBackgroundLayer()));

        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="CommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(CommandBox),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// What the box does with the text it just dispatched. Defaults to
        /// <see cref="CommandBoxCommitBehavior.SelectAll"/>, which leaves the command visible and
        /// selected so the next keystroke replaces it.
        /// </summary>
        [Category("Mosaic")]
        [Description("What the box does with the text after a command is executed: clear it, keep it, or select it.")]
        public CommandBoxCommitBehavior CommitBehavior
        {
            get => (CommandBoxCommitBehavior)this.GetValue(CommitBehaviorProperty);
            set => this.SetValue(CommitBehaviorProperty, value);
        }

        /// <summary>
        /// Whether executed commands are recorded and the arrow keys walk the history. Defaults to
        /// <see langword="true"/>.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether executed commands are recorded and Up/Down walk the command history.")]
        public bool IsHistoryEnabled
        {
            get => (bool)this.GetValue(IsHistoryEnabledProperty);
            set => this.SetValue(IsHistoryEnabledProperty, value);
        }

        /// <summary>
        /// The maximum number of commands retained by <see cref="History"/>. Zero means unlimited.
        /// Defaults to 100.
        /// </summary>
        [Category("Mosaic")]
        [Description("The maximum number of commands retained in the history. Zero means unlimited.")]
        public int MaxHistoryItems
        {
            get => (int)this.GetValue(MaxHistoryItemsProperty);
            set => this.SetValue(MaxHistoryItemsProperty, value);
        }

        /// <summary>
        /// Whether a command that repeats an earlier one is recorded again. Defaults to
        /// <see cref="Controls.HistoryDuplicatePolicy.SkipConsecutive"/>.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether a command that repeats an earlier one is recorded in the history again.")]
        public HistoryDuplicatePolicy HistoryDuplicatePolicy
        {
            get => (HistoryDuplicatePolicy)this.GetValue(HistoryDuplicatePolicyProperty);
            set => this.SetValue(HistoryDuplicatePolicyProperty, value);
        }

        /// <summary>
        /// Whether the thin line caret is replaced with a solid block caret. Defaults to
        /// <see langword="false"/>. The renderer is installed and removed as this changes, so it can
        /// be toggled or bound at runtime.
        /// </summary>
        [Category("Mosaic")]
        [Description("Replaces the thin line caret with a solid terminal style block caret.")]
        public bool UseBlockCaret
        {
            get => (bool)this.GetValue(UseBlockCaretProperty);
            set => this.SetValue(UseBlockCaretProperty, value);
        }

        /// <summary>
        /// The brush the block caret is filled with. When <see langword="null"/> (the default) the
        /// control's <see cref="Control.Foreground"/> is used, so the caret follows a theme switch
        /// without extra wiring.
        /// </summary>
        [Category("Mosaic")]
        [Description("The brush the block caret is filled with. Null uses the control foreground.")]
        public Brush? BlockCaretBrush
        {
            get => (Brush?)this.GetValue(BlockCaretBrushProperty);
            set => this.SetValue(BlockCaretBrushProperty, value);
        }

        /// <summary>
        /// The brush the character under the block caret is re-drawn in. When <see langword="null"/>
        /// (the default) black or white is chosen automatically, whichever reads better on the
        /// composited block color.
        /// </summary>
        [Category("Mosaic")]
        [Description("The brush the character under the block caret is re-drawn in. Null picks a contrasting color.")]
        public Brush? BlockCaretTextBrush
        {
            get => (Brush?)this.GetValue(BlockCaretTextBrushProperty);
            set => this.SetValue(BlockCaretTextBrushProperty, value);
        }

        /// <summary>
        /// The opacity of the block caret, from 0.0 to 1.0. Defaults to 0.8.
        /// </summary>
        [Category("Mosaic")]
        [Description("The opacity of the block caret, from 0.0 to 1.0.")]
        public double BlockCaretOpacity
        {
            get => (double)this.GetValue(BlockCaretOpacityProperty);
            set => this.SetValue(BlockCaretOpacityProperty, value);
        }

        /// <summary>
        /// Whether <c>Escape</c> clears the box. Defaults to <see langword="true"/>.
        /// <see cref="EscapePressed"/> is raised either way.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether pressing Escape clears the command text.")]
        public bool ClearOnEscape
        {
            get => (bool)this.GetValue(ClearOnEscapeProperty);
            set => this.SetValue(ClearOnEscapeProperty, value);
        }

        /// <summary>
        /// Whether the existing text is selected when the box receives keyboard focus. Defaults to
        /// <see langword="true"/>.
        /// </summary>
        [Category("Mosaic")]
        [Description("Selects the existing text when the box receives keyboard focus.")]
        public bool SelectAllOnFocus
        {
            get => (bool)this.GetValue(SelectAllOnFocusProperty);
            set => this.SetValue(SelectAllOnFocusProperty, value);
        }

        /// <summary>
        /// Whether the first click into an unfocused box focuses it and selects everything rather
        /// than placing the caret. Defaults to <see langword="true"/>. Subsequent clicks behave
        /// normally.
        /// </summary>
        [Category("Mosaic")]
        [Description("The first click into an unfocused box focuses it and selects all of the text.")]
        public bool SelectAllOnMouseFocus
        {
            get => (bool)this.GetValue(SelectAllOnMouseFocusProperty);
            set => this.SetValue(SelectAllOnMouseFocusProperty, value);
        }

        /// <summary>
        /// Whether <c>Tab</c> completes the current text from the most recent matching history
        /// entry. Defaults to <see langword="true"/>. When there is no match the key falls through
        /// to normal focus navigation.
        /// </summary>
        [Category("Mosaic")]
        [Description("Tab completes the current text from the most recent matching history entry.")]
        public bool IsTabCompletionEnabled
        {
            get => (bool)this.GetValue(IsTabCompletionEnabledProperty);
            set => this.SetValue(IsTabCompletionEnabledProperty, value);
        }

        /// <summary>
        /// Whether leading and trailing whitespace is stripped from a command before it is
        /// dispatched. Defaults to <see langword="true"/>.
        /// </summary>
        [Category("Mosaic")]
        [Description("Strips leading and trailing whitespace from a command before it is dispatched.")]
        public bool TrimCommand
        {
            get => (bool)this.GetValue(TrimCommandProperty);
            set => this.SetValue(TrimCommandProperty, value);
        }

        /// <summary>
        /// Whether pressing <c>Enter</c> on an empty box dispatches an empty command. Defaults to
        /// <see langword="false"/>, which makes a bare <c>Enter</c> do nothing.
        /// </summary>
        [Category("Mosaic")]
        [Description("Allows pressing Enter on an empty box to dispatch an empty command.")]
        public bool AllowEmptyCommands
        {
            get => (bool)this.GetValue(AllowEmptyCommandsProperty);
            set => this.SetValue(AllowEmptyCommandsProperty, value);
        }

        /// <summary>
        /// Whether the <see cref="Prompt"/> glyph is shown at the left edge of the box. The prompt
        /// is drawn inside the control's own border and background, and the editable text starts to
        /// the right of it.
        /// </summary>
        [Category("Mosaic")]
        [Description("Shows a prompt glyph at the left edge of the box, inside its border.")]
        public bool ShowPrompt
        {
            get => (bool)this.GetValue(ShowPromptProperty);
            set => this.SetValue(ShowPromptProperty, value);
        }

        /// <summary>
        /// The prompt glyph drawn when <see cref="ShowPrompt"/> is <see langword="true"/>. It is
        /// decoration only: it is never part of <see cref="TextEditor.Text"/> and never part of the
        /// dispatched command.
        /// </summary>
        [Category("Mosaic")]
        [Description("The prompt glyph drawn when ShowPrompt is true. Decoration only; never part of the command.")]
        public string Prompt
        {
            get => (string)this.GetValue(PromptProperty);
            set => this.SetValue(PromptProperty, value);
        }

        /// <summary>
        /// The brush the <see cref="Prompt"/> is painted with. When <see langword="null"/> (the
        /// default) the control's <see cref="Control.Foreground"/> is used.
        /// </summary>
        [Category("Mosaic")]
        [Description("The brush the prompt is painted with. Null uses the foreground.")]
        public Brush? PromptBrush
        {
            get => (Brush?)this.GetValue(PromptBrushProperty);
            set => this.SetValue(PromptBrushProperty, value);
        }

        /// <summary>
        /// The space around the <see cref="Prompt"/>: the left value separates it from the border
        /// and the right value separates it from where the user starts typing. This sits inside the
        /// control's own <see cref="Control.Padding"/>.
        /// </summary>
        [Category("Mosaic")]
        [Description("Space around the prompt; left separates it from the border, right from the text.")]
        public Thickness PromptPadding
        {
            get => (Thickness)this.GetValue(PromptPaddingProperty);
            set => this.SetValue(PromptPaddingProperty, value);
        }

        /// <summary>
        /// Placeholder text painted behind the box while it is empty.
        /// </summary>
        [Category("Mosaic")]
        [Description("Placeholder text shown while the box is empty.")]
        public string Watermark
        {
            get => (string)this.GetValue(WatermarkProperty);
            set => this.SetValue(WatermarkProperty, value);
        }

        /// <summary>
        /// The brush the <see cref="Watermark"/> is painted with. When <see langword="null"/> (the
        /// default) the control's <see cref="Control.Foreground"/> at half opacity is used.
        /// </summary>
        [Category("Mosaic")]
        [Description("The brush the watermark is painted with. Null uses the foreground at half opacity.")]
        public Brush? WatermarkBrush
        {
            get => (Brush?)this.GetValue(WatermarkBrushProperty);
            set => this.SetValue(WatermarkBrushProperty, value);
        }

        /// <summary>
        /// An optional command executed after <see cref="CommandExecuted"/> has been raised, for
        /// MVVM hosts that would rather not handle a routed event.
        /// </summary>
        [Category("Mosaic")]
        [Description("A command executed after the CommandExecuted event has been raised.")]
        public ICommand? Command
        {
            get => (ICommand?)this.GetValue(CommandProperty);
            set => this.SetValue(CommandProperty, value);
        }

        /// <summary>
        /// The parameter passed to <see cref="Command"/>. When left <see langword="null"/> the
        /// command text is passed instead.
        /// </summary>
        [Category("Mosaic")]
        [Description("The parameter passed to Command. Null passes the command text.")]
        public object? CommandParameter
        {
            get => this.GetValue(CommandParameterProperty);
            set => this.SetValue(CommandParameterProperty, value);
        }

        #endregion

        /// <summary>
        /// The command history behind the box. Exposed so a host can persist and restore it;
        /// storage is not the control's job.
        /// </summary>
        public CommandBoxHistory History => _history;

        /// <summary>
        /// Creates a new <see cref="CommandBox"/>.
        /// </summary>
        public CommandBox()
        {
            this.FontFamily = new FontFamily("Consolas");
            this.WordWrap = false;
            this.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            this.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            this.Options.EnableEmailHyperlinks = false;
            this.Options.EnableHyperlinks = false;
            this.Options.ShowBoxForControlCharacters = false;
            this.Options.AllowScrollBelowDocument = false;

            // Theme aware defaults. These are resource references rather than literal brushes so a
            // theme switch carries through without the control having to subscribe to anything.
            this.SetResourceReference(BackgroundProperty, MosaicTheme.ControlBackgroundBrush);
            this.SetResourceReference(ForegroundProperty, MosaicTheme.ControlForegroundBrush);
            this.SetResourceReference(BorderBrushProperty, MosaicTheme.ControlBorderBrush);

            _watermarkRenderer = new CommandBoxWatermarkRenderer(this);
            this.TextArea.TextView.BackgroundRenderers.Add(_watermarkRenderer);

            var contextMenu = new ContextMenu();
            contextMenu.Items.Add(new MenuItem
            {
                Header = "Cu_t",
                Command = ApplicationCommands.Cut,
                CommandTarget = this.TextArea,
                InputGestureText = "Ctrl+X"
            });
            contextMenu.Items.Add(new MenuItem
            {
                Header = "_Copy",
                Command = ApplicationCommands.Copy,
                CommandTarget = this.TextArea,
                InputGestureText = "Ctrl+C"
            });
            contextMenu.Items.Add(new MenuItem
            {
                Header = "_Paste",
                Command = ApplicationCommands.Paste,
                CommandTarget = this.TextArea,
                InputGestureText = "Ctrl+V"
            });
            this.SetCurrentValue(ContextMenuProperty, contextMenu);

            this.RemoveNewLineCommands();
            DataObject.AddPastingHandler(this, this.OnPasting);

            this.DocumentChanged += this.OnDocumentChanged;
            this.TrackDocument();

            _history.MaxItems = this.MaxHistoryItems;
            _history.DuplicatePolicy = this.HistoryDuplicatePolicy;
        }

        #region History

        /// <summary>
        /// Appends previously persisted commands to <see cref="History"/>, oldest first.
        /// </summary>
        /// <param name="commands">The commands to import.</param>
        public void ImportHistory(IEnumerable<string>? commands)
        {
            _history.Import(commands);
            _history.ResetCursor(this.Text);
        }

        /// <summary>
        /// Empties <see cref="History"/> and resets the Up/Down cursor.
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
            _history.ResetCursor(this.Text);
        }

        #endregion

        #region Command Dispatch

        /// <summary>
        /// Normalizes the current text and dispatches it as a command, exactly as pressing
        /// <c>Enter</c> would.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when a command was dispatched, <see langword="false"/> when the
        /// box was empty or a <see cref="PreviewCommandExecuted"/> handler vetoed it.
        /// </returns>
        public bool ExecuteCommand()
        {
            string text = this.Normalize(this.Text);

            if (text.Length == 0 && !this.AllowEmptyCommands)
            {
                return false;
            }

            var preview = new CommandExecutedEventArgs(PreviewCommandExecutedEvent, this, text);
            this.RaiseEvent(preview);

            if (preview.Handled)
            {
                return false;
            }

            text = preview.Command;

            this.ApplyCommitBehavior();

            var executed = new CommandExecutedEventArgs(CommandExecutedEvent, this, text)
            {
                AddToHistory = preview.AddToHistory
            };

            this.RaiseEvent(executed);

            if (this.IsHistoryEnabled && executed.AddToHistory)
            {
                _history.Add(text);
            }

            _history.ResetCursor(this.Text);

            var command = this.Command;

            if (command != null)
            {
                object? parameter = this.CommandParameter ?? text;

                if (command.CanExecute(parameter))
                {
                    command.Execute(parameter);
                }
            }

            return true;
        }

        /// <summary>
        /// Applies <see cref="CommitBehavior"/> to the box after a command has been accepted.
        /// </summary>
        private void ApplyCommitBehavior()
        {
            switch (this.CommitBehavior)
            {
                case CommandBoxCommitBehavior.Clear:
                    this.SetTextInternal(string.Empty, selectAll: false);
                    break;
                case CommandBoxCommitBehavior.SelectAll:
                    this.SelectAll();
                    break;
                case CommandBoxCommitBehavior.Keep:
                default:
                    break;
            }
        }

        /// <summary>
        /// Collapses line breaks to spaces and, when <see cref="TrimCommand"/> is set, trims the
        /// result.
        /// </summary>
        /// <param name="value">The raw text to normalize.</param>
        private string Normalize(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string text = value.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');

            return this.TrimCommand ? text.Trim() : text;
        }

        #endregion

        #region Key Handling

        /// <summary>
        /// Handles the keys the command box owns before the editor sees them: <c>Enter</c> to
        /// dispatch, <c>Up</c> / <c>Down</c> to walk the history, <c>Escape</c> to clear and
        /// <c>Tab</c> to complete from the history.
        /// </summary>
        /// <param name="e">The key event.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Handled)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                    // Handled unconditionally, with or without modifiers, so no key combination can
                    // sneak a line break into a single line box.
                    e.Handled = true;
                    this.ExecuteCommand();
                    return;

                case Key.Up when this.IsHistoryEnabled && !this.IsReadOnly:
                    this.RecallHistory(_history.MoveBack());
                    e.Handled = true;
                    return;

                case Key.Down when this.IsHistoryEnabled && !this.IsReadOnly:
                    this.RecallHistory(_history.MoveForward());
                    e.Handled = true;
                    return;

                case Key.Escape:
                    e.Handled = this.HandleEscape();
                    this.RaiseEvent(new RoutedEventArgs(EscapePressedEvent, this));
                    return;

                case Key.Tab when this.IsTabCompletionEnabled && !this.IsReadOnly && Keyboard.Modifiers == ModifierKeys.None:
                    e.Handled = this.TryCompleteFromHistory();
                    return;
            }
        }

        /// <summary>
        /// Replaces the text with a command recalled from the history and parks the caret at the end
        /// with nothing selected.
        /// </summary>
        /// <param name="command">
        /// The recalled text, or <see langword="null"/> when the cursor could not move, in which
        /// case the box is left alone.
        /// </param>
        private void RecallHistory(string? command)
        {
            if (command == null)
            {
                return;
            }

            this.SetTextInternal(command, selectAll: false);
        }

        /// <summary>
        /// Clears the box when <see cref="ClearOnEscape"/> is set.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the key was consumed, which only happens when there was text
        /// to clear. Leaving an Escape on an already empty box unhandled lets it keep bubbling so a
        /// host can close a dialog or a popup with it.
        /// </returns>
        private bool HandleEscape()
        {
            if (!this.ClearOnEscape || this.IsReadOnly || this.Document == null || this.Document.TextLength == 0)
            {
                return false;
            }

            this.SetTextInternal(string.Empty, selectAll: false);
            _history.ResetCursor(string.Empty);

            return true;
        }

        /// <summary>
        /// Completes the current text from the most recent history entry that starts with it.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when a completion was applied. When nothing matches the key is
        /// left unhandled so <c>Tab</c> still moves focus.
        /// </returns>
        private bool TryCompleteFromHistory()
        {
            string prefix = this.Text;

            if (string.IsNullOrEmpty(prefix))
            {
                return false;
            }

            var items = _history.Items;

            // Walk backwards so the most recently entered match wins.
            for (int i = items.Count - 1; i >= 0; i--)
            {
                string candidate = items[i];

                if (candidate.Length > prefix.Length && candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    this.SetTextInternal(candidate, selectAll: false);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Focus and Mouse

        /// <summary>
        /// Selects the existing text when the box gains keyboard focus, if
        /// <see cref="SelectAllOnFocus"/> is set.
        /// </summary>
        /// <param name="e">The focus event.</param>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            if (this.SelectAllOnFocus)
            {
                this.SelectAll();
            }
        }

        /// <summary>
        /// Turns the first click into an unfocused box into a focus-and-select-all, if
        /// <see cref="SelectAllOnMouseFocus"/> is set. The click itself is swallowed so it does not
        /// immediately collapse the selection back to a caret.
        /// </summary>
        /// <param name="e">The mouse event.</param>
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (this.SelectAllOnMouseFocus && e.ChangedButton == MouseButton.Left && !this.IsKeyboardFocusWithin)
            {
                this.Focus();
                this.SelectAll();
                e.Handled = true;
                return;
            }

            base.OnPreviewMouseDown(e);
        }

        #endregion

        #region Single Line Enforcement

        /// <summary>
        /// Keeps the history draft in step with what the user typed, notifies automation clients
        /// that the value changed and repaints the watermark.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);

            if (!_sanitizing)
            {
                this.UpdateAfterTextChanged();
            }
        }

        /// <summary>
        /// Brings the history draft, the automation value and the watermark up to date with what is
        /// currently in the box.
        /// </summary>
        private void UpdateAfterTextChanged()
        {
            string text = this.Text;

            if (!_suppressHistoryReset)
            {
                // The user typed, so whatever is on screen becomes the draft that walking forward
                // past the newest history entry restores.
                _history.ResetCursor(text);
            }

            string oldText = _lastReportedText;
            _lastReportedText = text;

            if (!string.Equals(oldText, text, StringComparison.Ordinal) &&
                UIElementAutomationPeer.FromElement(this) is CommandBoxAutomationPeer peer)
            {
                peer.RaiseValueChanged(oldText, text);
            }

            this.InvalidateBackgroundLayer();
        }

        /// <summary>
        /// Re-points the line break watch at the current document whenever the document is replaced.
        /// </summary>
        /// <param name="sender">The editor.</param>
        /// <param name="e">The event data.</param>
        private void OnDocumentChanged(object? sender, EventArgs e)
        {
            this.TrackDocument();
        }

        /// <summary>
        /// Subscribes to the current document so line breaks can be collapsed once a change has
        /// fully completed.
        /// </summary>
        private void TrackDocument()
        {
            if (_trackedDocument != null)
            {
                _trackedDocument.UpdateFinished -= this.OnDocumentUpdateFinished;
            }

            _trackedDocument = this.Document;

            if (_trackedDocument != null)
            {
                _trackedDocument.UpdateFinished += this.OnDocumentUpdateFinished;
            }
        }

        /// <summary>
        /// Collapses any line break that made it into the document to a space.
        /// </summary>
        /// <param name="sender">The document that finished updating.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This is the catch-all for every path that can introduce a line break: a programmatic
        /// assignment, an IME, a drop, or a paste that slipped past the pasting handler. It runs on
        /// <see cref="TextDocument.UpdateFinished"/> rather than in the text-changed notification
        /// because AvalonEdit still has an undo group open at that earlier point and will not accept
        /// a nested edit.
        /// </remarks>
        private void OnDocumentUpdateFinished(object? sender, EventArgs e)
        {
            if (_sanitizing || _trackedDocument == null)
            {
                return;
            }

            string text = _trackedDocument.Text;

            if (text.IndexOf('\n') < 0 && text.IndexOf('\r') < 0)
            {
                return;
            }

            _sanitizing = true;

            try
            {
                int caret = this.CaretOffset;
                string collapsed = text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');

                this.Text = collapsed;
                this.CaretOffset = Math.Min(caret, collapsed.Length);
            }
            finally
            {
                _sanitizing = false;
            }

            // The bookkeeping was skipped while sanitizing, so run it now against the collapsed text.
            this.UpdateAfterTextChanged();
        }

        /// <summary>
        /// Replaces the text without disturbing the history cursor, and parks the caret at the end.
        /// </summary>
        /// <param name="text">The text to place in the box.</param>
        /// <param name="selectAll">Whether the new text should be selected.</param>
        private void SetTextInternal(string text, bool selectAll)
        {
            _suppressHistoryReset = true;

            try
            {
                this.Text = text;

                if (selectAll)
                {
                    this.SelectAll();
                }
                else
                {
                    this.Select(text.Length, 0);
                }
            }
            finally
            {
                _suppressHistoryReset = false;
            }
        }

        /// <summary>
        /// Collapses line breaks in pasted text to spaces so a multi-line paste becomes a single
        /// line rather than being rejected.
        /// </summary>
        /// <param name="sender">The paste target.</param>
        /// <param name="e">The paste event.</param>
        private void OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true))
            {
                return;
            }

            if (e.SourceDataObject.GetData(DataFormats.UnicodeText, true) is not string text)
            {
                return;
            }

            if (text.IndexOf('\n') < 0 && text.IndexOf('\r') < 0)
            {
                return;
            }

            var data = new DataObject();
            data.SetData(DataFormats.UnicodeText, text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' '));
            e.DataObject = data;
        }

        /// <summary>
        /// Strips the editor's newline commands so a gesture such as <c>Ctrl+M</c> cannot insert a
        /// line break behind the key handler's back.
        /// </summary>
        private void RemoveNewLineCommands()
        {
            var bindings = this.TextArea.DefaultInputHandler?.Editing?.CommandBindings;

            if (bindings == null)
            {
                return;
            }

            var newLineBindings = bindings
                .Where(b => b.Command == System.Windows.Documents.EditingCommands.EnterParagraphBreak ||
                            b.Command == System.Windows.Documents.EditingCommands.EnterLineBreak)
                .ToList();

            foreach (var binding in newLineBindings)
            {
                bindings.Remove(binding);
            }
        }

        /// <summary>
        /// Reports the height of exactly one text line so the box never grows to a second row.
        /// </summary>
        /// <param name="constraint">The available size.</param>
        protected override Size MeasureOverride(Size constraint)
        {
            var size = base.MeasureOverride(constraint);
            double lineHeight = this.TextArea.TextView.DefaultLineHeight;

            if (lineHeight > 0)
            {
                double chrome = this.BorderThickness.Top + this.BorderThickness.Bottom +
                                this.Padding.Top + this.Padding.Bottom;

                size.Height = lineHeight + chrome;
            }

            return size;
        }

        #endregion

        #region Block Caret

        /// <summary>
        /// The brush the block caret is filled with, read by the shared renderer.
        /// </summary>
        Brush? IBlockCaretOptions.CaretBrush => this.BlockCaretBrush;

        /// <summary>
        /// The brush the covered character is re-drawn in, read by the shared renderer.
        /// </summary>
        Brush? IBlockCaretOptions.CaretTextBrush => this.BlockCaretTextBrush;

        /// <summary>
        /// The opacity of the block, read by the shared renderer.
        /// </summary>
        double IBlockCaretOptions.CaretOpacity => this.BlockCaretOpacity;

        /// <summary>
        /// Installs or removes the block caret renderer.
        /// </summary>
        /// <param name="enabled">Whether the block caret should be active.</param>
        private void ApplyBlockCaret(bool enabled)
        {
            if (enabled)
            {
                _blockCaretRenderer ??= AvalonEditBlockCaretRenderer.Install(this, this);
            }
            else if (_blockCaretRenderer != null)
            {
                AvalonEditBlockCaretRenderer.Uninstall(this, _blockCaretRenderer);
                _blockCaretRenderer = null;
            }
        }

        /// <summary>
        /// Forces the caret layer to redraw after one of the block caret appearance properties
        /// changes. The renderer reads those properties as it draws.
        /// </summary>
        private void InvalidateCaretLayer()
        {
            if (_blockCaretRenderer != null)
            {
                this.TextArea.TextView.InvalidateLayer(KnownLayer.Caret);
            }
        }

        /// <summary>
        /// Forces the background layer to redraw, which is where the watermark lives.
        /// </summary>
        private void InvalidateBackgroundLayer()
        {
            this.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        }

        #endregion

        #region Prompt

        /// <summary>
        /// Installs or removes the prompt margin. The margin is an AvalonEdit left margin, so it
        /// renders inside the editor's border and background and pushes the text to its right.
        /// </summary>
        /// <param name="enabled">Whether the prompt should be shown.</param>
        private void ApplyPrompt(bool enabled)
        {
            if (enabled)
            {
                if (_promptMargin != null)
                {
                    return;
                }

                _promptMargin = new CommandBoxPromptMargin(this);
                this.TextArea.LeftMargins.Add(_promptMargin);
            }
            else if (_promptMargin != null)
            {
                this.TextArea.LeftMargins.Remove(_promptMargin);
                _promptMargin = null;
            }
        }

        /// <summary>
        /// Re-measures and repaints the prompt after one of its appearance properties changes.
        /// </summary>
        private void RefreshPrompt()
        {
            _promptMargin?.Refresh();
        }

        /// <summary>
        /// Keeps the prompt in step with the font and foreground it borrows from the control.
        /// </summary>
        /// <param name="e">The property that changed.</param>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (_promptMargin == null)
            {
                return;
            }

            if (e.Property == FontFamilyProperty || e.Property == FontSizeProperty ||
                e.Property == FontWeightProperty || e.Property == FontStyleProperty ||
                e.Property == FontStretchProperty || e.Property == ForegroundProperty ||
                e.Property == FlowDirectionProperty)
            {
                _promptMargin.Refresh();
            }
        }

        #endregion

        /// <summary>
        /// Creates the automation peer for this control.
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new CommandBoxAutomationPeer(this);
        }
    }
}
