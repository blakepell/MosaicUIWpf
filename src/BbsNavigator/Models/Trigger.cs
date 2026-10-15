/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BbsNavigator.Models;

/// <summary>
/// Runs a script when a complete received line matches a cached regular expression.
/// </summary>
/// <remarks>
/// Editable values are owned by the UI dispatcher. Each instance retains only its current
/// compiled expression, including a failed compilation, rather than growing a global cache.
/// Matches and received lines are not retained by the model.
/// </remarks>
public partial class Trigger : ObservableObject, ICloneable
{
    private Regex? _regex;
    private string? _cachedPattern;
    private string? _patternError;

    /// <summary>
    /// Gets or sets the persistent identifier for this trigger.
    /// </summary>
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the descriptive name of this trigger.
    /// </summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the case-sensitive regular expression matched against received text without ANSI codes.
    /// </summary>
    [ObservableProperty]
    public partial string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the script, using %0 for the entire line and %1, %2, or %name for capture groups.
    /// </summary>
    [ObservableProperty]
    public partial string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processing priority; lower numbers run first.
    /// </summary>
    [ObservableProperty]
    public partial int Priority { get; set; } = 10000;

    /// <summary>
    /// Gets or sets a value that indicates whether this trigger can run.
    /// </summary>
    [ObservableProperty]
    public partial bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value that indicates whether a match disables this trigger before starting its script.
    /// </summary>
    [ObservableProperty]
    public partial bool DisableAfterTriggered { get; set; }

    /// <summary>
    /// Gets or sets the number of scripts started by this trigger.
    /// </summary>
    [ObservableProperty]
    public partial long Count { get; set; }

    /// <summary>
    /// Gets the first line of the script for display in lists.
    /// </summary>
    [JsonIgnore]
    public string CommandPreview => Command.TrimStart().Split('\n', 2)[0].TrimEnd('\r');

    /// <summary>
    /// Notifies list bindings when the script changes.
    /// </summary>
    /// <param name="value">The updated script.</param>
    partial void OnCommandChanged(string value) => OnPropertyChanged(nameof(CommandPreview));

    /// <summary>
    /// Gets the cached expression, compiling it only after the pattern changes.
    /// </summary>
    /// <param name="error">When this method returns, contains a validation error or null.</param>
    /// <returns>The compiled expression, or null for an empty or invalid pattern.</returns>
    public Regex? GetRegex(out string? error)
    {
        if (!string.Equals(_cachedPattern, Pattern, StringComparison.Ordinal))
        {
            _cachedPattern = Pattern;
            _regex = null;
            _patternError = null;
            try
            {
                if (string.IsNullOrWhiteSpace(Pattern))
                    _patternError = "Enter a regular expression.";
                else
                    _regex = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
            }
            catch (ArgumentException ex)
            {
                _patternError = ex.Message;
            }
        }

        error = _patternError;
        return _regex;
    }

    /// <summary>
    /// Creates an editable copy with the same identifier and cached expression.
    /// </summary>
    /// <returns>An independent observable model sharing only immutable regex state.</returns>
    public object Clone()
    {
        var copy = new Trigger { Id = Id, Count = Count };
        copy.CopyFrom(this);
        copy._regex = _regex;
        copy._cachedPattern = _cachedPattern;
        copy._patternError = _patternError;
        return copy;
    }

    /// <summary>
    /// Copies editable values without replacing this trigger's identity or execution count.
    /// </summary>
    /// <param name="source">The trigger to copy.</param>
    public void CopyFrom(Trigger source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Name = source.Name;
        Pattern = source.Pattern;
        Command = source.Command;
        Priority = source.Priority;
        Enabled = source.Enabled;
        DisableAfterTriggered = source.DisableAfterTriggered;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name} {Pattern} {Command}";
}
