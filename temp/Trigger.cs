using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Design;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using Argus.Extensions;
using Argus.Memory;
using Argus.Network;
using BbsNavigator.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Text;
using Mosaic.UI.Wpf.Logging;

namespace BbsNavigator.Models
{
    /// <summary>
    /// A trigger is an action that is executed based off of a pattern that is sent from the game.
    /// </summary>
    [Display(Name = "Edit Trigger")]
    public partial class Trigger : ObservableObject
    {
        /// <summary>
        /// A descriptive name for the trigger.
        /// </summary>
        [property:Browsable(true)]
        [property:Category("Misc")]
        [property: Display(Name = "Name")]
        [ObservableProperty]
        public partial string Name { get; set; } = "";

        private string _pattern = "";

        [Browsable(true)]
        [Category("Misc")]
        [Display(Name = "Pattern")]
        public string Pattern
        {
            get => _pattern;
            set
            {
                try
                {
                    // Only set the pattern if it compiled.
                    Regex = new(value, RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));
                    _pattern = value;
                    OnPropertyChanged();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }

        /// <summary>
        /// The command to execute or the script to run.
        /// </summary>
        [property: Display(Name = "Command")]
        [NotifyCanExecuteChangedFor("OkCommand")]
        [ObservableProperty]
        public partial string Command { get; set; } = "";

        /// <summary>
        /// Affects the order in which triggers are processed.  Higher priority triggers
        /// go first (the higher the number the lower the priority).
        /// </summary>
        [property: Display(Name = "Priority")]
        [ObservableProperty]
        public partial int Priority { get; set; } = 10000;

        /// <summary>
        /// Compiled reused Regex for matching the trigger.
        /// </summary>
        [property: Category("Internal")]
        [property: Description("Internal RegEx Object")]
        [property: ReadOnly(true)]
        [property: Browsable(false)]
        [JsonIgnore]
        public Regex Regex { get; set; }

        /// <summary>
        /// If the trigger is currently enabled/active.
        /// </summary>
        [property: Display(Name = "Enabled")]
        [property: Category("Options")]
        [property: Description("If the trigger is currently enabled.")]
        [property: ReadOnly(false)]
        [property: Browsable(true)]
        [ObservableProperty]
        public partial bool Enabled { get; set; } = true;

        /// <summary>
        /// The command after it's been processed.  This is what should get sent to the game.
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public string ProcessedCommand { get; private set; } = "";

        /// <summary>
        /// The text that triggered the trigger.
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public string TriggeringText { get; private set; } = "";

        [JsonIgnore]
        [Browsable(false)]
        public Match Match { get; set; }

        /// <summary>
        /// The number of times this trigger has fired.
        /// </summary>
        [property: Display(Name = "Count")]
        [property: Category("Internal")]
        [property: Description("The number of times the trigger has matched and executed.")]
        [property: ReadOnly(false)]
        [property: Browsable(false)]
        [ObservableProperty]
        public partial int Count { get; set; } = 0;


        /// <summary>
        /// The unique identifier for this alias.
        /// </summary>
        [property: Category("Internal")]
        [property: Description("The unique identifier for the object.")]
        [property: ReadOnly(true)]
        [property: Browsable(false)]
        [ObservableProperty]
        public partial string Id { get; set; }

        /// <summary>
        /// If the trigger is currently enabled/active.
        /// </summary>
        [property: Display(Name = "Max Execution Time")]
        [property: Category("Performance")]
        [property: Description("The longest observed execution time in milliseconds.")]
        [property: ReadOnly(false)]
        [property: Browsable(true)]
        [ObservableProperty]
        public partial double MaxExecutionTime { get; set; }

        /// <summary>
        /// Thread-safe cache for compiled regex patterns.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

        /// <summary>
        /// Constructor
        /// </summary>
        public Trigger()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Trigger()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Clones the trigger.
        /// </summary>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Overrideable Execute method.
        /// </summary>
        public virtual void Execute()
        {

        }

        /// <summary>
        /// If the model is considered valid.
        /// </summary>
        public override bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Pattern)
                   && !string.IsNullOrWhiteSpace(Command);
        }

