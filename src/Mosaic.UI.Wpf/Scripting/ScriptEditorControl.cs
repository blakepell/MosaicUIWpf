/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Mosaic.UI.Wpf.Controls;

namespace Mosaic.UI.Wpf.Scripting;

/// <summary>
/// Provides a themed, executable JavaScript editor for a document or dock tool window.
/// </summary>
[TemplatePart(Name = "PART_Editor", Type = typeof(SyntaxEditor))]
[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Executed))]
public class ScriptEditorControl : Control
{
    private SyntaxEditor? _editor;
    private ScriptEditorSupport? _support;
    private CancellationTokenSource? _execution;
    private bool _synchronizing;
    private bool _isUnloaded;
    private string _savedText = string.Empty;
    private readonly AsyncRelayCommand _runCommand;
    private readonly RelayCommand _stopCommand;

    static ScriptEditorControl() => DefaultStyleKeyProperty.OverrideMetadata(typeof(ScriptEditorControl), new FrameworkPropertyMetadata(typeof(ScriptEditorControl)));

    /// <summary>
    /// Initializes a new instance of the ScriptEditorControl class.
    /// </summary>
    public ScriptEditorControl()
    {
        _runCommand = new AsyncRelayCommand(RunFromCommandAsync, () => !IsRunning && Environment != null);
        _stopCommand = new RelayCommand(Stop, () => IsRunning);
        SaveCommand = new AsyncRelayCommand(SaveFromCommandAsync);
        CompletionCommand = new RelayCommand(() => _support?.ShowCompletion());
        SnippetsCommand = new RelayCommand(() => _support?.ShowCompletion(true));
        SetCurrentValue(EnvironmentProperty, new ScriptEnvironment());
        Loaded += (_, _) => { _isUnloaded = false; AttachSupport(); };
        Unloaded += (_, _) => { _isUnloaded = true; DetachSupport(); Stop(); };
        InputBindings.Add(new KeyBinding(RunCommand, Key.F5, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(StopCommand, Key.F6, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(StopCommand, Key.F5, ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(SaveCommand, Key.S, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(CompletionCommand, Key.Space, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(CompletionCommand, Key.OemPeriod, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(SnippetsCommand, Key.F1, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(new RelayCommand(() =>
        {
            if (_editor is { IsReadOnly: false }) _editor.SelectedText = Guid.NewGuid().ToString();
        }), Key.G, ModifierKeys.Control));
    }

    /// <summary>
    /// Identifies the Environment dependency property.
    /// </summary>
    public static readonly DependencyProperty EnvironmentProperty = DependencyProperty.Register(nameof(Environment), typeof(ScriptEnvironment), typeof(ScriptEditorControl),
        new FrameworkPropertyMetadata(null, (d, _) => ((ScriptEditorControl)d).AttachSupport()));

    /// <summary>
    /// Gets or sets the engine and registration context; null disables execution and completion.
    /// </summary>
    public ScriptEnvironment? Environment { get => (ScriptEnvironment?)GetValue(EnvironmentProperty); set => SetValue(EnvironmentProperty, value); }

    /// <summary>
    /// Identifies the Text dependency property.
    /// </summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(ScriptEditorControl),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((ScriptEditorControl)d).SynchronizeText(), (_, value) => value ?? string.Empty));

    /// <summary>
    /// Gets or sets script text without replacing the host's DataContext.
    /// </summary>
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>
    /// Identifies the FilePath dependency property.
    /// </summary>
    public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register(nameof(FilePath), typeof(string), typeof(ScriptEditorControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>
    /// Gets or sets the save destination; saving without one displays a file picker.
    /// </summary>
    public string? FilePath { get => (string?)GetValue(FilePathProperty); set => SetValue(FilePathProperty, value); }

    /// <summary>
    /// Identifies the IsReadOnly dependency property.
    /// </summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(ScriptEditorControl), new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether editing is disabled; execution remains available.
    /// </summary>
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>
    /// Identifies the ToolBarVisibility dependency property.
    /// </summary>
    public static readonly DependencyProperty ToolBarVisibilityProperty = DependencyProperty.Register(nameof(ToolBarVisibility), typeof(Visibility), typeof(ScriptEditorControl), new PropertyMetadata(Visibility.Visible));

    /// <summary>
    /// Gets or sets the visibility of save, run and stop actions.
    /// </summary>
    public Visibility ToolBarVisibility { get => (Visibility)GetValue(ToolBarVisibilityProperty); set => SetValue(ToolBarVisibilityProperty, value); }

    private static readonly DependencyPropertyKey IsRunningPropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsRunning), typeof(bool), typeof(ScriptEditorControl), new PropertyMetadata(false));
    /// <summary>
    /// Identifies the read-only IsRunning dependency property.
    /// </summary>
    public static readonly DependencyProperty IsRunningProperty = IsRunningPropertyKey.DependencyProperty;
    /// <summary>
    /// Gets whether execution is queued or running.
    /// </summary>
    public bool IsRunning => (bool)GetValue(IsRunningProperty);

    private static readonly DependencyPropertyKey IsModifiedPropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsModified), typeof(bool), typeof(ScriptEditorControl), new PropertyMetadata(false));
    /// <summary>
    /// Identifies the read-only IsModified dependency property.
    /// </summary>
    public static readonly DependencyProperty IsModifiedProperty = IsModifiedPropertyKey.DependencyProperty;
    /// <summary>
    /// Gets whether text differs from the last loaded or saved version.
    /// </summary>
    public bool IsModified => (bool)GetValue(IsModifiedProperty);

    private static readonly DependencyPropertyKey LastErrorPropertyKey = DependencyProperty.RegisterReadOnly(nameof(LastError), typeof(Exception), typeof(ScriptEditorControl), new PropertyMetadata(null));
    /// <summary>
    /// Identifies the read-only LastError dependency property.
    /// </summary>
    public static readonly DependencyProperty LastErrorProperty = LastErrorPropertyKey.DependencyProperty;
    /// <summary>
    /// Gets the most recent execution or save error.
    /// </summary>
    public Exception? LastError => (Exception?)GetValue(LastErrorProperty);

    /// <summary>
    /// Gets the inner Mosaic editor after its template is applied.
    /// </summary>
    public SyntaxEditor? Editor => _editor;
    /// <summary>
    /// Gets the run command (F5).
    /// </summary>
    public ICommand RunCommand => _runCommand;
    /// <summary>
    /// Gets the cancellation command (F6 or Shift+F5).
    /// </summary>
    public ICommand StopCommand => _stopCommand;
    /// <summary>
    /// Gets the save command (Ctrl+S).
    /// </summary>
    public ICommand SaveCommand { get; }
    /// <summary>
    /// Gets the completion command (Ctrl+Space or Ctrl+period).
    /// </summary>
    public ICommand CompletionCommand { get; }
    /// <summary>
    /// Gets the snippet command (F1).
    /// </summary>
    public ICommand SnippetsCommand { get; }

    /// <summary>
    /// Gets or sets an optional application save callback, used instead of file saving.
    /// </summary>
    public Func<string, CancellationToken, Task>? SaveTextAsync { get; set; }

    /// <summary>
    /// Identifies the Executing routed event.
    /// </summary>
    public static readonly RoutedEvent ExecutingEvent = EventManager.RegisterRoutedEvent(nameof(Executing), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ScriptEditorControl));
    /// <summary>
    /// Occurs before execution, on the UI thread.
    /// </summary>
    public event RoutedEventHandler Executing { add => AddHandler(ExecutingEvent, value); remove => RemoveHandler(ExecutingEvent, value); }
    /// <summary>
    /// Identifies the Executed routed event.
    /// </summary>
    public static readonly RoutedEvent ExecutedEvent = EventManager.RegisterRoutedEvent(nameof(Executed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ScriptEditorControl));
    /// <summary>
    /// Occurs after success, cancellation or failure, once the running state has been reset.
    /// </summary>
    public event RoutedEventHandler Executed { add => AddHandler(ExecutedEvent, value); remove => RemoveHandler(ExecutedEvent, value); }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        DetachSupport();
        if (_editor != null) _editor.TextChanged -= OnEditorTextChanged;
        base.OnApplyTemplate();
        _editor = GetTemplateChild("PART_Editor") as SyntaxEditor;
        if (_editor != null)
        {
            _editor.Text = Text;
            _editor.TextChanged += OnEditorTextChanged;
            AttachSupport();
        }
    }

    /// <summary>
    /// Runs a snapshot of the current text and propagates errors to the caller.
    /// </summary>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Dispatcher.VerifyAccess();
        if (IsRunning) return;
        var environment = Environment ?? throw new InvalidOperationException("A scripting environment is required.");
        string code = Text;
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _execution = source;
        SetValue(LastErrorPropertyKey, null);
        SetRunning(true);
        try
        {
            RaiseEvent(new RoutedEventArgs(ExecutingEvent, this));
            await environment.ExecuteAsync(code, source.Token);
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested) { }
        catch (Exception ex) { SetValue(LastErrorPropertyKey, ex); throw; }
        finally
        {
            _execution = null;
            SetRunning(false);
            RaiseEvent(new RoutedEventArgs(ExecutedEvent, this));
        }
    }

    /// <summary>
    /// Requests cooperative cancellation; custom .NET calls must return before execution can stop.
    /// </summary>
    public void Stop() { Dispatcher.VerifyAccess(); _execution?.Cancel(); }

    /// <summary>
    /// Loads a file and marks its contents as unmodified.
    /// </summary>
    /// <param name="path">The source file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        Dispatcher.VerifyAccess();
        string text = await File.ReadAllTextAsync(path, cancellationToken);
        SetCurrentValue(FilePathProperty, path);
        SetCurrentValue(TextProperty, text);
        MarkSaved();
    }

    /// <summary>
    /// Saves a text snapshot through the callback or file path, prompting for a path when needed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        Dispatcher.VerifyAccess();
        string text = Text;
        if (SaveTextAsync != null) await SaveTextAsync(text, cancellationToken);
        else
        {
            string? path = FilePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                var dialog = new SaveFileDialog { Filter = "JavaScript (*.js)|*.js|All files (*.*)|*.*", DefaultExt = ".js" };
                if (dialog.ShowDialog() != true) return;
                path = dialog.FileName;
            }
            await File.WriteAllTextAsync(path, text, cancellationToken);
            SetCurrentValue(FilePathProperty, path);
        }
        _savedText = text;
        SetValue(IsModifiedPropertyKey, Text != _savedText);
    }

    /// <summary>
    /// Marks the current bound text as the saved baseline.
    /// </summary>
    public void MarkSaved() { _savedText = Text; SetValue(IsModifiedPropertyKey, false); }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new ScriptEditorAutomationPeer(this);

    private void AttachSupport()
    {
        DetachSupport();
        _runCommand?.NotifyCanExecuteChanged();
        if (!_isUnloaded && _editor != null && Environment != null) _support = new ScriptEditorSupport(_editor, Environment);
    }
    private void DetachSupport() { _support?.Dispose(); _support = null; }
    private void SynchronizeText()
    {
        if (!_synchronizing && _editor != null && _editor.Text != Text) _editor.Text = Text;
        SetValue(IsModifiedPropertyKey, Text != _savedText);
    }
    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        _synchronizing = true;
        try { SetCurrentValue(TextProperty, _editor?.Text ?? string.Empty); }
        finally { _synchronizing = false; }
    }
    private void SetRunning(bool running)
    {
        SetValue(IsRunningPropertyKey, running);
        _runCommand.NotifyCanExecuteChanged();
        _stopCommand.NotifyCanExecuteChanged();
    }
    private async Task RunFromCommandAsync()
    {
        try { await RunAsync(); }
        catch (Exception ex) { SetValue(LastErrorPropertyKey, ex); }
    }
    private async Task SaveFromCommandAsync()
    {
        try { SetValue(LastErrorPropertyKey, null); await SaveAsync(); }
        catch (Exception ex) { SetValue(LastErrorPropertyKey, ex); }
    }

    private sealed class ScriptEditorAutomationPeer(ScriptEditorControl owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(ScriptEditorControl);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetNameCore() => string.IsNullOrEmpty(base.GetNameCore()) ? "Script editor" : base.GetNameCore();
    }
}
