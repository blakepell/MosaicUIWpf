using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Argus.Memory;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;
using Xunit;
using Xunit.Abstractions;
using ToggleButton = System.Windows.Controls.Primitives.ToggleButton;
using Button = System.Windows.Controls.Button;

namespace Mosaic.UI.Wpf.Tests
{
    public class TempProbeTests
    {
        private readonly ITestOutputHelper _output;

        public TempProbeTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public void Probe()
        {
            var lines = new List<string>();

            RunSta(() =>
            {
                var app = new Application();
                var theme = new ThemeManager { Native = true, SystemColors = true, Theme = MosaicThemeMode.Dark };
                app.Resources = new ResourceDictionary();
                app.Resources.MergedDictionaries.Add(theme);

                var viewer = new MarkdownViewer
                {
                    Markdown = "Hello world\n\n```csharp\nvar a = 1;\nvar b = 2;\n```",
                    FontSize = 14
                };

                var window = new Window { Width = 700, Height = 400, Content = viewer };
                window.Show();
                DoEvents();

                viewer.IsFindPanelOpen = true;
                viewer.FindText = "world";
                DoEvents();

                var findPanel = (FrameworkElement)viewer.Template.FindName("PART_FindPanel", viewer);
                var buttons = Descendants<Button>(findPanel).ToList();
                var toggles = Descendants<ToggleButton>(findPanel).ToList();

                lines.Add($"A theme={theme.Theme} buttons={buttons.Count} toggles={toggles.Count} appHasButtonStyle={app.Resources.Contains(typeof(Button))}");
                lines.Add($"B {Describe(buttons[0], app)}");
                lines.Add($"C {Describe(toggles[0], app)}");

                theme.Theme = MosaicThemeMode.Light;
                DoEvents();
                theme.Theme = MosaicThemeMode.Dark;
                DoEvents();

                var freshButton = new Button();
                var probeHost = new StackPanel();
                probeHost.Children.Add(freshButton);
                ((Grid)VisualTreeHelper.GetParent(findPanel)).Children.Add(probeHost);
                DoEvents();

                lines.Add($"X lookupViewer={Brush(viewer.FindResource(MosaicTheme.ControlBackgroundBrush) as Brush)} lookupApp={Brush(app.Resources[MosaicTheme.ControlBackgroundBrush] as Brush)} panelBorderBg={Brush((findPanel as Border)?.Background)} freshButtonBg={Brush(freshButton.Background)} viewerFg={Brush(viewer.Foreground)}");
                var findPanel2 = (FrameworkElement)viewer.Template.FindName("PART_FindPanel", viewer);
                var buttons2 = Descendants<Button>(findPanel2).ToList();
                var toggles2 = Descendants<ToggleButton>(findPanel2).ToList();
                lines.Add($"Y sameBorder={ReferenceEquals(findPanel, findPanel2)} sameButton={(buttons2.Count > 0 && ReferenceEquals(buttons[0], buttons2[0]))} border2Bg={Brush((findPanel2 as Border)?.Background)}");
                lines.Add($"Z requeried {(buttons2.Count == 0 ? "none" : Describe(buttons2[0], app))}");
                lines.Add($"W requeriedToggle {(toggles2.Count == 0 ? "none" : Describe(toggles2[0], app))}");
                lines.Add($"D after cycle {Describe(buttons[0], app)}");
                lines.Add($"E after cycle {Describe(toggles[0], app)}");

                // The same buttons in the syntax editor's search panel, which looks right.
                var editor = Descendants<SyntaxEditor>(viewer).FirstOrDefault();

                if (editor != null)
                {
                    editor.TextArea.Focus();
                    ApplicationCommands.Find.Execute(null, editor);
                    DoEvents();

                    var panelButton = Descendants<Button>(editor).FirstOrDefault();
                    lines.Add($"F editorPanelButton {(panelButton == null ? "none" : Describe(panelButton, app))}");
                }

                window.Close();
                app.Shutdown();
            });

            foreach (string line in lines)
            {
                _output.WriteLine(line);
            }

            Assert.True(false, string.Join("\n", lines));
        }

        private static string Describe(Control control, Application app)
        {
            object? appStyle = app.Resources[control is ToggleButton ? typeof(ToggleButton) : typeof(Button)];
            bool isApp = ReferenceEquals(control.Style, appStyle);
            bool basedOnIsApp = control.Style?.BasedOn != null && ReferenceEquals(control.Style.BasedOn, appStyle);

            return $"styleIsAppStyle={isApp} basedOnIsAppStyle={basedOnIsApp} appStyleNull={appStyle == null} " +
                   $"bg={Brush(control.Background)} fg={Brush(control.Foreground)} template={control.Template?.VisualTree?.Type.Name ?? control.Template?.GetType().Name}";
        }

        private static string Brush(Brush? brush)
        {
            return brush switch
            {
                SolidColorBrush s => s.Color.ToString(),
                null => "null",
                _ => brush.GetType().Name
            };
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);

                if (child is T match)
                {
                    yield return match;
                }

                foreach (var nested in Descendants<T>(child))
                {
                    yield return nested;
                }
            }
        }

        private static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

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
    }
}
