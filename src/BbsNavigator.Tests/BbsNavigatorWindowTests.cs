/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Models;
using BbsNavigator.Views;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Themes;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace BbsNavigator.Tests;

public class BbsNavigatorWindowTests
{
    [Fact]
    public void NewWindowsLoadAndRenderWithMosaicTheme()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            string folder = Path.Combine(Path.GetTempPath(), "bbs-window-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var profile = new BbsProfile { Name = "Example BBS", Host = "example.invalid" };
                Window[] windows =
                [
                    new LoginSequenceWindow(Array.Empty<LoginStep>()),
                    new TextSendWindow("Hello everyone!\r\nA message composed locally.", profile.Name, Encoding.UTF8, 5, 100),
                    new MessageComposerWindow(profile, folder, _ => Task.FromResult(false), () => { }),
                    new BbsEditorWindow(profile)
                ];
                string screenshots = Path.Combine(AppContext.BaseDirectory, "bbs-window-previews");
                Directory.CreateDirectory(screenshots);
                foreach (Window window in windows)
                {
                    window.Resources.MergedDictionaries.Add(new ThemeManager { Theme = MosaicThemeMode.Blue, Native = true });
                    var content = (FrameworkElement)window.Content;
                    var size = new Size(window.Width, window.Height);
                    content.Measure(size);
                    content.Arrange(new Rect(size));
                    content.UpdateLayout();
                    Assert.True(content.ActualWidth > 0);
                    var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(content);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using (var stream = File.Create(Path.Combine(screenshots, window.GetType().Name + ".png"))) encoder.Save(stream);
                    if (window is TextSendWindow preview)
                    {
                        ((TextBox)preview.FindName("PreviewBox")).Text = "Updated\nmessage";
                        Assert.Equal("Updated\rmessage", preview.TextToSend);
                    }
                    window.Close();
                }
            }
            catch (Exception ex) { failure = ex; }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Window layout did not finish.");
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
