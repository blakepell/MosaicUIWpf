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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class WebcamTests
    {
        private static readonly Guid Nv12 = new("3231564e-0000-0010-8000-00aa00389b71");
        private static readonly Guid Yuy2 = new("32595559-0000-0010-8000-00aa00389b71");
        private static readonly Guid Mjpg = new("47504a4d-0000-0010-8000-00aa00389b71");

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
        /// Runs an async test body on an STA thread with a dispatcher pumping, which the control's
        /// lifecycle (dispatcher-marshalled frame rendering, async continuations) requires.
        /// </summary>
        private static void RunStaAsync(Func<Task> body)
        {
            RunSta(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var frame = new DispatcherFrame();
                var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
                timeout.Tick += (_, _) => frame.Continue = false;
                timeout.Start();

                Task task = body();
                task.ContinueWith(_ => frame.Continue = false, TaskScheduler.FromCurrentSynchronizationContext());
                Dispatcher.PushFrame(frame);
                timeout.Stop();

                Assert.True(task.IsCompleted, "The test body did not finish within 30 seconds.");
                task.GetAwaiter().GetResult();
            });
        }

        private static NativeVideoMode Mode(int index, Guid subtype, int width, int height, uint fps)
        {
            return new NativeVideoMode(index, subtype, width, height, fps, 1);
        }

        [Fact]
        public void AutomaticSelection_PrefersMainstream1080p30_OverHigherResolutionAndFrameRate()
        {
            var modes = new List<NativeVideoMode>
            {
                Mode(0, Mjpg, 3840, 2160, 30),
                Mode(1, Mjpg, 1920, 1080, 60),
                Mode(2, Mjpg, 1920, 1080, 30),
                Mode(3, Mjpg, 1280, 720, 30),
                Mode(4, Yuy2, 1920, 1080, 5)
            };

            var ordered = WebcamModeSelector.OrderCandidates(modes, null);

            Assert.Equal(2, ordered[0].Index);
            Assert.Equal(1, ordered[1].Index);
        }

        [Fact]
        public void AutomaticSelection_PrefersCheaperSubtype_WhenModesAreOtherwiseEqual()
        {
            var modes = new List<NativeVideoMode>
            {
                Mode(0, Mjpg, 1280, 720, 30),
                Mode(1, Yuy2, 1280, 720, 30),
                Mode(2, Nv12, 1280, 720, 30)
            };

            Assert.Equal(2, WebcamModeSelector.OrderCandidates(modes, null)[0].Index);
        }

        [Fact]
        public void AutomaticSelection_ChoosesSmallestOversizeMode_WhenNothingFitsWithin1080p()
        {
            var modes = new List<NativeVideoMode>
            {
                Mode(0, Mjpg, 3840, 2160, 30),
                Mode(1, Mjpg, 2560, 1440, 30)
            };

            Assert.Equal(1, WebcamModeSelector.OrderCandidates(modes, null)[0].Index);
        }

        [Fact]
        public void RequestedSelection_UsesExactMatchFirst_ThenClosest()
        {
            var modes = new List<NativeVideoMode>
            {
                Mode(0, Mjpg, 1920, 1080, 30),
                Mode(1, Mjpg, 640, 480, 30),
                Mode(2, Mjpg, 1280, 720, 30)
            };

            Assert.Equal(1, WebcamModeSelector.OrderCandidates(modes, new WebcamResolution(640, 480, 30))[0].Index);
            Assert.Equal(2, WebcamModeSelector.OrderCandidates(modes, new WebcamResolution(1280, 800, 30))[0].Index);
        }

        [Fact]
        public void Selection_IgnoresModesWithoutSizeOrFrameRate()
        {
            var modes = new List<NativeVideoMode>
            {
                new(0, Mjpg, 0, 0, 30, 1),
                new(1, Mjpg, 640, 480, 0, 0)
            };

            Assert.Empty(WebcamModeSelector.OrderCandidates(modes, null));
        }

        [Fact]
        public void DistinctResolutions_CollapseSubtypesAndSortLargestFirst()
        {
            var modes = new List<NativeVideoMode>
            {
                Mode(0, Yuy2, 640, 480, 30),
                Mode(1, Mjpg, 640, 480, 30),
                Mode(2, Mjpg, 1920, 1080, 30),
                new(3, Mjpg, 1280, 720, 30000, 1001)
            };

            var list = WebcamModeSelector.GetDistinctResolutions(modes);

            Assert.Equal(
                [new WebcamResolution(1920, 1080, 30), new WebcamResolution(1280, 720, 30), new WebcamResolution(640, 480, 30)],
                list);
        }

        [Fact]
        public void FrameExchange_DropsStaleFrames_AndSchedulesAtMostOnce()
        {
            var exchange = new WebcamFrameExchange();
            int schedules = 0;

            for (int i = 1; i <= 5; i++)
            {
                WebcamFrame frame = exchange.WriteFrame;
                frame.EnsureFormat(2, 2);
                frame.FrameNumber = i;

                if (exchange.Publish())
                {
                    schedules++;
                }
            }

            Assert.Equal(1, schedules);

            WebcamFrame? latest = exchange.AcquireLatest();
            Assert.NotNull(latest);
            Assert.Equal(5, latest.FrameNumber);
            Assert.Null(exchange.AcquireLatest());

            // After the consumer ran, the next frame schedules again.
            exchange.WriteFrame.FrameNumber = 6;
            Assert.True(exchange.Publish());
        }

        [Fact]
        public void FrameExchange_NeverHandsTheConsumersFrameToTheProducer()
        {
            var exchange = new WebcamFrameExchange();
            exchange.Publish();
            WebcamFrame? reading = exchange.AcquireLatest();

            for (int i = 0; i < 10; i++)
            {
                Assert.NotSame(reading, exchange.WriteFrame);
                exchange.Publish();
            }
        }

        [Fact]
        public void FrameEventArgs_CopyPixels_ThrowsAfterInvalidation()
        {
            var frame = new WebcamFrame();
            frame.EnsureFormat(2, 1);
            frame.Pixels[0] = 42;

            var args = new WebcamFrameEventArgs(frame);
            var buffer = new byte[args.Stride * args.PixelHeight];
            args.CopyPixels(buffer);
            Assert.Equal(42, buffer[0]);

            args.Invalidate();
            Assert.Throws<InvalidOperationException>(() => args.CopyPixels(buffer));
        }

        [Fact]
        public void Device_EqualityUsesSymbolicLinkCaseInsensitively()
        {
            var a = new WebcamDevice("Camera", @"\\?\usb#vid_046d&pid_085c");
            var b = new WebcamDevice("Renamed", @"\\?\USB#VID_046D&PID_085C");

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.Equal("Camera", a.ToString());
            Assert.Throws<ArgumentException>(() => new WebcamDevice("x", " "));
        }

        [Fact]
        public void ShippedTemplate_ProvidesImagePart_AndMirrorsWithRenderTransform()
        {
            RunSta(() =>
            {
                var dictionary = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/Webcam/Webcam.xaml")
                };

                var webcam = new Webcam { Style = (Style)dictionary[typeof(Webcam)] };
                webcam.Measure(new Size(320, 240));
                webcam.Arrange(new Rect(0, 0, 320, 240));
                webcam.ApplyTemplate();

                var image = Assert.IsType<Image>(webcam.Template.FindName("PART_Image", webcam));
                Assert.Equal(Stretch.Uniform, image.Stretch);
                Assert.Equal(WebcamState.Stopped, webcam.State);
                Assert.False(webcam.IsRunning);

                webcam.Mirror = true;
                var scale = Assert.IsType<ScaleTransform>(image.RenderTransform);
                Assert.Equal(-1, scale.ScaleX);
            });
        }

        [Fact]
        public void StartAsync_WithoutDevice_FaultsAndRaisesError_WithoutChangingState()
        {
            RunSta(() =>
            {
                var webcam = new Webcam();
                WebcamErrorEventArgs? raised = null;
                webcam.Error += (_, e) => raised = e;

                Task start = webcam.StartAsync();

                var exception = Assert.IsType<WebcamException>(start.Exception?.InnerException);
                Assert.Equal(WebcamErrorKind.DeviceNotSelected, exception.Kind);
                Assert.Equal(WebcamErrorKind.DeviceNotSelected, raised?.Kind);
                Assert.Equal(WebcamState.Stopped, webcam.State);
                Assert.True(webcam.StopAsync().IsCompletedSuccessfully);
            });
        }

        [Fact]
        public void CaptureFrameAsync_ReturnsNull_WhenNothingHasBeenDisplayed()
        {
            RunSta(() =>
            {
                Task<BitmapSource?> capture = new Webcam().CaptureFrameAsync();
                Assert.True(capture.IsCompletedSuccessfully);
                Assert.Null(capture.Result);
            });
        }

        [Fact]
        public void DeviceEnumeration_ReleasesCleanly_AcrossRepeatedCalls()
        {
            // Exercises MFStartup/MFShutdown reference counting and IMFActivate release; works with zero cameras.
            for (int i = 0; i < 5; i++)
            {
                IReadOnlyList<WebcamDevice> devices = WebcamDeviceManager.GetDevices();
                Assert.All(devices, d => Assert.False(string.IsNullOrWhiteSpace(d.SymbolicLink)));
            }
        }

        /// <summary>
        /// End-to-end against real hardware: start, receive frames, capture, stop, restart.  Passes
        /// trivially on machines without a camera (CI).
        /// </summary>
        [Fact]
        public void Hardware_StartCaptureStopRestart_WhenACameraIsPresent()
        {
            WebcamDevice? device = WebcamDeviceManager.GetDevices().FirstOrDefault();

            if (device is null)
            {
                return;
            }

            RunStaAsync(async () =>
            {
                var webcam = new Webcam { Device = device };
                int frames = 0;
                webcam.FrameReceived += (_, _) => frames++;

                for (int cycle = 0; cycle < 2; cycle++)
                {
                    await webcam.StartAsync();
                    Assert.Equal(WebcamState.Running, webcam.State);
                    Assert.NotNull(webcam.ActualResolution);
                    Assert.NotEmpty(webcam.AvailableResolutions);

                    BitmapSource? snapshot = await webcam.CaptureFrameAsync();
                    Assert.NotNull(snapshot);
                    Assert.True(snapshot.IsFrozen);
                    Assert.Equal(webcam.ActualResolution!.Width, snapshot.PixelWidth);

                    await webcam.StopAsync();
                    Assert.Equal(WebcamState.Stopped, webcam.State);
                    Assert.Null(webcam.ActualResolution);
                }

                Assert.True(frames > 0);
            });
        }
    }
}
