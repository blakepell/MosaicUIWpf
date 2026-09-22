/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls;
using System.Runtime.ExceptionServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Control level tests for <see cref="CommandBox"/>. These run on a manually created STA thread,
    /// matching the pattern used by the other WPF control tests in this project.
    /// </summary>
    public class CommandBoxTests
    {
        /// <summary>
        /// Runs an action on a dedicated STA thread and rethrows any failure on the calling thread.
        /// </summary>
        /// <param name="action">The action to run.</param>
        private static void RunSta(Action action)
        {
            Exception? failure = null;

            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        /// <summary>
        /// Raises a tunneling PreviewKeyDown on the box, which is where the command box does its key
        /// handling.
        /// </summary>
        /// <param name="box">The box to send the key to.</param>
        /// <param name="key">The key to send.</param>
        /// <returns>Whether the key was handled.</returns>
        private static bool PressKey(CommandBox box, Key key)
        {
            var args = new KeyEventArgs(
                Keyboard.PrimaryDevice,
                new HwndSource(0, 0, 0, 0, 0, "t", IntPtr.Zero),
                0,
                key)
            {
                RoutedEvent = Keyboard.PreviewKeyDownEvent
            };

            box.RaiseEvent(args);
            return args.Handled;
        }

        [Fact]
        public void Enter_Raises_CommandExecuted_With_Normalized_Text()
        {
            RunSta(() =>
            {
                var box = new CommandBox { Text = "   look north   " };
                string? captured = null;

                box.CommandExecuted += (_, e) => captured = e.Command;

                Assert.True(PressKey(box, Key.Enter));
                Assert.Equal("look north", captured);
            });
        }

        [Fact]
        public void Enter_On_An_Empty_Box_Raises_Nothing()
        {
            RunSta(() =>
            {
                var box = new CommandBox();
                bool raised = false;

                box.CommandExecuted += (_, _) => raised = true;

                // The key is still swallowed, a single line box never inserts a newline.
                Assert.True(PressKey(box, Key.Enter));
                Assert.False(raised);
            });
        }

        [Fact]
        public void AllowEmptyCommands_Dispatches_An_Empty_Command()
        {
            RunSta(() =>
            {
                var box = new CommandBox { AllowEmptyCommands = true };
                int count = 0;

                box.CommandExecuted += (_, _) => count++;

                PressKey(box, Key.Enter);

                Assert.Equal(1, count);
            });
        }

        [Fact]
        public void TrimCommand_Disabled_Keeps_Surrounding_Whitespace()
        {
            RunSta(() =>
            {
                var box = new CommandBox { TrimCommand = false, Text = " say hi " };
                string? captured = null;

                box.CommandExecuted += (_, e) => captured = e.Command;

                PressKey(box, Key.Enter);

                Assert.Equal(" say hi ", captured);
            });
        }

        [Fact]
        public void CommitBehavior_Clear_Empties_The_Box()
        {
            RunSta(() =>
            {
                var box = new CommandBox { CommitBehavior = CommandBoxCommitBehavior.Clear, Text = "north" };

                PressKey(box, Key.Enter);

                Assert.Equal(string.Empty, box.Text);
            });
        }

        [Fact]
        public void CommitBehavior_Keep_Leaves_The_Text_Unselected()
        {
            RunSta(() =>
            {
                var box = new CommandBox { CommitBehavior = CommandBoxCommitBehavior.Keep, Text = "north" };
                box.Select(5, 0);

                PressKey(box, Key.Enter);

                Assert.Equal("north", box.Text);
                Assert.Equal(0, box.SelectionLength);
            });
        }

        [Fact]
        public void CommitBehavior_SelectAll_Keeps_And_Selects_The_Text()
        {
            RunSta(() =>
            {
                var box = new CommandBox { CommitBehavior = CommandBoxCommitBehavior.SelectAll, Text = "north" };

                PressKey(box, Key.Enter);

                Assert.Equal("north", box.Text);
                Assert.Equal(box.Text.Length, box.SelectionLength);
            });
        }

        [Fact]
        public void Executed_Commands_Are_Recorded_In_The_History()
        {
            RunSta(() =>
            {
                var box = new CommandBox { Text = "north" };

                PressKey(box, Key.Enter);
                box.Text = "east";
                PressKey(box, Key.Enter);

                Assert.Equal(["north", "east"], box.History.Items);
            });
        }

        [Fact]
        public void A_Handler_Can_Keep_A_Command_Out_Of_The_History()
        {
            RunSta(() =>
            {
                var box = new CommandBox { Text = "hunter2" };

                box.CommandExecuted += (_, e) => e.AddToHistory = false;

                PressKey(box, Key.Enter);

                Assert.Empty(box.History.Items);
            });
        }

        [Fact]
        public void IsHistoryEnabled_False_Records_Nothing()
        {
            RunSta(() =>
            {
                var box = new CommandBox { IsHistoryEnabled = false, Text = "north" };

                PressKey(box, Key.Enter);

                Assert.Empty(box.History.Items);
            });
        }

        [Fact]
        public void PreviewCommandExecuted_Can_Rewrite_The_Command()
        {
            RunSta(() =>
            {
                var box = new CommandBox { Text = "n" };
                string? captured = null;

                box.PreviewCommandExecuted += (_, e) => e.Command = "north";
                box.CommandExecuted += (_, e) => captured = e.Command;

                PressKey(box, Key.Enter);

                Assert.Equal("north", captured);
                Assert.Equal(["north"], box.History.Items);
            });
        }

        [Fact]
        public void PreviewCommandExecuted_Can_Veto_The_Command()
        {
            RunSta(() =>
            {
                var box = new CommandBox { CommitBehavior = CommandBoxCommitBehavior.Clear, Text = "north" };
                bool raised = false;

                box.PreviewCommandExecuted += (_, e) => e.Handled = true;
                box.CommandExecuted += (_, _) => raised = true;

                PressKey(box, Key.Enter);

                Assert.False(raised);
                Assert.Equal("north", box.Text);
                Assert.Empty(box.History.Items);
            });
        }

        [Fact]
        public void Command_Is_Executed_With_The_Command_Text_By_Default()
        {
            RunSta(() =>
            {
                object? parameter = null;
                var box = new CommandBox
                {
                    Text = "north",
                    Command = new DelegateCommand(p => parameter = p)
                };

                PressKey(box, Key.Enter);

                Assert.Equal("north", parameter);
            });
        }

        [Fact]
        public void CommandParameter_Overrides_The_Command_Text()
        {
            RunSta(() =>
            {
                object? parameter = null;
                var box = new CommandBox
                {
                    Text = "north",
                    CommandParameter = 42,
                    Command = new DelegateCommand(p => parameter = p)
                };

                PressKey(box, Key.Enter);

                Assert.Equal(42, parameter);
            });
        }

        [Fact]
        public void Up_And_Down_Walk_The_History_And_Restore_The_Draft()
        {
            RunSta(() =>
            {
                var box = new CommandBox { CommitBehavior = CommandBoxCommitBehavior.Clear };

                box.Text = "north";
                PressKey(box, Key.Enter);
                box.Text = "east";
                PressKey(box, Key.Enter);

                box.Text = "half typed";

                Assert.True(PressKey(box, Key.Up));
                Assert.Equal("east", box.Text);

                PressKey(box, Key.Up);
                Assert.Equal("north", box.Text);

                // Already on the oldest entry, so the box is left alone.
                PressKey(box, Key.Up);
                Assert.Equal("north", box.Text);

                PressKey(box, Key.Down);
                Assert.Equal("east", box.Text);

                PressKey(box, Key.Down);
                Assert.Equal("half typed", box.Text);
            });
        }

        [Fact]
        public void History_Recall_Parks_The_Caret_At_The_End()
        {
            RunSta(() =>
            {
                var box = new CommandBox { CommitBehavior = CommandBoxCommitBehavior.Clear, Text = "north" };

                PressKey(box, Key.Enter);
                PressKey(box, Key.Up);

                Assert.Equal("north", box.Text);
                Assert.Equal(0, box.SelectionLength);
                Assert.Equal(box.Text.Length, box.CaretOffset);
            });
        }

        [Fact]
        public void IsHistoryEnabled_False_Leaves_The_Arrow_Keys_Alone()
        {
            RunSta(() =>
            {
                var box = new CommandBox { IsHistoryEnabled = false, Text = "north" };

                Assert.False(PressKey(box, Key.Up));
                Assert.Equal("north", box.Text);
            });
        }

        [Fact]
        public void Escape_Clears_The_Box_And_Raises_EscapePressed()
        {
            RunSta(() =>
            {
                var box = new CommandBox { Text = "north" };
                int escapes = 0;

                box.EscapePressed += (_, _) => escapes++;

                Assert.True(PressKey(box, Key.Escape));
                Assert.Equal(string.Empty, box.Text);
                Assert.Equal(1, escapes);

                // Nothing left to clear, so the key keeps bubbling for the host to use.
                Assert.False(PressKey(box, Key.Escape));
                Assert.Equal(2, escapes);
            });
        }

        [Fact]
        public void ClearOnEscape_False_Keeps_The_Text_But_Still_Raises_EscapePressed()
        {
            RunSta(() =>
            {
                var box = new CommandBox { ClearOnEscape = false, Text = "north" };
                int escapes = 0;

                box.EscapePressed += (_, _) => escapes++;

                Assert.False(PressKey(box, Key.Escape));
                Assert.Equal("north", box.Text);
                Assert.Equal(1, escapes);
            });
        }

        [Fact]
        public void Tab_Completes_From_The_Most_Recent_Match()
        {
            RunSta(() =>
            {
                var box = new CommandBox { CommitBehavior = CommandBoxCommitBehavior.Clear };

                box.Text = "north";
                PressKey(box, Key.Enter);
                box.Text = "nod";
                PressKey(box, Key.Enter);

                box.Text = "no";

                Assert.True(PressKey(box, Key.Tab));
                Assert.Equal("nod", box.Text);
            });
        }

        [Fact]
        public void Tab_With_No_Match_Falls_Through_To_Focus_Navigation()
        {
            RunSta(() =>
            {
                var box = new CommandBox { Text = "zz" };

                Assert.False(PressKey(box, Key.Tab));
                Assert.Equal("zz", box.Text);
            });
        }

        [Fact]
        public void Tab_Completion_Can_Be_Turned_Off()
        {
            RunSta(() =>
            {
                var box = new CommandBox { IsTabCompletionEnabled = false, CommitBehavior = CommandBoxCommitBehavior.Clear };

                box.Text = "north";
                PressKey(box, Key.Enter);
                box.Text = "no";

                Assert.False(PressKey(box, Key.Tab));
                Assert.Equal("no", box.Text);
            });
        }

        [Fact]
        public void Multi_Line_Text_Is_Collapsed_To_A_Single_Line()
        {
            RunSta(() =>
            {
                var box = new CommandBox { Text = "a\r\nb" };

                Assert.Equal("a b", box.Text);
                Assert.Equal(1, box.Document.LineCount);
            });
        }

        [Fact]
        public void Bare_Line_Feeds_And_Carriage_Returns_Are_Collapsed_Too()
        {
            RunSta(() =>
            {
                var box = new CommandBox { Text = "a\nb\rc" };

                Assert.Equal("a b c", box.Text);
                Assert.Equal(1, box.Document.LineCount);
            });
        }

        [Fact]
        public void UseBlockCaret_Adds_And_Removes_Exactly_One_Renderer()
        {
            RunSta(() =>
            {
                var box = new CommandBox();
                var renderers = box.TextArea.TextView.BackgroundRenderers;

                // The watermark renderer is always installed, so measure the delta.
                int baseline = renderers.Count;

                Assert.Null(box.TextArea.Caret.CaretBrush);

                box.UseBlockCaret = true;
                Assert.Equal(baseline + 1, renderers.Count);
                Assert.Equal(Brushes.Transparent, box.TextArea.Caret.CaretBrush);

                // Setting it again must not stack a second renderer.
                box.UseBlockCaret = true;
                Assert.Equal(baseline + 1, renderers.Count);

                box.UseBlockCaret = false;
                Assert.Equal(baseline, renderers.Count);
                Assert.Null(box.TextArea.Caret.CaretBrush);
            });
        }

        [Fact]
        public void ImportHistory_Seeds_The_Arrow_Keys()
        {
            RunSta(() =>
            {
                var box = new CommandBox();

                box.ImportHistory(["one", "two"]);

                PressKey(box, Key.Up);
                Assert.Equal("two", box.Text);

                PressKey(box, Key.Up);
                Assert.Equal("one", box.Text);
            });
        }

        [Fact]
        public void ClearHistory_Empties_The_History()
        {
            RunSta(() =>
            {
                var box = new CommandBox { Text = "north" };

                PressKey(box, Key.Enter);
                box.ClearHistory();

                Assert.Empty(box.History.Items);
            });
        }

        [Fact]
        public void MaxHistoryItems_Flows_Through_To_The_History()
        {
            RunSta(() =>
            {
                var box = new CommandBox
                {
                    MaxHistoryItems = 2,
                    HistoryDuplicatePolicy = HistoryDuplicatePolicy.Allow,
                    CommitBehavior = CommandBoxCommitBehavior.Clear
                };

                foreach (string command in new[] { "one", "two", "three" })
                {
                    box.Text = command;
                    PressKey(box, Key.Enter);
                }

                Assert.Equal(["two", "three"], box.History.Items);
            });
        }

        [Fact]
        public void ExecuteCommand_Can_Be_Driven_From_Code()
        {
            RunSta(() =>
            {
                var box = new CommandBox { Text = "north" };
                string? captured = null;

                box.CommandExecuted += (_, e) => captured = e.Command;

                Assert.True(box.ExecuteCommand());
                Assert.Equal("north", captured);

                box.Text = string.Empty;
                Assert.False(box.ExecuteCommand());
            });
        }

        /// <summary>
        /// The prompt is off by default and installs a left margin when it is turned on.
        /// </summary>
        [Fact]
        public void ShowPrompt_InstallsAndRemovesLeftMargin()
        {
            RunSta(() =>
            {
                var box = new CommandBox();
                int baseline = box.TextArea.LeftMargins.Count;

                Assert.False(box.ShowPrompt);

                box.ShowPrompt = true;
                Assert.Equal(baseline + 1, box.TextArea.LeftMargins.Count);

                // Setting it again must not stack a second margin.
                box.ShowPrompt = true;
                Assert.Equal(baseline + 1, box.TextArea.LeftMargins.Count);

                box.ShowPrompt = false;
                Assert.Equal(baseline, box.TextArea.LeftMargins.Count);
            });
        }

        /// <summary>
        /// The prompt is decoration: it never becomes part of the text or of the dispatched command.
        /// </summary>
        [Fact]
        public void Prompt_IsNotPartOfTheCommand()
        {
            RunSta(() =>
            {
                var box = new CommandBox { ShowPrompt = true, Prompt = ">" };
                string? captured = null;
                box.CommandExecuted += (_, e) => captured = e.Command;

                box.Text = "look";

                Assert.Equal("look", box.Text);
                Assert.True(box.ExecuteCommand());
                Assert.Equal("look", captured);
            });
        }

        /// <summary>
        /// The prompt defaults to a chevron with padding that separates it from both the border and
        /// the text.
        /// </summary>
        [Fact]
        public void Prompt_HasTerminalStyleDefaults()
        {
            RunSta(() =>
            {
                var box = new CommandBox();

                Assert.Equal(">", box.Prompt);
                Assert.Null(box.PromptBrush);
                Assert.True(box.PromptPadding.Left > 0);
                Assert.True(box.PromptPadding.Right > 0);
            });
        }

        /// <summary>
        /// A minimal <see cref="ICommand"/> used to verify the MVVM hook.
        /// </summary>
        private sealed class DelegateCommand : ICommand
        {
            private readonly Action<object?> _execute;

            public DelegateCommand(Action<object?> execute)
            {
                _execute = execute;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter) => _execute(parameter);
        }
    }
}
