/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Mosaic.UI.Wpf.Controls;
using Xunit;
using static Mosaic.UI.Wpf.Controls.TrayIconNative.NativeMethods;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Tray icon tests. These register real icons with the running shell (Explorer must be running) and drive the
    /// component by posting the same callback messages the shell sends.
    /// </summary>
    /// <summary>
    /// Runs tray icon tests alone: the handle-leak test reads process-wide GDI/USER counters.
    /// </summary>
    [CollectionDefinition("TrayIcon", DisableParallelization = true)]
    public class TrayIconCollection
    {
    }

    [Collection("TrayIcon")]
    public partial class TrayIconTests
    {
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_LBUTTONDBLCLK = 0x0203;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_CONTEXTMENU = 0x007B;
        private const uint NIN_KEYSELECT = 0x0401;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

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
                finally
                {
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
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
        /// Pumps the dispatcher for <paramref name="milliseconds"/> so timers and posted work run.
        /// </summary>
        private static void Pump(int milliseconds)
        {
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(milliseconds) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                frame.Continue = false;
            };
            timer.Start();
            Dispatcher.PushFrame(frame);
        }

        /// <summary>
        /// Sends a NOTIFYICON_VERSION_4 style callback to the tray icon's message window.
        /// </summary>
        private static void SendShellEvent(TrayIcon tray, uint eventCode, int x = 100, int y = 100)
        {
            var native = tray.Native ?? throw new InvalidOperationException("Tray icon is not shown.");
            Assert.True(native.IsVersion4, "Expected the shell to accept NOTIFYICON_VERSION_4.");

            var lParam = (IntPtr)(long)((TrayIconNative.IconId << 16) | eventCode);
            var wParam = (IntPtr)(long)(((uint)(ushort)(short)y << 16) | (ushort)(short)x);
            SendMessageW(native.Handle, TrayIconNative.CallbackMessage, wParam, lParam);
        }

        private static BitmapSource CreateBitmap(int size, byte alpha)
        {
            var pixels = new byte[size * size * 4];

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 0x20;     // B
                pixels[i + 1] = 0x80; // G
                pixels[i + 2] = 0xF0; // R
                pixels[i + 3] = alpha;
            }

            var bitmap = BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, size * 4);
            bitmap.Freeze();
            return bitmap;
        }

        private sealed class TestCommand : ICommand
        {
            public bool Allowed { get; set; } = true;

            public int Executions { get; private set; }

            public object? LastParameter { get; private set; }

            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }

            public bool CanExecute(object? parameter) => this.Allowed;

            public void Execute(object? parameter)
            {
                this.Executions++;
                this.LastParameter = parameter;
            }
        }

        [Fact]
        public void NotifyIconData_MatchesNativeLayout()
        {
            unsafe
            {
                // NOTIFYICONDATAW is 976 bytes on 64-bit and 956 bytes on 32-bit Windows.
                Assert.Equal(IntPtr.Size == 8 ? 976 : 956, sizeof(NOTIFYICONDATAW));
            }
        }

        [Fact]
        public void Construction_CreatesNoShellState()
        {
            RunSta(() =>
            {
                using var tray = new TrayIcon { ToolTipText = "Test", Icon = CreateBitmap(16, 255) };

                Assert.False(tray.IsVisible);
                Assert.False(tray.IsRegistered);
                Assert.Null(tray.Native);
            });
        }

        [Fact]
        public void ShowHide_AndIsVisible_ShareState()
        {
            RunSta(() =>
            {
                using var tray = new TrayIcon { ToolTipText = "Mosaic test icon" };

                tray.Show();
                Assert.True(tray.IsVisible);
                Assert.True(tray.IsRegistered);
                Assert.True(tray.Native!.IsAdded);

                tray.Hide();
                Assert.False(tray.IsVisible);
                Assert.False(tray.IsRegistered);
                Assert.Null(tray.Native);

                tray.IsVisible = true;
                Assert.True(tray.IsRegistered);

                tray.IsVisible = false;
                Assert.Null(tray.Native);
            });
        }

        [Fact]
        public void RuntimeUpdates_ApplyWhileVisible_AndUnchangedIconIsNotReconverted()
        {
            RunSta(() =>
            {
                using var tray = new TrayIcon { Icon = CreateBitmap(32, 255), ToolTipText = "One" };
                tray.Show();

                var firstHandle = tray.Native!.ShellIconHandle;
                Assert.NotEqual(IntPtr.Zero, firstHandle);

                tray.ToolTipText = new string('x', 300); // longer than the shell limit: truncated, not rejected
                Assert.True(tray.IsRegistered);
                Assert.Equal(firstHandle, tray.Native.ShellIconHandle);

                tray.Icon = CreateBitmap(16, 128);
                Assert.True(tray.IsRegistered);
                Assert.NotEqual(firstHandle, tray.Native.ShellIconHandle);
            });
        }

        [Fact]
        public void RepeatedIconUpdates_DoNotLeakHandles()
        {
            RunSta(() =>
            {
                using var tray = new TrayIcon { Icon = CreateBitmap(16, 255) };
                tray.Show();

                var process = GetCurrentProcess();
                uint gdiBefore = GetGuiResources(process, 0);
                uint userBefore = GetGuiResources(process, 1);

                for (int i = 0; i < 200; i++)
                {
                    tray.Icon = CreateBitmap(16, (byte)(i % 255));
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                uint gdiAfter = GetGuiResources(process, 0);
                uint userAfter = GetGuiResources(process, 1);

                // Each icon is two GDI bitmaps + one USER object; 200 leaked icons would be obvious.
                Assert.InRange((int)gdiAfter - (int)gdiBefore, int.MinValue, 10);
                Assert.InRange((int)userAfter - (int)userBefore, int.MinValue, 10);
            });
        }

        [Fact]
        public void MultipleInstances_CoexistAndRouteEventsIndependently()
        {
            RunSta(() =>
            {
                using var first = new TrayIcon { ToolTipText = "First" };
                using var second = new TrayIcon { ToolTipText = "Second" };
                int firstClicks = 0;
                int secondClicks = 0;
                first.LeftClick += (_, _) => firstClicks++;
                second.LeftClick += (_, _) => secondClicks++;

                first.Show();
                second.Show();

                Assert.True(first.IsRegistered);
                Assert.True(second.IsRegistered);
                Assert.NotEqual(first.Native!.Handle, second.Native!.Handle);

                SendShellEvent(second, WM_LBUTTONDOWN);
                SendShellEvent(second, WM_LBUTTONUP);

                Assert.Equal(0, firstClicks);
                Assert.Equal(1, secondClicks);
            });
        }

        [Fact]
        public void LeftClick_FiresImmediately_WhenDoubleClickIsNotHandled()
        {
            RunSta(() =>
            {
                using var tray = new TrayIcon();
                TrayIconEventArgs? args = null;
                tray.LeftClick += (_, e) => args = e;
                tray.Show();

                SendShellEvent(tray, WM_LBUTTONDOWN, 120, 340);
                SendShellEvent(tray, WM_LBUTTONUP, 120, 340);

                Assert.NotNull(args);
                Assert.Equal(TrayIconInputSource.Mouse, args!.InputSource);
                Assert.Equal(new Point(120, 340), args.ScreenPosition);
            });
        }

        [Fact]
        public void DoubleClick_DoesNotAlsoRaiseLeftClick()
        {
            RunSta(() =>
            {
                using var tray = new TrayIcon();
                int left = 0;
                int dbl = 0;
                tray.LeftClick += (_, _) => left++;
                tray.DoubleClick += (_, _) => dbl++;
                tray.Show();

                SendShellEvent(tray, WM_LBUTTONDOWN);
                SendShellEvent(tray, WM_LBUTTONUP);
                SendShellEvent(tray, WM_LBUTTONDBLCLK);
                SendShellEvent(tray, WM_LBUTTONUP);

                Pump((int)GetDoubleClickTime() + 200);

                Assert.Equal(1, dbl);
                Assert.Equal(0, left);

                // A lone click is still delivered once the double-click time elapses.
                SendShellEvent(tray, WM_LBUTTONDOWN);
                SendShellEvent(tray, WM_LBUTTONUP);
                Assert.Equal(0, left);

                Pump((int)GetDoubleClickTime() + 200);
                Assert.Equal(1, left);
            });
        }

        [Fact]
        public void Commands_RespectCanExecute_AndHandled()
        {
            RunSta(() =>
            {
                var command = new TestCommand { Allowed = false };
                using var tray = new TrayIcon { LeftClickCommand = command, CommandParameter = "param" };
                tray.Show();

                SendShellEvent(tray, WM_LBUTTONUP);
                Assert.Equal(0, command.Executions);

                command.Allowed = true;
                SendShellEvent(tray, WM_LBUTTONUP);
                Assert.Equal(1, command.Executions);
                Assert.Equal("param", command.LastParameter);

                tray.LeftClick += (_, e) => e.Handled = true;
                SendShellEvent(tray, WM_LBUTTONUP);
                Assert.Equal(1, command.Executions);
            });
        }

        [Fact]
        public void KeySelect_RaisesKeyboardActivation()
        {
            RunSta(() =>
            {
                using var tray = new TrayIcon();
                TrayIconEventArgs? args = null;
                tray.DoubleClick += (_, e) => args = e;
                tray.Show();

                // Only double-click is handled, so Enter/Space activates it.
                SendShellEvent(tray, NIN_KEYSELECT);

                Assert.NotNull(args);
                Assert.Equal(TrayIconInputSource.Keyboard, args!.InputSource);
            });
        }

        [Fact]
        public void ContextMenuRequest_RaisesRightClick_ThenCommand_ThenOpensMenu()
        {
            RunSta(() =>
            {
                var order = new List<string>();
                var command = new TestCommand();
                var menu = new ContextMenu { Items = { new MenuItem { Header = "Open" }, new Separator(), new MenuItem { Header = "Exit" } } };
                menu.Opened += (_, _) => order.Add("opened");

                using var tray = new TrayIcon { ContextMenu = menu, RightClickCommand = command };
                TrayIconInputSource? source = null;
                tray.RightClick += (_, e) =>
                {
                    order.Add("event");
                    source = e.InputSource;
                };
                tray.Show();

                SendShellEvent(tray, WM_RBUTTONUP);
                SendShellEvent(tray, WM_CONTEXTMENU);
                Pump(100);

                Assert.Equal(TrayIconInputSource.Mouse, source);
                Assert.Equal(1, command.Executions);
                Assert.True(menu.IsOpen);
                Assert.Equal(new[] { "event", "opened" }, order);

                menu.IsOpen = false;
                Pump(50);

                // Without a preceding right-button-up the request came from the keyboard.
                SendShellEvent(tray, WM_CONTEXTMENU);
                Pump(100);
                Assert.Equal(TrayIconInputSource.Keyboard, source);
                Assert.True(menu.IsOpen);

                // Hiding the icon closes a menu it opened.
                tray.Hide();
                Assert.False(menu.IsOpen);
            });
        }

        [Fact]
        public void RightClickHandled_SuppressesMenu()
        {
            RunSta(() =>
            {
                var menu = new ContextMenu { Items = { new MenuItem { Header = "Open" } } };
                using var tray = new TrayIcon { ContextMenu = menu };
                tray.RightClick += (_, e) => e.Handled = true;
                tray.Show();

                SendShellEvent(tray, WM_RBUTTONUP);
                SendShellEvent(tray, WM_CONTEXTMENU);
                Pump(50);

                Assert.False(menu.IsOpen);
            });
        }

        [Fact]
        public void MenuDataContext_FollowsTray_UnlessSetExplicitly()
        {
            RunSta(() =>
            {
                var viewModel = new object();
                var menu = new ContextMenu();
                using var tray = new TrayIcon { ContextMenu = menu, DataContext = viewModel };

                Assert.Same(viewModel, menu.DataContext);

                var other = new object();
                tray.DataContext = other;
                Assert.Same(other, menu.DataContext);

                var explicitContext = new object();
                var explicitMenu = new ContextMenu { DataContext = explicitContext };
                tray.ContextMenu = explicitMenu;
                Assert.Same(explicitContext, explicitMenu.DataContext);

                // The replaced menu no longer follows the tray.
                Assert.Null(menu.DataContext);
            });
        }

        [Fact]
        public void MenuItemCommandBinding_ResolvesAgainstTrayDataContext()
        {
            RunSta(() =>
            {
                var command = new TestCommand();
                var item = new MenuItem { Header = "Exit" };
                item.SetBinding(MenuItem.CommandProperty, new System.Windows.Data.Binding("ExitCommand"));
                var menu = new ContextMenu { Items = { item } };

                using var tray = new TrayIcon { ContextMenu = menu, DataContext = new { ExitCommand = (ICommand)command } };
                Pump(20);

                Assert.Same(command, item.Command);
            });
        }

        [Fact]
        public void XamlResource_CreatesNoShellState_AndBindsThroughInheritanceContext()
        {
            RunSta(() =>
            {
                const string xaml = """
                    <Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui">
                        <Border.Resources>
                            <mosaic:TrayIcon x:Key="Tray" ToolTipText="{Binding Title}">
                                <ContextMenu>
                                    <MenuItem Header="Open" Command="{Binding OpenCommand}" />
                                    <Separator />
                                    <MenuItem Header="Exit" />
                                </ContextMenu>
                            </mosaic:TrayIcon>
                        </Border.Resources>
                    </Border>
                    """;

                var command = new TestCommand();
                var border = (Border)System.Windows.Markup.XamlReader.Parse(xaml);
                border.DataContext = new { Title = "From view model", OpenCommand = (ICommand)command };

                using var tray = (TrayIcon)border.FindResource("Tray");
                Pump(50);

                Assert.False(tray.IsVisible);
                Assert.Null(tray.Native);
                Assert.Equal("From view model", tray.ToolTipText);
                Assert.NotNull(tray.ContextMenu);
                Assert.Equal(3, tray.ContextMenu!.Items.Count);
                Assert.Same(command, ((MenuItem)tray.ContextMenu.Items[0]).Command);
                Assert.Same(border.DataContext, tray.DataContext);

                // An explicit value overrides the inherited one; clearing it restores inheritance.
                var explicitContext = new object();
                tray.DataContext = explicitContext;
                Assert.Same(explicitContext, tray.ContextMenu.DataContext);

                tray.ClearValue(TrayIcon.DataContextProperty);
                Assert.Same(border.DataContext, tray.DataContext);
                Assert.Same(border.DataContext, tray.ContextMenu.DataContext);
            });
        }

        [Fact]
        public void TaskbarCreated_ReRegistersVisibleIcon()
        {
            RunSta(() =>
            {
                using var tray = new TrayIcon { ToolTipText = "Explorer restart" };
                tray.Show();

                SendMessageW(tray.Native!.Handle, TrayIconNative.TaskbarCreatedMessage, IntPtr.Zero, IntPtr.Zero);

                Assert.True(tray.IsRegistered);
                Assert.True(tray.Native.IsAdded);
                Assert.NotEqual(IntPtr.Zero, tray.Native.ShellIconHandle);
            });
        }

        [Fact]
        public void Dispose_IsIdempotent_AndBlocksFurtherUse()
        {
            RunSta(() =>
            {
                var tray = new TrayIcon();
                tray.Show();
                Assert.True(tray.IsRegistered);

                tray.Dispose();
                tray.Dispose();

                Assert.False(tray.IsVisible);
                Assert.False(tray.IsRegistered);
                Assert.Null(tray.Native);
                Assert.Throws<ObjectDisposedException>(() => tray.Show());

                tray.IsVisible = true; // coerced back to false, no registration
                Assert.False(tray.IsVisible);
                Assert.Null(tray.Native);
            });
        }

        [Fact]
        public void Dispose_FromOwnEventHandler_IsSafe()
        {
            RunSta(() =>
            {
                var tray = new TrayIcon();
                tray.LeftClick += (_, _) => tray.Dispose();
                tray.Show();

                SendShellEvent(tray, WM_LBUTTONUP);
                Pump(50);

                Assert.Null(tray.Native);
                Assert.False(tray.IsVisible);
            });
        }

        [Fact]
        public void Freezable_CannotFreeze_AndClonesAreHidden()
        {
            RunSta(() =>
            {
                using var tray = new TrayIcon { ToolTipText = "Original" };
                tray.Show();

                Assert.False(tray.CanFreeze);

                using var clone = (TrayIcon)tray.Clone();
                Assert.Equal("Original", clone.ToolTipText);
                Assert.False(clone.IsVisible);
                Assert.Null(clone.Native);
                Assert.True(tray.IsRegistered);
            });
        }

        [Fact]
        public void Converter_PreservesAlpha_AndUsesDirectCopyAtNativeSize()
        {
            RunSta(() =>
            {
                var source = CreateBitmap(16, 0x40);
                var direct = TrayIconImageConverter.Rasterize(source, 16)!;
                Assert.Equal(PixelFormats.Pbgra32, direct.Format);
                var premultiplied = new FormatConvertedBitmap(source, PixelFormats.Pbgra32, null, 0);
                Assert.Same(premultiplied, TrayIconImageConverter.Rasterize(premultiplied, 16));

                var scaled = TrayIconImageConverter.Rasterize(CreateBitmap(48, 0x40), 24)!;
                Assert.Equal(24, scaled.PixelWidth);
                Assert.Equal(PixelFormats.Pbgra32, scaled.Format);

                var pixels = new byte[24 * 24 * 4];
                scaled.CopyPixels(pixels, 24 * 4, 0);
                int center = ((12 * 24) + 12) * 4;
                Assert.InRange(pixels[center + 3], 0x3E, 0x42); // alpha survives
                Assert.InRange(pixels[center + 2], 59, 61); // 240 * 64 / 255

                using var icon = TrayIconImageConverter.Convert(source, 20);
                Assert.NotNull(icon);
                Assert.False(icon!.IsInvalid);
            });
        }

        [Fact]
        public void Converter_RendersVectorSources_AndRejectsEmptyOnes()
        {
            RunSta(() =>
            {
                var vector = new DrawingImage(new GeometryDrawing(Brushes.Red, null, new RectangleGeometry(new Rect(0, 0, 10, 5))));
                var bitmap = TrayIconImageConverter.Rasterize(vector, 32)!;
                Assert.Equal(32, bitmap.PixelWidth);

                // Non-square: uniformly scaled and centered, so the top row stays transparent.
                var pixels = new byte[32 * 32 * 4];
                bitmap.CopyPixels(pixels, 32 * 4, 0);
                Assert.Equal(0, pixels[3]);
                Assert.Equal(255, pixels[((16 * 32) + 16) * 4 + 3]);

                Assert.Null(TrayIconImageConverter.Convert(new DrawingImage(), 16));

                using var fallback = TrayIconImageConverter.GetDefaultIcon(16);
                Assert.False(fallback.IsInvalid);
            });
        }

        [Theory]
        // Bottom taskbar: work area ends at 1040, icon at y=1060 -> menu sits above the taskbar, opening right.
        [InlineData(1700, 1060, 0, 0, 1920, 1040, 1700, 840)]
        // Bottom taskbar, icon near the right edge -> flips to open left.
        [InlineData(1900, 1060, 0, 0, 1920, 1040, 1700, 840)]
        // Top taskbar: work area starts at 40, icon at y=20 -> menu below the taskbar.
        [InlineData(1700, 20, 0, 40, 1920, 1080, 1700, 40)]
        // Left taskbar: work area starts at x=60 -> menu pushed right of it.
        [InlineData(20, 900, 60, 0, 1920, 1080, 60, 700)]
        // Right taskbar: work area ends at x=1860 -> menu left of it.
        [InlineData(1890, 900, 0, 0, 1860, 1080, 1660, 700)]
        // Secondary monitor with negative coordinates (left of primary).
        [InlineData(-200, 1060, -1920, 0, 0, 1040, -200, 840)]
        public void ComputeMenuPosition_StaysInsideWorkArea(int ax, int ay, int l, int t, int r, int b, int ex, int ey)
        {
            var position = TrayIcon.ComputeMenuPosition(new POINT { X = ax, Y = ay }, 200, 200, new RECT(l, t, r, b));

            Assert.Equal(ex, position.X);
            Assert.Equal(ey, position.Y);
        }

        [Fact]
        public void ComputeMenuPosition_MenuLargerThanWorkArea_PinsToTopLeft()
        {
            var position = TrayIcon.ComputeMenuPosition(new POINT { X = 500, Y = 500 }, 2000, 2000, new RECT(0, 0, 1000, 1000));

            Assert.Equal(0, position.X);
            Assert.Equal(0, position.Y);
        }
    }
}
