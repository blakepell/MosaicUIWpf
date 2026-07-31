/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Markup;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Covers the <see cref="TimeSpinner"/> control itself: coercion onto the offered values, the two value
    /// properties mirroring each other, the commit modes, and wheel generation.
    /// </summary>
    public class TimeSpinnerTests
    {
        /// <summary>
        /// Registers the <c>pack</c> URI scheme. Without an <see cref="Application"/> the scheme is only registered
        /// as a side effect of whichever WPF type happens to be touched first, which makes loading a component
        /// resource dictionary depend on test ordering.
        /// </summary>
        static TimeSpinnerTests()
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
        private static TimeSpinner Realize(TimeSpinner control)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/TimeSpinner/TimeSpinner.xaml")
            };

            control.Style = (Style)dictionary[typeof(TimeSpinner)];
            control.Measure(new Size(400, 400));
            control.Arrange(new Rect(0, 0, 400, 400));
            control.ApplyTemplate();
            control.UpdateLayout();

            return control;
        }

        private static DateSpinnerSelector? SelectorPart(TimeSpinner spinner, string part)
        {
            return spinner.Template?.FindName(part, spinner) as DateSpinnerSelector;
        }

        #region Defaults And Nullability

        [Fact]
        public void Defaults_To_No_Selection_And_Shows_Placeholders()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner { Culture = new CultureInfo("en-US") });

                Assert.Null(spinner.SelectedTime);
                Assert.Null(spinner.SelectedTimeSpan);
                Assert.False(spinner.HasSelectedTime);
                Assert.False(spinner.IsDropDownOpen);
                Assert.Equal(TimeSpinnerCommitMode.Explicit, spinner.CommitMode);

                // The placeholders come from the theme dictionary, so they resolve without being set per instance.
                Assert.Equal("Hour", spinner.HourText);
                Assert.Equal("Minute", spinner.MinuteText);
                Assert.Equal("AM/PM", spinner.MeridiemText);
            });
        }

        [Fact]
        public void Explicit_Placeholder_Text_Wins_Over_The_Theme_Resource()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner { HourPlaceholderText = "hh", MeridiemPlaceholderText = "tt" });

                Assert.Equal("hh", spinner.HourText);
                Assert.Equal("Minute", spinner.MinuteText);
                Assert.Equal("tt", spinner.MeridiemText);
            });
        }

        [Fact]
        public void Setting_A_Time_Discards_Seconds()
        {
            RunSta(() =>
            {
                var spinner = new TimeSpinner { SelectedTime = new TimeOnly(17, 42, 9) };

                Assert.Equal(new TimeOnly(17, 42), spinner.SelectedTime);
                Assert.True(spinner.HasSelectedTime);
            });
        }

        [Fact]
        public void The_Closed_Surface_Renders_A_Twelve_Hour_Time()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner
                {
                    Culture = new CultureInfo("en-US"),
                    SelectedTime = new TimeOnly(13, 5)
                });

                Assert.Equal("1", spinner.HourText);
                Assert.Equal("05", spinner.MinuteText);
                Assert.Equal("PM", spinner.MeridiemText);
            });
        }

        [Fact]
        public void Clear_Sets_The_Selection_Back_To_Null()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner { SelectedTime = new TimeOnly(13, 30) });

                spinner.Clear();

                Assert.Null(spinner.SelectedTime);
                Assert.Null(spinner.SelectedTimeSpan);
                Assert.False(spinner.HasSelectedTime);
                Assert.Equal("Hour", spinner.HourText);
            });
        }

        [Fact]
        public void A_Read_Only_Spinner_Cannot_Be_Opened_Or_Cleared()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner
                {
                    Culture = new CultureInfo("en-US"),
                    SelectedTime = new TimeOnly(13, 30),
                    IsReadOnly = true
                });

                spinner.Open();
                Assert.False(spinner.IsDropDownOpen);

                spinner.Clear();
                Assert.Equal(new TimeOnly(13, 30), spinner.SelectedTime);

                // The value stays visible and the control stays focusable.
                Assert.Equal("1", spinner.HourText);
                Assert.True(spinner.Focusable);
            });
        }

        #endregion

        #region The TimeSpan Mirror

        [Fact]
        public void Selecting_A_Time_Publishes_The_Matching_TimeSpan()
        {
            RunSta(() =>
            {
                var spinner = new TimeSpinner { SelectedTime = new TimeOnly(13, 30) };

                Assert.Equal(new TimeSpan(13, 30, 0), spinner.SelectedTimeSpan);
            });
        }

        [Fact]
        public void Assigning_A_TimeSpan_Publishes_The_Matching_Time()
        {
            RunSta(() =>
            {
                var spinner = new TimeSpinner { SelectedTimeSpan = new TimeSpan(9, 0, 0) };

                Assert.Equal(new TimeOnly(9, 0), spinner.SelectedTime);
            });
        }

        [Fact]
        public void The_TimeSpan_Mirror_Reflects_The_Value_After_Snapping_Rather_Than_The_One_Requested()
        {
            RunSta(() =>
            {
                var spinner = new TimeSpinner
                {
                    MinuteInterval = 15,
                    SelectedTimeSpan = new TimeSpan(9, 7, 0)
                };

                Assert.Equal(new TimeOnly(9, 0), spinner.SelectedTime);
                Assert.Equal(new TimeSpan(9, 0, 0), spinner.SelectedTimeSpan);
            });
        }

        [Fact]
        public void A_TimeSpan_Outside_A_Single_Day_Is_Folded_Into_One()
        {
            RunSta(() =>
            {
                var spinner = new TimeSpinner { SelectedTimeSpan = new TimeSpan(26, 30, 0) };
                Assert.Equal(new TimeOnly(2, 30), spinner.SelectedTime);

                var backwards = new TimeSpinner { SelectedTimeSpan = new TimeSpan(-1, 0, 0) };
                Assert.Equal(new TimeOnly(23, 0), backwards.SelectedTime);
            });
        }

        [Fact]
        public void Clearing_Through_Either_Property_Clears_Both()
        {
            RunSta(() =>
            {
                var spinner = new TimeSpinner { SelectedTime = new TimeOnly(13, 30) };

                spinner.SelectedTimeSpan = null;

                Assert.Null(spinner.SelectedTime);
                Assert.Null(spinner.SelectedTimeSpan);
            });
        }

        #endregion

        #region Range And Interval Coercion

        [Fact]
        public void A_Time_Before_The_Minimum_Is_Coerced_Up()
        {
            RunSta(() =>
            {
                var spinner = new TimeSpinner
                {
                    MinimumTime = new TimeOnly(8, 0),
                    MaximumTime = new TimeOnly(17, 0),
                    SelectedTime = new TimeOnly(6, 0)
                };

                Assert.Equal(new TimeOnly(8, 0), spinner.SelectedTime);
            });
        }

        [Fact]
        public void A_Time_After_The_Maximum_Is_Coerced_Down()
        {
            RunSta(() =>
            {
                var spinner = new TimeSpinner
                {
                    MinimumTime = new TimeOnly(8, 0),
                    MaximumTime = new TimeOnly(17, 0),
                    SelectedTime = new TimeOnly(23, 0)
                };

                Assert.Equal(new TimeOnly(17, 0), spinner.SelectedTime);
            });
        }

        [Fact]
        public void A_Maximum_Below_The_Minimum_Is_Lifted_Rather_Than_Rejected()
        {
            RunSta(() =>
            {
                var spinner = new TimeSpinner { MinimumTime = new TimeOnly(12, 0), MaximumTime = new TimeOnly(8, 0) };

                Assert.Equal(new TimeOnly(12, 0), spinner.MaximumTime);
            });
        }

        [Fact]
        public void Tightening_The_Range_Pulls_An_Existing_Selection_In()
        {
            RunSta(() =>
            {
                var spinner = new TimeSpinner { SelectedTime = new TimeOnly(6, 0) };

                spinner.MinimumTime = new TimeOnly(9, 0);

                Assert.Equal(new TimeOnly(9, 0), spinner.SelectedTime);
            });
        }

        [Fact]
        public void Coarsening_The_Interval_Snaps_An_Existing_Selection()
        {
            RunSta(() =>
            {
                var spinner = new TimeSpinner { SelectedTime = new TimeOnly(9, 7) };

                Assert.Equal(new TimeOnly(9, 7), spinner.SelectedTime);

                spinner.MinuteInterval = 15;

                Assert.Equal(new TimeOnly(9, 0), spinner.SelectedTime);
            });
        }

        [Fact]
        public void An_Unusable_Interval_Is_Coerced()
        {
            RunSta(() =>
            {
                var spinner = new TimeSpinner { MinuteInterval = 0 };

                Assert.Equal(1, spinner.MinuteInterval);
            });
        }

        #endregion

        #region Wheels

        [Fact]
        public void The_Hour_Wheel_Always_Offers_Twelve_Entries_Starting_At_Twelve()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner());

                Assert.NotNull(spinner.HourItems);
                Assert.Equal(12, spinner.HourItems!.Count);
                Assert.Equal(12, spinner.HourItems[0].Value);
                Assert.Equal(1, spinner.HourItems[1].Value);
            });
        }

        [Fact]
        public void The_Minute_Wheel_Follows_The_Interval()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner { MinuteInterval = 15 });

                Assert.Equal(4, spinner.MinuteItems!.Count);
                Assert.Equal(new[] { 0, 15, 30, 45 }, spinner.MinuteItems.Select(i => i.Value));
            });
        }

        [Fact]
        public void Entries_Outside_The_Range_Are_Present_But_Not_Selectable()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner
                {
                    MinimumTime = new TimeOnly(13, 0),
                    MaximumTime = new TimeOnly(17, 0),
                    SelectedTime = new TimeOnly(14, 0)
                });

                // A PM only range keeps AM on the wheel so the wheel's geometry does not change under the user.
                var am = spinner.MeridiemItems!.Single(i => i.Value == (int)TimeSpinnerMeridiem.Am);
                var pm = spinner.MeridiemItems!.Single(i => i.Value == (int)TimeSpinnerMeridiem.Pm);

                Assert.Equal(2, spinner.MeridiemItems!.Count);
                Assert.False(am.IsSelectable);
                Assert.True(pm.IsSelectable);

                // 11 PM is outside the range, 2 PM is inside it.
                Assert.False(spinner.HourItems!.Single(i => i.Value == 11).IsSelectable);
                Assert.True(spinner.HourItems!.Single(i => i.Value == 2).IsSelectable);
            });
        }

        #endregion

        #region Commit Modes

        [Fact]
        public void Explicit_Mode_Does_Not_Commit_Until_Apply()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner
                {
                    CommitMode = TimeSpinnerCommitMode.Explicit,
                    SelectedTime = new TimeOnly(9, 0)
                });

                spinner.Open();

                var hour = SelectorPart(spinner, "PART_HourSelector");
                Assert.NotNull(hour);
                hour!.SelectedNumber = 11;

                // The wheel moved, the committed value did not.
                Assert.Equal(new TimeOnly(9, 0), spinner.SelectedTime);

                spinner.Apply();

                Assert.Equal(new TimeOnly(11, 0), spinner.SelectedTime);
                Assert.False(spinner.IsDropDownOpen);
            });
        }

        [Fact]
        public void Cancel_Restores_The_Time_The_Drop_Down_Opened_With()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner
                {
                    CommitMode = TimeSpinnerCommitMode.Immediate,
                    SelectedTime = new TimeOnly(9, 0)
                });

                spinner.Open();

                var hour = SelectorPart(spinner, "PART_HourSelector");
                hour!.SelectedNumber = 11;

                // Immediate mode has already written the change through.
                Assert.Equal(new TimeOnly(11, 0), spinner.SelectedTime);

                spinner.Cancel();

                Assert.Equal(new TimeOnly(9, 0), spinner.SelectedTime);
            });
        }

        [Fact]
        public void Moving_The_Meridiem_Wheel_Shifts_The_Time_By_Twelve_Hours()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner
                {
                    CommitMode = TimeSpinnerCommitMode.Immediate,
                    SelectedTime = new TimeOnly(9, 30)
                });

                spinner.Open();

                var meridiem = SelectorPart(spinner, "PART_MeridiemSelector");
                meridiem!.SelectedNumber = (int)TimeSpinnerMeridiem.Pm;

                Assert.Equal(new TimeOnly(21, 30), spinner.SelectedTime);
            });
        }

        [Fact]
        public void A_Changed_Event_Reports_Both_Sides_Of_The_Change()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner { SelectedTime = new TimeOnly(9, 0) });

                TimeSpinnerTimeChangedEventArgs? captured = null;
                spinner.SelectedTimeChanged += (_, e) => captured = e;

                spinner.SelectedTime = new TimeOnly(10, 15);

                Assert.NotNull(captured);
                Assert.Equal(new TimeOnly(9, 0), captured!.OldTime);
                Assert.Equal(new TimeOnly(10, 15), captured.NewTime);
            });
        }

        [Fact]
        public void Assigning_A_Time_That_Snaps_Onto_The_Current_One_Raises_Nothing()
        {
            RunSta(() =>
            {
                var spinner = Realize(new TimeSpinner { MinuteInterval = 15, SelectedTime = new TimeOnly(9, 0) });

                int raised = 0;
                spinner.SelectedTimeChanged += (_, _) => raised++;

                // 9:07 snaps back onto 9:00, so nothing about the selection actually changed.
                spinner.SelectedTime = new TimeOnly(9, 7);

                Assert.Equal(0, raised);
            });
        }

        #endregion

        #region XAML

        [Fact]
        public void Time_Bounds_Can_Be_Written_As_XAML_Literals()
        {
            RunSta(() =>
            {
                const string Markup =
                    """
                    <mosaic:TimeSpinner xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                        xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"
                                        MinimumTime="08:00" MaximumTime="17:00" SelectedTime="13:30" />
                    """;

                var spinner = (TimeSpinner)XamlReader.Parse(Markup);

                Assert.Equal(new TimeOnly(8, 0), spinner.MinimumTime);
                Assert.Equal(new TimeOnly(17, 0), spinner.MaximumTime);
                Assert.Equal(new TimeOnly(13, 30), spinner.SelectedTime);
            });
        }

        #endregion
    }
}
