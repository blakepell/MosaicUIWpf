/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class TagBoxTests
    {
        /// <summary>
        /// Runs the test body on an STA thread, which WPF controls require.
        /// </summary>
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
        /// Applies the control's shipped style and realizes its template, which also proves the XAML parses.
        /// </summary>
        private static TagBox Realize(TagBox control)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/TagBox/TagBox.xaml")
            };

            control.Style = (Style)dictionary[typeof(TagBox)];

            // These tests assert synchronously, so opt out of the typing debounce unless a test opts back in.
            control.SuggestionDelay = TimeSpan.Zero;

            control.Measure(new Size(400, 400));
            control.Arrange(new Rect(0, 0, 400, 400));
            control.ApplyTemplate();
            control.UpdateLayout();

            return control;
        }

        /// <summary>
        /// Gets the template's input box, which is what the control listens to for typed text.
        /// </summary>
        private static TextBox InputBox(TagBox control)
        {
            return (TextBox)control.Template.FindName("PART_TextBox", control);
        }

        [Fact]
        public void Adds_And_Removes_Tags_Through_The_Bound_Collection()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox());

                Assert.True(tagBox.AddTag("  wpf  "));
                Assert.Equal(new[] { "wpf" }, tagBox.Tags);

                // Duplicates are rejected case-insensitively unless AllowDuplicates is set.
                Assert.False(tagBox.AddTag("WPF"));
                tagBox.AllowDuplicates = true;
                Assert.True(tagBox.AddTag("WPF"));

                Assert.True(tagBox.RemoveTag("wpf"));
                Assert.Equal(new[] { "WPF" }, tagBox.Tags);
            });
        }

        [Fact]
        public void TagChanging_Can_Veto_An_Add()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox());
                tagBox.TagChanging += (_, e) => e.Cancel = e.Tag == "blocked";

                Assert.False(tagBox.AddTag("blocked"));
                Assert.Empty(tagBox.Tags);
            });
        }

        [Fact]
        public void No_Suggestions_Are_Produced_Without_A_SuggestionsSource()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox());
                InputBox(tagBox).Text = "w";

                Assert.Empty(tagBox.FilteredSuggestions);
                Assert.False(tagBox.IsSuggestionListOpen);
            });
        }

        [Fact]
        public void Typing_Filters_The_SuggestionsSource_With_Contains_Matching()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox
                {
                    SuggestionsSource = new ObservableCollection<string> { "WPF", "WinForms", "Showcase", "MAUI" }
                });

                InputBox(tagBox).Text = "w";

                // "WPF" and "WinForms" start with it; "Showcase" merely contains it.
                Assert.Equal(new[] { "WPF", "WinForms", "Showcase" }, tagBox.FilteredSuggestions);
            });
        }

        [Fact]
        public void StartsWith_Matching_Narrows_The_Suggestions()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox
                {
                    SuggestionFilterMode = AutoCompleteBoxFilterMode.StartsWith,
                    SuggestionsSource = new ObservableCollection<string> { "WPF", "WinForms", "Showcase", "MAUI" }
                });

                InputBox(tagBox).Text = "w";

                Assert.Equal(new[] { "WPF", "WinForms" }, tagBox.FilteredSuggestions);
            });
        }

        [Fact]
        public void Suggestions_Honor_MinimumPrefixLength_And_MaxSuggestionCount()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox
                {
                    MinimumPrefixLength = 2,
                    MaxSuggestionCount = 1,
                    SuggestionsSource = new ObservableCollection<string> { "WPF", "WinForms" }
                });

                var textBox = InputBox(tagBox);

                textBox.Text = "w";
                Assert.Empty(tagBox.FilteredSuggestions);

                textBox.Text = "wi";
                Assert.Equal(new[] { "WinForms" }, tagBox.FilteredSuggestions);

                textBox.Text = "w";
                Assert.Empty(tagBox.FilteredSuggestions);
            });
        }

        [Fact]
        public void Already_Applied_Tags_Drop_Out_Of_The_Suggestions()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox
                {
                    SuggestionsSource = new ObservableCollection<string> { "WPF", "WinForms" }
                });

                var textBox = InputBox(tagBox);
                textBox.Text = "w";
                Assert.Equal(2, tagBox.FilteredSuggestions.Count);

                tagBox.AddTag("WPF");
                Assert.Equal(new[] { "WinForms" }, tagBox.FilteredSuggestions);

                // Opting out puts it back.
                tagBox.ExcludeExistingTagsFromSuggestions = false;
                Assert.Equal(new[] { "WPF", "WinForms" }, tagBox.FilteredSuggestions);
            });
        }

        [Fact]
        public void Changing_The_Suggestion_Collection_Refreshes_The_List()
        {
            RunSta(() =>
            {
                var suggestions = new ObservableCollection<string> { "WPF" };
                var tagBox = Realize(new TagBox { SuggestionsSource = suggestions });

                InputBox(tagBox).Text = "w";
                Assert.Equal(new[] { "WPF" }, tagBox.FilteredSuggestions);

                suggestions.Add("WinUI");
                Assert.Equal(new[] { "WPF", "WinUI" }, tagBox.FilteredSuggestions);
            });
        }

        /// <summary>
        /// Runs the dispatcher for the supplied duration so that <see cref="DispatcherTimer"/> ticks can fire on the
        /// test's manually created STA thread.
        /// </summary>
        private static void Pump(TimeSpan duration)
        {
            var frame = new DispatcherFrame();

            var timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
            {
                Interval = duration
            };

            timer.Tick += (_, _) =>
            {
                timer.Stop();
                frame.Continue = false;
            };

            timer.Start();
            Dispatcher.PushFrame(frame);
        }

        [Fact]
        public void Typing_Debounces_The_Suggestion_Lookup()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox
                {
                    SuggestionsSource = new ObservableCollection<string> { "WinForms", "WinUI" }
                });

                tagBox.SuggestionDelay = TimeSpan.FromMilliseconds(50);

                var textBox = InputBox(tagBox);
                textBox.Text = "w";

                // Nothing runs on the keystroke itself.
                Assert.Empty(tagBox.FilteredSuggestions);

                // A second keystroke inside the window restarts the wait, then one lookup runs for the final text.
                textBox.Text = "winu";
                Assert.Empty(tagBox.FilteredSuggestions);

                Pump(TimeSpan.FromMilliseconds(250));

                Assert.Equal(new[] { "WinUI" }, tagBox.FilteredSuggestions);
            });
        }

        [Fact]
        public void A_Zero_Delay_Filters_Synchronously()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox
                {
                    SuggestionsSource = new ObservableCollection<string> { "WinForms", "WinUI" }
                });

                Assert.Equal(TimeSpan.Zero, tagBox.SuggestionDelay);
                InputBox(tagBox).Text = "winu";

                Assert.Equal(new[] { "WinUI" }, tagBox.FilteredSuggestions);
            });
        }

        [Fact]
        public void A_Negative_Delay_Is_Coerced_To_Zero()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox());
                tagBox.SuggestionDelay = TimeSpan.FromMilliseconds(-5);

                Assert.Equal(TimeSpan.Zero, tagBox.SuggestionDelay);
            });
        }

        [Fact]
        public void Committing_Before_The_Debounce_Fires_Uses_The_Current_Text()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox
                {
                    SuggestionsSource = new ObservableCollection<string> { "WinForms", "WinUI" }
                });

                // Type slowly enough to get a drop-down showing both entries, with "WinForms" selected.
                var textBox = InputBox(tagBox);
                textBox.Text = "win";
                tagBox.OpenSuggestionList();
                Assert.Equal(new[] { "WinForms", "WinUI" }, tagBox.FilteredSuggestions);

                // Now type past it with a debounce long enough that the lookup is still pending on the next keystroke.
                tagBox.SuggestionDelay = TimeSpan.FromMilliseconds(5000);
                textBox.Text = "winu";

                PressKey(tagBox, Key.Tab);

                // Tab flushed the pending lookup rather than committing "WinForms" off the stale, still-open list.
                Assert.DoesNotContain("WinForms", tagBox.Tags);
                Assert.Equal(new[] { "winu" }, tagBox.Tags);
            });
        }

        /// <summary>
        /// Sends a key straight to the template's input box, which is where the control's handler is attached.
        /// </summary>
        private static KeyEventArgs PressKey(TagBox control, Key key)
        {
            var textBox = InputBox(control);
            var keyboardDevice = InputManager.Current.PrimaryKeyboardDevice;

            var args = new KeyEventArgs(keyboardDevice, new HwndSource(0, 0, 0, 0, 0, "t", IntPtr.Zero), 0, key)
            {
                RoutedEvent = Keyboard.PreviewKeyDownEvent
            };

            textBox.RaiseEvent(args);

            return args;
        }

        [Fact]
        public void Tab_Commits_The_Selected_Suggestion_As_A_Tag()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox
                {
                    SuggestionsSource = new ObservableCollection<string> { "WinForms", "WinUI" }
                });

                var textBox = InputBox(tagBox);
                textBox.Text = "win";

                // Typing only auto-opens the drop-down for a focused control, which a headless test never is.
                tagBox.OpenSuggestionList();
                Assert.True(tagBox.IsSuggestionListOpen);

                var args = PressKey(tagBox, Key.Tab);

                // The suggestion becomes the tag rather than the partial text, and focus stays put.
                Assert.True(args.Handled);
                Assert.Equal(new[] { "WinForms" }, tagBox.Tags);
                Assert.Equal(string.Empty, textBox.Text);
                Assert.False(tagBox.IsSuggestionListOpen);
            });
        }

        [Fact]
        public void Tab_Commits_Free_Form_Text_When_There_Is_No_Suggestion()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox());
                var textBox = InputBox(tagBox);
                textBox.Text = "handmade";

                var args = PressKey(tagBox, Key.Tab);

                Assert.True(args.Handled);
                Assert.Equal(new[] { "handmade" }, tagBox.Tags);
                Assert.Equal(string.Empty, textBox.Text);
            });
        }

        [Fact]
        public void Tab_Moves_Focus_On_When_Nothing_Is_Pending()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox
                {
                    SuggestionsSource = new ObservableCollection<string> { "WinForms" }
                });

                tagBox.AddTag("WPF");

                // Nothing typed, so the control leaves Tab alone and normal focus navigation happens.
                var args = PressKey(tagBox, Key.Tab);

                Assert.False(args.Handled);
                Assert.Equal(new[] { "WPF" }, tagBox.Tags);
            });
        }

        [Fact]
        public void Escape_Discards_The_Pending_Text_But_Keeps_Existing_Tags()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox
                {
                    SuggestionsSource = new ObservableCollection<string> { "WinForms", "WinUI" }
                });

                tagBox.AddTag("WPF");

                var textBox = InputBox(tagBox);
                textBox.Text = "win";

                // Typing only auto-opens the drop-down for a focused control, which a headless test never is.
                tagBox.OpenSuggestionList();
                Assert.True(tagBox.IsSuggestionListOpen);

                var args = PressKey(tagBox, Key.Escape);

                Assert.True(args.Handled);
                Assert.Equal(string.Empty, textBox.Text);
                Assert.False(tagBox.IsSuggestionListOpen);
                Assert.Equal(new[] { "WPF" }, tagBox.Tags);
            });
        }

        [Fact]
        public void Escape_Is_Left_Unhandled_When_There_Is_Nothing_To_Discard()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox());
                tagBox.AddTag("WPF");

                // Nothing pending, so Esc keeps bubbling (a hosting dialog can still close on it).
                var args = PressKey(tagBox, Key.Escape);

                Assert.False(args.Handled);
                Assert.Equal(new[] { "WPF" }, tagBox.Tags);
            });
        }

        [Fact]
        public void A_Custom_Predicate_Replaces_The_Built_In_Matching()
        {
            RunSta(() =>
            {
                var tagBox = Realize(new TagBox
                {
                    SuggestionFilterMode = AutoCompleteBoxFilterMode.Custom,
                    SuggestionFilterPredicate = (item, searchText) => ((string)item).Length == searchText.Length,
                    SuggestionsSource = new ObservableCollection<string> { "WPF", "WinForms" }
                });

                InputBox(tagBox).Text = "abc";

                Assert.Equal(new[] { "WPF" }, tagBox.FilteredSuggestions);
            });
        }
    }
}
