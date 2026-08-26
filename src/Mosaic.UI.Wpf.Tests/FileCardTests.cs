/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class FileCardTests
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
        private static FileCard Realize(FileCard control)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/FileCard/FileCard.xaml")
            };

            control.Style = (Style)dictionary[typeof(FileCard)];
            control.Measure(new Size(400, 400));
            control.Arrange(new Rect(0, 0, 400, 400));
            control.ApplyTemplate();
            control.UpdateLayout();

            return control;
        }

        /// <summary>
        /// Creates a temporary file of a known length so the formatted size can be asserted.
        /// </summary>
        private static string CreateTempFile(int bytes, string extension)
        {
            string path = Path.Combine(Path.GetTempPath(), $"mosaic-filecard-{Guid.NewGuid():N}{extension}");
            File.WriteAllBytes(path, new byte[bytes]);
            return path;
        }

        [Fact]
        public void Existing_File_Shows_Its_Name_And_Formatted_Size()
        {
            RunSta(() =>
            {
                string path = CreateTempFile(2048, ".txt");

                try
                {
                    var card = Realize(new FileCard { FilePath = path });

                    Assert.True(card.FileExists);
                    Assert.Equal(Path.GetFileName(path), card.FileName);
                    Assert.Equal("2 KB", card.FileSizeText);
                    Assert.NotNull(card.Icon);
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        [Fact]
        public void Missing_File_Keeps_The_Name_But_Reports_No_Size()
        {
            RunSta(() =>
            {
                string path = Path.Combine(Path.GetTempPath(), "mosaic-filecard-does-not-exist.xlsx");
                var card = Realize(new FileCard { FilePath = path });

                Assert.False(card.FileExists);
                Assert.Equal("mosaic-filecard-does-not-exist.xlsx", card.FileName);
                Assert.Equal(string.Empty, card.FileSizeText);

                // The error glyph stands in for the shell icon so the card is never blank.
                Assert.NotNull(card.Icon);
            });
        }

        [Fact]
        public void Card_Background_Is_Always_Painted_With_Or_Without_The_Tint()
        {
            RunSta(() =>
            {
                string path = CreateTempFile(10, ".txt");

                try
                {
                    var tinted = Realize(new FileCard { FilePath = path, IsTintEnabled = true });
                    var plain = Realize(new FileCard { FilePath = path, IsTintEnabled = false });

                    Assert.IsType<SolidColorBrush>(tinted.CardBackground);
                    Assert.IsType<SolidColorBrush>(plain.CardBackground);

                    // Toggling the property re-evaluates the brush rather than leaving a stale one behind.
                    var before = ((SolidColorBrush)tinted.CardBackground!).Color;
                    tinted.IsTintEnabled = false;
                    Assert.Equal(((SolidColorBrush)plain.CardBackground!).Color, ((SolidColorBrush)tinted.CardBackground!).Color);

                    tinted.IsTintEnabled = true;
                    Assert.Equal(before, ((SolidColorBrush)tinted.CardBackground!).Color);
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        [Fact]
        public void Template_Wires_An_Animatable_Transform_And_Shadow_That_Rest_Flat()
        {
            RunSta(() =>
            {
                var card = Realize(new FileCard { FilePath = @"C:\does-not-matter.txt" });
                var border = (Border)card.Template.FindName("PART_Card", card);

                // The raise is only reversible if these are per-instance and unfrozen, so a frozen or
                // missing transform is exactly the regression this guards.
                var transform = Assert.IsType<TranslateTransform>(border.RenderTransform);
                var shadow = Assert.IsType<DropShadowEffect>(border.Effect);

                Assert.False(transform.IsFrozen);
                Assert.False(shadow.IsFrozen);

                // With no pointer over the card it sits flat on the surface.
                Assert.Equal(0d, transform.Y);
                Assert.Equal(1d, shadow.ShadowDepth);
            });
        }

        [Fact]
        public void Clicking_Raises_The_Routed_Event_And_Passes_The_File_Path_To_The_Command()
        {
            RunSta(() =>
            {
                string path = CreateTempFile(1, ".dat");

                try
                {
                    object? received = null;
                    int clicks = 0;

                    var card = Realize(new FileCard
                    {
                        FilePath = path,
                        Command = new DelegateCommand(p => received = p)
                    });

                    card.Click += (_, _) => clicks++;
                    card.RaiseClick();

                    Assert.Equal(1, clicks);
                    Assert.Equal(path, received);
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        [Fact]
        public void An_Explicit_Command_Parameter_Wins_Over_The_File_Path()
        {
            RunSta(() =>
            {
                object? received = null;

                var card = Realize(new FileCard
                {
                    FilePath = @"C:\does-not-matter.txt",
                    CommandParameter = 42,
                    Command = new DelegateCommand(p => received = p)
                });

                card.RaiseClick();

                Assert.Equal(42, received);
            });
        }

        [Fact]
        public void Opening_The_File_On_Click_Is_Opt_In()
        {
            RunSta(() =>
            {
                var card = new FileCard();

                Assert.False(card.OpenFileOnClick);
                Assert.False((bool)FileCard.OpenFileOnClickProperty.DefaultMetadata.DefaultValue);
            });
        }

        [Fact]
        public void Missing_File_Does_Not_Raise_An_Error_Or_Skip_The_Command()
        {
            RunSta(() =>
            {
                int commands = 0;
                int errors = 0;
                string path = Path.Combine(Path.GetTempPath(), $"mosaic-filecard-missing-{Guid.NewGuid():N}.txt");
                var card = new FileCard
                {
                    FilePath = path,
                    OpenFileOnClick = true,
                    Command = new DelegateCommand(_ => commands++)
                };

                card.OnError += (_, _) => errors++;
                card.RaiseClick();

                Assert.Equal(1, commands);
                Assert.Equal(0, errors);
            });
        }

        [Fact]
        public void Shell_Failure_Raises_OnError_Without_Escaping_The_Click()
        {
            RunSta(() =>
            {
                string path = CreateTempFile(1, ".exe");

                try
                {
                    Exception? reported = null;
                    var card = new FileCard
                    {
                        FilePath = path,
                        OpenFileOnClick = true
                    };

                    card.OnError += (_, exception) => reported = exception;

                    Exception? escaped = Record.Exception(card.RaiseClick);

                    Assert.Null(escaped);
                    Assert.NotNull(reported);
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        /// <summary>
        /// Minimal <see cref="ICommand"/> used to capture the parameter the card supplies.
        /// </summary>
        private sealed class DelegateCommand : ICommand
        {
            private readonly Action<object?> _execute;

            public DelegateCommand(Action<object?> execute)
            {
                _execute = execute;
            }

            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter) => _execute(parameter);
        }
    }
}
