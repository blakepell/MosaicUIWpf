/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Windows;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Verifies the bundled scripting reference's document links, heading links, and back history.
    /// </summary>
    public class ScriptReferenceNavigationTests
    {
        [Fact]
        public void EveryReferenceLinkLoadsFromResourcesAndCanNavigateBack()
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var viewer = new MarkdownViewer();
                    var styles = new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/MarkdownViewer/MarkdownViewer.xaml")
                    };
                    viewer.Style = (Style)styles[typeof(MarkdownViewer)];
                    viewer.Measure(new Size(800, 600));
                    viewer.Arrange(new Rect(0, 0, 800, 600));
                    viewer.ApplyTemplate();

                    var home = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Scripting/Docs/README.md");
                    var pending = new Queue<Uri>();
                    var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var navigate = typeof(MarkdownViewer).GetMethod("NavigateTo", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    pending.Enqueue(home);

                    while (pending.TryDequeue(out var source))
                    {
                        if (!visited.Add(source.AbsoluteUri))
                        {
                            continue;
                        }

                        viewer.Source = source;
                        Assert.DoesNotContain("# Document not found", viewer.Markdown);
                        var markdown = viewer.Markdown;

                        foreach (Match match in Regex.Matches(markdown, @"\]\(([^)]+\.md(?:#[^)]*)?)\)"))
                        {
                            var target = new Uri(source, match.Groups[1].Value);
                            navigate.Invoke(viewer, new object[] { target });
                            Assert.Equal(target, viewer.Source);
                            Assert.DoesNotContain("# Document not found", viewer.Markdown);

                            if (!string.IsNullOrEmpty(target.Fragment))
                            {
                                Assert.True(viewer.ScrollToAnchor(target.Fragment), $"Missing heading: {target}");
                            }

                            Assert.True(viewer.CanGoBack);
                            viewer.GoBack();
                            Assert.Equal(source, viewer.Source);
                            Assert.Equal(markdown, viewer.Markdown);
                            pending.Enqueue(new Uri(target.GetLeftPart(UriPartial.Query)));
                        }
                    }

                    Assert.Equal(12, visited.Count);
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