        /// <summary>
        /// Override of ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            using (var sb = ZString.CreateStringBuilder())
            {
                sb.Append(Name);
                sb.Append(' ');
                sb.Append(Command);
                sb.Append(' ');
                sb.Append(Pattern);
                sb.Append(' ');

                return sb.ToString();
            }
        }


        /// <summary>
        /// Gets or creates a cached regex for a pattern.
        /// </summary>
        /// <param name="pattern"></param>
        private Regex GetOrCreateCachedRegex(string pattern)
        {
            return RegexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(250)));
        }

        /// <summary>
        /// If the trigger matches the provided line.
        /// </summary>
        /// <param name="line"></param>
        public virtual bool IsMatch(string line)
        {
            Match? match;

            long t0 = Stopwatch.GetTimestamp(); 

            // Does this trigger contain any variables?  If so, we'll need to special handle it.  We're also
            // going to require that the VariableReplacement value is set to true so the player has to
            // specifically opt into this.  Since the Gag triggers run -a lot- on the terminal rendering
            // the bool will much faster as a first check before the string contains check.  This is
            // a micro optimization that had real payoff in the performance profiler.  Also when profiling, IndexOf
            // a char without Ordinal consistently ran faster than Contains and IndexOf with Ordinal.
            if (VariableReplacement && Pattern.IndexOf('@') >= 0)
            {
                // Replace any variables with their literal values.
                string tempPattern = Interpreter.ReplaceVariablesWithValue(Pattern);
                match = GetOrCreateCachedRegex(tempPattern).Match(line);
            }
            else
            {
                // Run the match normal Match, this will be most all cases.
                match = Regex?.Match(line);
            }

            long t1 = Stopwatch.GetTimestamp();
            double ms = (t1 - t0) * 1000.0 / Stopwatch.Frequency;

            if (ms > this.MaxExecutionTime)
            {
                this.MaxExecutionTime = ms;
            }

            // If it's not a match, get out.
            if (match == null || !match.Success)
            {
                return false;
            }

            // If it's supposed to auto disable itself after it fires then set that.
            if (DisableAfterTriggered)
            {
                Enabled = false;
            }

            // Save the match for CLR processing if needed.
            Match = match;

            Logger.Log(LogSeverity.Debug, $"Trigger: {Name} {Id}", referenceId: Id);

            if (LineTransformer && ExecuteAs == ExecuteType.JavaScript)
            {
                var paramList = new string[match.Groups.Count];
                paramList[0] = line;

                for (int i = 1; i < match.Groups.Count; i++)
                {
                    paramList[i] = match.Groups[i].Value;
                }

                // We'll send the function we want to call but also the code, if the code has changed
                // it nothing will be reloaded thus saving memory and calls.  This is why replacing %1
                // variables is problematic here and why we are forcing the use of Lua varargs (...)
                try
                {
                    string? newLine = Interpreter?.JsEngine?.ExecuteExpression(Utilities.Utilities.ReplaceArgs(Command, paramList)) as string;

                    if (!string.IsNullOrWhiteSpace(newLine))
                    {
                        ProcessedCommand = newLine;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogSeverity.Error, $"LineTransformer Trigger JavaScript Error: {ex.Message}", referenceId: Id);
                    Interpreter?.MudTerminal.EchoLog($"Trigger error: {Pattern}", LogType.Error);
                    Interpreter?.MudTerminal.EchoLog(ex.Message, LogType.Error);
                    return false;
                }
            }
            else if (LineTransformer && ExecuteAs == ExecuteType.Command)
            {
                // If this is the route then it will be a 1:1 replacement, no script needs to be run.
                ProcessedCommand = Command;
            }
            else
            {
                // This is the block that swaps matched groups into the processed command as the user
                // has requested (e.g. %0, %1, %2, %3, etc.)

                // Save the text that triggered this trigger so that it can be used if needed elsewhere like in
                // a CLR trigger.
                TriggeringText = line;

                using (var sb = ZString.CreateStringBuilder())
                {
                    // Set the command that we may or may not process.  Allow the user to have the content of
                    // the last trigger if they need it.
                    sb.Append(Command?.Replace("%0", TriggeringText) ?? "");

                    // Go through any groups backwards that came back in the trigger match.  Groups are matched in reverse
                    // order so that %1 doesn't overwrite %12 and leave a trailing 2.
                    for (int i = match.Groups.Count - 1; i >= 0; i--)
                    {
                        Logger.LogDebug($"Matched Group: %{i} = {match.Groups[i].Value}");

                        // If it's a named match, we specifically named it in the trigger and thus we're going
                        // to automatically store it in a variable that can then be used later by aliases, triggers, etc.
                        // If there are variables that came back that aren't named, throw those into the more generic
                        // %1, %2, %3 values.
                        if (!string.IsNullOrWhiteSpace(match.Groups[i].Name) && !match.Groups[i].Name.IsNumeric() && !string.IsNullOrWhiteSpace(match.Groups[i].Value))
                        {
                            if (this.Interpreter == null)
                            {
                                var vm = AppServices.GetRequiredService<AppViewModel>();
                                this.Interpreter = vm.ActiveInterpreter;
                            }

                            this?.Interpreter?.SetVariable(match.Groups[i].Name, match.Groups[i].Value);
                            sb.Replace($"%{i.ToString()}", match.Groups[i].Value);
                        }
                        else
                        {
                            // Replace %1, %2, etc. variables with their values from the pattern match.  ToString() was
                            // called to avoid a boxing allocation.
                            sb.Replace($"%{i.ToString()}", match.Groups[i].Value);
                        }
                    }

                    ProcessedCommand = sb.ToString();
                }
            }

            LastMatched = DateTime.Now;

            return match.Success;
        }
    }
}
