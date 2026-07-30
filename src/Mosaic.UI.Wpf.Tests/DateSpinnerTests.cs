/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Covers the <see cref="DateSpinner"/> control itself: coercion, the temporary versus committed selection,
    /// the commit modes, and binding behavior.
    /// </summary>
    public class DateSpinnerTests
    {
        /// <summary>
        /// Registers the <c>pack</c> URI scheme. Without an <see cref="Application"/> the scheme is only registered
        /// as a side effect of whichever WPF type happens to be touched first, which makes loading a component
        /// resource dictionary depend on test ordering.
        /// </summary>
        static DateSpinnerTests()
        {
            _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
        }

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
        private static DateSpinner Realize(DateSpinner control)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/DateSpinner/DateSpinner.xaml")
            };

            control.Style = (Style)dictionary[typeof(DateSpinner)];
            control.Measure(new Size(400, 400));
            control.Arrange(new Rect(0, 0, 400, 400));
            control.ApplyTemplate();
            control.UpdateLayout();

            return control;
        }

        /// <summary>
        /// Realizes the control inside a real window, which is what a popup needs in order to open.
        /// </summary>
        private static (DateSpinner Spinner, Window Window) Host(DateSpinner control)
        {
            var window = new Window
            {
                Width = 400,
                Height = 400,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
                ShowActivated = false,
                Content = control
            };

            window.Show();
            control.ApplyTemplate();
            window.UpdateLayout();

            return (control, window);
        }

        private static DateSpinnerSelector? SelectorPart(DateSpinner spinner, string part)
        {
            return spinner.Template?.FindName(part, spinner) as DateSpinnerSelector;
        }

        private static void Press(DateSpinner spinner, Key key)
        {
            spinner.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(spinner), 0, key)
            {
                RoutedEvent = Keyboard.KeyDownEvent
            });
        }

        #region Defaults And Nullability

        [Fact]
        public void Defaults_To_No_Selection_And_Shows_Placeholders()
        {
            RunSta(() =>
            {
                var spinner = Realize(new DateSpinner { Culture = new CultureInfo("en-US") });

                Assert.Null(spinner.SelectedDate);
                Assert.False(spinner.HasSelectedDate);
                Assert.False(spinner.IsDropDownOpen);
                Assert.Equal(DateSpinnerCommitMode.Explicit, spinner.CommitMode);

                // The placeholders come from the theme dictionary, so they resolve without being set per instance.
                Assert.Equal("Month", spinner.MonthText);
                Assert.Equal("Day", spinner.DayText);
                Assert.Equal("Year", spinner.YearText);
            });
        }

        [Fact]
        public void Explicit_Placeholder_Text_Wins_Over_The_Theme_Resource()
        {
            RunSta(() =>
            {
                var spinner = Realize(new DateSpinner { MonthPlaceholderText = "mm", YearPlaceholderText = "yyyy" });

                Assert.Equal("mm", spinner.MonthText);
                Assert.Equal("Day", spinner.DayText);
                Assert.Equal("yyyy", spinner.YearText);
            });
        }

        [Fact]
        public void Setting_A_Date_Normalizes_The_Time_Of_Day()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner { SelectedDate = new DateTime(2025, 3, 15, 17, 42, 9) };

                Assert.Equal(new DateTime(2025, 3, 15), spinner.SelectedDate);
                Assert.True(spinner.HasSelectedDate);
            });
        }

        [Fact]
        public void Clear_Sets_The_Selection_Back_To_Null()
        {
            RunSta(() =>
            {
                var spinner = Realize(new DateSpinner { SelectedDate = new DateTime(2025, 3, 15) });

                spinner.Clear();

                Assert.Null(spinner.SelectedDate);
                Assert.False(spinner.HasSelectedDate);
                Assert.Equal("Month", spinner.MonthText);
            });
        }

        [Fact]
        public void A_Read_Only_Spinner_Cannot_Be_Opened_Or_Cleared()
        {
            RunSta(() =>
            {
                var spinner = Realize(new DateSpinner
                {
                    SelectedDate = new DateTime(2025, 3, 15),
                    IsReadOnly = true
                });

                spinner.Open();
                Assert.False(spinner.IsDropDownOpen);

                spinner.Clear();
                Assert.Equal(new DateTime(2025, 3, 15), spinner.SelectedDate);

                // The value stays visible and the control stays focusable.
                Assert.Equal("March", spinner.MonthText);
                Assert.True(spinner.Focusable);
            });
        }

        #endregion

        #region Range Coercion

        [Fact]
        public void A_Date_Before_The_Minimum_Is_Coerced_Up()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner
                {
                    MinimumDate = new DateTime(2025, 1, 1),
                    MaximumDate = new DateTime(2025, 12, 31),
                    SelectedDate = new DateTime(2020, 6, 1)
                };

                Assert.Equal(new DateTime(2025, 1, 1), spinner.SelectedDate);
            });
        }

        [Fact]
        public void A_Date_After_The_Maximum_Is_Coerced_Down()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner
                {
                    MinimumDate = new DateTime(2025, 1, 1),
                    MaximumDate = new DateTime(2025, 12, 31),
                    SelectedDate = new DateTime(2030, 6, 1)
                };

                Assert.Equal(new DateTime(2025, 12, 31), spinner.SelectedDate);
            });
        }

        [Fact]
        public void A_Maximum_Below_The_Minimum_Is_Lifted_To_It()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner
                {
                    MinimumDate = new DateTime(2025, 1, 1),
                    MaximumDate = new DateTime(2020, 1, 1)
                };

                Assert.Equal(new DateTime(2025, 1, 1), spinner.MaximumDate);
                Assert.True(spinner.MinimumDate <= spinner.MaximumDate);
            });
        }

        [Fact]
        public void Raising_The_Minimum_After_A_Selection_Recoerces_The_Value()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner { SelectedDate = new DateTime(2025, 6, 1) };

                spinner.MinimumDate = new DateTime(2026, 1, 1);

                Assert.Equal(new DateTime(2026, 1, 1), spinner.SelectedDate);
            });
        }

        [Fact]
        public void Lowering_The_Maximum_After_A_Selection_Recoerces_The_Value()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner { SelectedDate = new DateTime(2025, 6, 1) };

                spinner.MaximumDate = new DateTime(2024, 5, 4);

                Assert.Equal(new DateTime(2024, 5, 4), spinner.SelectedDate);
            });
        }

        [Fact]
        public void The_Year_Wheel_Only_Offers_Years_Inside_The_Range()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner
                {
                    MinimumDate = new DateTime(2020, 3, 1),
                    MaximumDate = new DateTime(2025, 8, 1)
                };

                var years = spinner.YearItems;

                Assert.NotNull(years);
                Assert.Equal(6, years!.Count);
                Assert.Equal(2020, years[0].Value);
                Assert.Equal(2025, years[^1].Value);
                Assert.All(years, item => Assert.True(item.IsSelectable));
            });
        }

        #endregion

        #region Boundary Filtering

        [Fact]
        public void Months_Before_The_Minimum_Are_Not_Selectable_In_The_Boundary_Year()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner
                {
                    MinimumDate = new DateTime(2025, 3, 15),
                    MaximumDate = new DateTime(2030, 12, 31),
                    SelectedDate = new DateTime(2025, 6, 1)
                };

                var months = spinner.MonthItems;

                Assert.NotNull(months);
                Assert.Equal(12, months!.Count);
                Assert.False(months[0].IsSelectable);
                Assert.False(months[1].IsSelectable);
                Assert.True(months[2].IsSelectable);
                Assert.True(months[11].IsSelectable);
            });
        }

        [Fact]
        public void Days_Before_The_Minimum_Are_Not_Selectable_In_The_Boundary_Month()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner
                {
                    MinimumDate = new DateTime(2025, 3, 15),
                    MaximumDate = new DateTime(2030, 12, 31),
                    SelectedDate = new DateTime(2025, 3, 20)
                };

                var days = spinner.DayItems;

                Assert.NotNull(days);
                Assert.Equal(31, days!.Count);

                // Days 1 through 14 fall before March 15.
                Assert.All(days.Take(14), item => Assert.False(item.IsSelectable));
                Assert.All(days.Skip(14), item => Assert.True(item.IsSelectable));
            });
        }

        [Fact]
        public void Days_After_The_Maximum_Are_Not_Selectable_In_The_Boundary_Month()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner
                {
                    MinimumDate = new DateTime(2020, 1, 1),
                    MaximumDate = new DateTime(2025, 9, 10),
                    SelectedDate = new DateTime(2025, 9, 5)
                };

                var days = spinner.DayItems;

                Assert.NotNull(days);
                Assert.Equal(30, days!.Count);
                Assert.All(days.Take(10), item => Assert.True(item.IsSelectable));
                Assert.All(days.Skip(10), item => Assert.False(item.IsSelectable));
            });
        }

        [Fact]
        public void The_Day_Wheel_Resizes_To_The_Length_Of_The_Selected_Month()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner { SelectedDate = new DateTime(2025, 1, 15) };
                Assert.Equal(31, spinner.DayItems!.Count);

                spinner.SelectedDate = new DateTime(2025, 2, 15);
                Assert.Equal(28, spinner.DayItems!.Count);

                spinner.SelectedDate = new DateTime(2024, 2, 15);
                Assert.Equal(29, spinner.DayItems!.Count);

                spinner.SelectedDate = new DateTime(2025, 4, 15);
                Assert.Equal(30, spinner.DayItems!.Count);
            });
        }

        #endregion

        #region Events

        [Fact]
        public void Changing_The_Date_Raises_The_Routed_Event_With_Both_Values()
        {
            RunSta(() =>
            {
                var spinner = Realize(new DateSpinner { SelectedDate = new DateTime(2025, 1, 1) });

                DateSpinnerDateChangedEventArgs? args = null;
                spinner.SelectedDateChanged += (_, e) => args = e;

                spinner.SelectedDate = new DateTime(2025, 2, 2);

                Assert.NotNull(args);
                Assert.Equal(new DateTime(2025, 1, 1), args!.OldDate);
                Assert.Equal(new DateTime(2025, 2, 2), args.NewDate);
            });
        }

        [Fact]
        public void Assigning_The_Same_Effective_Day_Raises_Nothing()
        {
            RunSta(() =>
            {
                var spinner = Realize(new DateSpinner { SelectedDate = new DateTime(2025, 3, 15) });

                int count = 0;
                spinner.SelectedDateChanged += (_, _) => count++;

                // Same day, different time of day. Normalization makes these the same value.
                spinner.SelectedDate = new DateTime(2025, 3, 15, 9, 30, 0);
                spinner.SelectedDate = new DateTime(2025, 3, 15);

                Assert.Equal(0, count);

                // A value that coerces onto the day already selected is also not a change.
                spinner.MaximumDate = new DateTime(2025, 3, 15);
                spinner.SelectedDate = new DateTime(2099, 1, 1);

                Assert.Equal(0, count);
                Assert.Equal(new DateTime(2025, 3, 15), spinner.SelectedDate);
            });
        }

        [Fact]
        public void Clearing_Raises_A_Change_With_A_Null_New_Date()
        {
            RunSta(() =>
            {
                var spinner = Realize(new DateSpinner { SelectedDate = new DateTime(2025, 3, 15) });

                DateSpinnerDateChangedEventArgs? args = null;
                spinner.SelectedDateChanged += (_, e) => args = e;

                spinner.Clear();

                Assert.NotNull(args);
                Assert.Equal(new DateTime(2025, 3, 15), args!.OldDate);
                Assert.Null(args.NewDate);
            });
        }

        [Fact]
        public void Opening_And_Closing_Raise_The_Drop_Down_Events()
        {
            RunSta(() =>
            {
                var (spinner, window) = Host(new DateSpinner());

                int opened = 0;
                int closed = 0;
                spinner.DropDownOpened += (_, _) => opened++;
                spinner.DropDownClosed += (_, _) => closed++;

                spinner.Open();
                Assert.True(spinner.IsDropDownOpen);
                Assert.Equal(1, opened);

                spinner.Close();
                Assert.False(spinner.IsDropDownOpen);
                Assert.Equal(1, closed);

                window.Close();
            });
        }

        #endregion

        #region Temporary Versus Committed Selection

        [Fact]
        public void Opening_And_Closing_Without_Touching_A_Wheel_Assigns_Nothing()
        {
            RunSta(() =>
            {
                var (spinner, window) = Host(new DateSpinner());

                Assert.Null(spinner.SelectedDate);

                spinner.Open();
                Assert.Null(spinner.SelectedDate);

                spinner.Close();
                Assert.Null(spinner.SelectedDate);

                window.Close();
            });
        }

        [Fact]
        public void Explicit_Apply_Commits_The_Temporary_Date()
        {
            RunSta(() =>
            {
                var (spinner, window) = Host(new DateSpinner
                {
                    CommitMode = DateSpinnerCommitMode.Explicit,
                    MinimumDate = new DateTime(2020, 1, 1),
                    MaximumDate = new DateTime(2030, 12, 31)
                });

                spinner.Open();

                // With nothing selected the temporary date starts at today.
                spinner.Apply();

                Assert.Equal(DateTime.Today, spinner.SelectedDate);
                Assert.False(spinner.IsDropDownOpen);

                window.Close();
            });
        }

        [Fact]
        public void Explicit_Cancel_Restores_The_Date_From_Before_The_Drop_Down_Opened()
        {
            RunSta(() =>
            {
                var (spinner, window) = Host(new DateSpinner
                {
                    CommitMode = DateSpinnerCommitMode.Explicit,
                    SelectedDate = new DateTime(2025, 3, 15)
                });

                spinner.Open();

                var month = SelectorPart(spinner, "PART_MonthSelector");
                Assert.NotNull(month);
                month!.SelectedNumber = 7;

                // The temporary date moved but nothing was committed.
                Assert.Equal(new DateTime(2025, 3, 15), spinner.SelectedDate);

                spinner.Cancel();

                Assert.Equal(new DateTime(2025, 3, 15), spinner.SelectedDate);
                Assert.False(spinner.IsDropDownOpen);

                window.Close();
            });
        }

        [Fact]
        public void Explicit_Apply_Commits_A_Wheel_Change()
        {
            RunSta(() =>
            {
                var (spinner, window) = Host(new DateSpinner
                {
                    CommitMode = DateSpinnerCommitMode.Explicit,
                    SelectedDate = new DateTime(2025, 3, 15)
                });

                spinner.Open();

                var month = SelectorPart(spinner, "PART_MonthSelector");
                Assert.NotNull(month);
                month!.SelectedNumber = 7;

                spinner.Apply();

                Assert.Equal(new DateTime(2025, 7, 15), spinner.SelectedDate);

                window.Close();
            });
        }

        [Fact]
        public void Immediate_Mode_Writes_Every_Wheel_Change_Straight_Through()
        {
            RunSta(() =>
            {
                var (spinner, window) = Host(new DateSpinner
                {
                    CommitMode = DateSpinnerCommitMode.Immediate,
                    SelectedDate = new DateTime(2025, 3, 15)
                });

                spinner.Open();

                var month = SelectorPart(spinner, "PART_MonthSelector");
                Assert.NotNull(month);
                month!.SelectedNumber = 7;

                Assert.Equal(new DateTime(2025, 7, 15), spinner.SelectedDate);

                spinner.Close();

                // Closing preserves the latest value in immediate mode.
                Assert.Equal(new DateTime(2025, 7, 15), spinner.SelectedDate);

                window.Close();
            });
        }

        [Fact]
        public void A_Wheel_Change_Clamps_The_Day_When_Moving_To_A_Shorter_Month()
        {
            RunSta(() =>
            {
                var (spinner, window) = Host(new DateSpinner
                {
                    CommitMode = DateSpinnerCommitMode.Immediate,
                    SelectedDate = new DateTime(2025, 1, 31)
                });

                spinner.Open();

                var month = SelectorPart(spinner, "PART_MonthSelector");
                Assert.NotNull(month);

                month!.SelectedNumber = 2;
                Assert.Equal(new DateTime(2025, 2, 28), spinner.SelectedDate);

                // The same move in a leap year keeps one more day.
                var year = SelectorPart(spinner, "PART_YearSelector");
                Assert.NotNull(year);

                spinner.SelectedDate = new DateTime(2024, 1, 31);
                month.SelectedNumber = 2;
                Assert.Equal(new DateTime(2024, 2, 29), spinner.SelectedDate);

                window.Close();
            });
        }

        [Fact]
        public void Escape_Cancels_And_Closes()
        {
            RunSta(() =>
            {
                var (spinner, window) = Host(new DateSpinner
                {
                    CommitMode = DateSpinnerCommitMode.Explicit,
                    SelectedDate = new DateTime(2025, 3, 15)
                });

                spinner.Open();

                var month = SelectorPart(spinner, "PART_MonthSelector");
                month!.SelectedNumber = 9;

                Press(spinner, Key.Escape);

                Assert.False(spinner.IsDropDownOpen);
                Assert.Equal(new DateTime(2025, 3, 15), spinner.SelectedDate);

                window.Close();
            });
        }

        [Fact]
        public void Enter_Opens_The_Closed_Spinner()
        {
            RunSta(() =>
            {
                var (spinner, window) = Host(new DateSpinner());

                Press(spinner, Key.Enter);

                Assert.True(spinner.IsDropDownOpen);

                window.Close();
            });
        }

        #endregion

        #region Fields And Culture

        [Fact]
        public void The_Last_Visible_Field_Cannot_Be_Hidden()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner();

                spinner.IsMonthVisible = false;
                spinner.IsDayVisible = false;
                spinner.IsYearVisible = false;

                Assert.False(spinner.IsMonthVisible);
                Assert.False(spinner.IsDayVisible);
                Assert.True(spinner.IsYearVisible);
            });
        }

        [Fact]
        public void Hiding_A_Field_Preserves_That_Component_Of_The_Date()
        {
            RunSta(() =>
            {
                var spinner = Realize(new DateSpinner
                {
                    SelectedDate = new DateTime(2025, 3, 15),
                    IsDayVisible = false
                });

                Assert.Equal(new DateTime(2025, 3, 15), spinner.SelectedDate);
                Assert.Equal(15, spinner.SelectedDate!.Value.Day);
            });
        }

        [Fact]
        public void Field_Order_Follows_The_Culture()
        {
            RunSta(() =>
            {
                var spinner = Realize(new DateSpinner { Culture = new CultureInfo("en-US") });

                Assert.Equal(0, spinner.MonthFieldIndex);
                Assert.Equal(1, spinner.DayFieldIndex);
                Assert.Equal(2, spinner.YearFieldIndex);

                spinner.Culture = new CultureInfo("en-GB");

                Assert.Equal(0, spinner.DayFieldIndex);
                Assert.Equal(1, spinner.MonthFieldIndex);
                Assert.Equal(2, spinner.YearFieldIndex);
            });
        }

        [Fact]
        public void Only_The_First_Visible_Field_Omits_Its_Divider()
        {
            RunSta(() =>
            {
                var spinner = Realize(new DateSpinner { Culture = new CultureInfo("en-US") });

                Assert.Equal(Visibility.Collapsed, spinner.MonthSeparatorVisibility);
                Assert.Equal(Visibility.Visible, spinner.DaySeparatorVisibility);
                Assert.Equal(Visibility.Visible, spinner.YearSeparatorVisibility);

                // With the month hidden the day becomes the first visible field.
                spinner.IsMonthVisible = false;

                Assert.Equal(Visibility.Collapsed, spinner.DaySeparatorVisibility);
                Assert.Equal(Visibility.Visible, spinner.YearSeparatorVisibility);
            });
        }

        [Fact]
        public void Month_Names_Are_Culture_Aware()
        {
            RunSta(() =>
            {
                var spinner = Realize(new DateSpinner
                {
                    Culture = new CultureInfo("fr-FR"),
                    SelectedDate = new DateTime(2025, 1, 15)
                });

                Assert.Equal("janvier", spinner.MonthText);

                spinner.Culture = new CultureInfo("de-DE");
                Assert.Equal("Januar", spinner.MonthText);
            });
        }

        [Fact]
        public void Changing_The_Display_Mode_Refreshes_The_Displayed_Values()
        {
            RunSta(() =>
            {
                var spinner = Realize(new DateSpinner
                {
                    Culture = new CultureInfo("en-US"),
                    SelectedDate = new DateTime(2025, 3, 15)
                });

                Assert.Equal("March", spinner.MonthText);

                spinner.MonthDisplayMode = DateSpinnerMonthDisplayMode.AbbreviatedName;
                Assert.Equal("Mar", spinner.MonthText);

                spinner.MonthFormat = "MM";
                Assert.Equal("03", spinner.MonthText);

                spinner.YearFormat = "yy";
                Assert.Equal("25", spinner.YearText);
            });
        }

        #endregion

        #region Binding

        private sealed class Reservation : INotifyPropertyChanged
        {
            private DateTime? _arrival;

            public DateTime? Arrival
            {
                get => _arrival;
                set
                {
                    if (_arrival == value)
                    {
                        return;
                    }

                    _arrival = value;
                    this.OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string? name = null)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        [Fact]
        public void Two_Way_Binding_Flows_In_Both_Directions()
        {
            RunSta(() =>
            {
                var model = new Reservation { Arrival = new DateTime(2025, 3, 15) };
                var spinner = Realize(new DateSpinner { DataContext = model });

                // SelectedDate binds two way by default, so no Mode is needed.
                spinner.SetBinding(DateSpinner.SelectedDateProperty, new Binding(nameof(Reservation.Arrival)));

                Assert.Equal(new DateTime(2025, 3, 15), spinner.SelectedDate);

                // View model to control.
                model.Arrival = new DateTime(2026, 7, 4);
                Assert.Equal(new DateTime(2026, 7, 4), spinner.SelectedDate);

                // Control to view model.
                spinner.SelectedDate = new DateTime(2027, 1, 2);
                Assert.Equal(new DateTime(2027, 1, 2), model.Arrival);

                // Clearing propagates the null.
                spinner.Clear();
                Assert.Null(model.Arrival);
            });
        }

        [Fact]
        public void Rebinding_And_Rapid_Cycling_Leaves_The_Control_Consistent()
        {
            RunSta(() =>
            {
                var (spinner, window) = Host(new DateSpinner { IsClearButtonVisible = true });

                for (int i = 0; i < 25; i++)
                {
                    spinner.Open();
                    spinner.SelectedDate = new DateTime(2000 + i, 1 + i % 12, 1 + i % 28);
                    spinner.Close();
                    spinner.Clear();
                }

                Assert.Null(spinner.SelectedDate);
                Assert.False(spinner.IsDropDownOpen);

                window.Close();
            });
        }

        #endregion

        #region Wheel Behavior

        [Fact]
        public void A_Wheel_Skips_Values_Outside_The_Range_When_Moving()
        {
            RunSta(() =>
            {
                var (spinner, window) = Host(new DateSpinner
                {
                    MinimumDate = new DateTime(2025, 3, 15),
                    MaximumDate = new DateTime(2025, 12, 31),
                    SelectedDate = new DateTime(2025, 3, 20)
                });

                spinner.Open();

                var day = SelectorPart(spinner, "PART_DaySelector");
                Assert.NotNull(day);

                // Home lands on the first day the range permits, not on the first day of the month.
                Assert.True(day!.MoveToEnd(false));
                Assert.Equal(15, day.SelectedNumber);

                Assert.True(day.MoveToEnd(true));
                Assert.Equal(31, day.SelectedNumber);

                window.Close();
            });
        }

        [Fact]
        public void A_Wheel_Pads_Its_Values_So_The_Ends_Can_Reach_The_Centre()
        {
            RunSta(() =>
            {
                var selector = new DateSpinnerSelector
                {
                    VisibleItemCount = 5,
                    Values = new List<DateSpinnerItem>
                    {
                        new(1, "one", true),
                        new(2, "two", true),
                        new(3, "three", true)
                    }
                };

                // Two spacers at each end for a five item viewport.
                Assert.Equal(7, selector.Items.Count);
                Assert.True(((DateSpinnerItem)selector.Items[0]).IsSpacer);
                Assert.True(((DateSpinnerItem)selector.Items[6]).IsSpacer);
                Assert.Equal(2, ((DateSpinnerItem)selector.Items[3]).Value);
            });
        }

        [Fact]
        public void The_Wheel_Style_Realizes_Its_Template()
        {
            RunSta(() =>
            {
                // No style is assigned here on purpose. The wheel has to pick up its default style from the
                // assembly's Generic.xaml, which is what proves DateSpinnerSelector.xaml is registered and parses.
                var selector = new DateSpinnerSelector
                {
                    Values = new List<DateSpinnerItem>
                    {
                        new(1, "one", true),
                        new(2, "two", true),
                        new(3, "three", true)
                    }
                };

                var window = new Window
                {
                    Width = 300,
                    Height = 300,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                    ShowActivated = false,
                    Content = selector
                };

                window.Show();
                selector.ApplyTemplate();
                window.UpdateLayout();

                // The scroll viewer the wheel drives has to exist, and it has to be the inertia one.
                var viewer = selector.Template.FindName("PART_ScrollViewer", selector);

                Assert.IsType<InertiaScrollViewer>(viewer);
                Assert.Equal(selector.ItemHeight * selector.VisibleItemCount, selector.ViewportHeight);

                // Selecting the middle value centres it, which for real index 1 is exactly one item height.
                selector.SelectedNumber = 2;
                window.UpdateLayout();

                Assert.Equal(selector.ItemHeight, ((InertiaScrollViewer)viewer!).VerticalOffset, 1);

                window.Close();
            });
        }

        [Fact]
        public void A_Wheel_Coerces_Its_Viewport_To_An_Odd_Number_Of_Rows()
        {
            RunSta(() =>
            {
                var selector = new DateSpinnerSelector { VisibleItemCount = 4 };
                Assert.Equal(5, selector.VisibleItemCount);

                selector.VisibleItemCount = 1;
                Assert.Equal(3, selector.VisibleItemCount);

                selector.ItemHeight = 40;
                Assert.Equal(120, selector.ViewportHeight);
            });
        }

        #endregion

        [Fact]
        public void Property_Assignments_Before_The_Template_Is_Applied_Do_Not_Throw()
        {
            RunSta(() =>
            {
                var spinner = new DateSpinner
                {
                    SelectedDate = new DateTime(2025, 3, 15),
                    MinimumDate = new DateTime(2000, 1, 1),
                    MaximumDate = new DateTime(2050, 12, 31),
                    Culture = new CultureInfo("de-DE"),
                    MonthDisplayMode = DateSpinnerMonthDisplayMode.Numeric,
                    IsDayVisible = false,
                    IsClearButtonVisible = true
                };

                spinner.Open();
                spinner.Apply();
                spinner.Clear();

                Assert.Null(spinner.SelectedDate);
            });
        }
    }
}
