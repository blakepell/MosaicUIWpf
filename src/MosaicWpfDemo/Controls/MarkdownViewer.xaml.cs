using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Argus.Extensions;
using Argus.Memory;
using Markdig;
using Microsoft.Web.WebView2.Core;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Themes;

namespace MosaicWpfDemo.Controls
{
    public partial class MarkdownViewer : UserControl
    {
        private readonly TaskCompletionSource<bool> _webViewReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public MarkdownViewer()
        {
            InitializeComponent();

            // Attach initialization completed handler
            WebView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

            // If already initialized, set ready immediately
            if (WebView.CoreWebView2 != null)
            {
                _webViewReady.TrySetResult(true);
            }
            else
            {
                // Start initialization in background to reduce latency on first use
                _ = EnsureWebViewInitializedAsync();
            }
        }

        private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            _webViewReady.TrySetResult(e.IsSuccess);
        }

        private async Task EnsureWebViewInitializedAsync()
        {
            try
            {
                // Try to initialize WebView2; this will complete the event handler if successful.
                await WebView.EnsureCoreWebView2Async();
            }
            catch
            {
                // Ignore initialization exceptions; UpdateMarkdownAsync will handle fallback and retries.
            }

            // If CoreWebView2 is present after EnsureCoreWebView2Async, mark ready.
            if (WebView.CoreWebView2 != null)
            {
                _webViewReady.TrySetResult(true);
            }
        }

        // Wait for webview readiness with a short timeout and a final best-effort initialization attempt.
        private async Task EnsureWebViewReadyAsync(int timeoutMilliseconds = 2000)
        {
            if (_webViewReady.Task.IsCompleted)
            {
                return;
            }

            var completed = await Task.WhenAny(_webViewReady.Task, Task.Delay(timeoutMilliseconds));
            if (completed == _webViewReady.Task)
            {
                // ready
                await _webViewReady.Task; // observe exceptions if any (none expected)
                return;
            }

            // Timeout elapsed: attempt one last initialization attempt, but don't throw
            try
            {
                await WebView.EnsureCoreWebView2Async();
            }
            catch
            {
                // ignore
            }

            // If initialization happened after the attempt, set TCS to true
            if (WebView.CoreWebView2 != null)
            {
                _webViewReady.TrySetResult(true);
            }
        }

        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register(
                nameof(Markdown),
                typeof(string),
                typeof(MarkdownViewer),
                new PropertyMetadata(string.Empty, OnMarkdownChanged));

        public string Markdown
        {
            get => (string)GetValue(MarkdownProperty);
            set => SetValue(MarkdownProperty, value);
        }

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownViewer viewer)
            {
                // Fire-and-forget is acceptable for UI update; method handles exceptions.
                _ = viewer.UpdateMarkdownAsync(e.NewValue as string ?? string.Empty);
            }
        }

        private async Task UpdateMarkdownAsync(string markdown)
        {
            // Convert markdown to HTML using Markdig
            string convertedHtml;

            try
            {
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                convertedHtml = Markdig.Markdown.ToHtml(markdown, pipeline);
            }
            catch (Exception ex)
            {
                // Fallback: encode and show raw text if conversion fails
                convertedHtml = $"<pre>{ex.ToFormattedString().HtmlEncode()}</pre>";
            }

            string backgroundColor;
            string foregroundColor;
            string strTheme;

            var theme = AppServices.GetService<ThemeManager>();

            if (theme == null || theme.Theme == ThemeMode.Dark)
            {
                strTheme = "dark";
                backgroundColor = "#232325";
                foregroundColor = "#FFFFFF";
            }
            else
            {
                strTheme = "light";
                backgroundColor = "#FFFFFF";
                foregroundColor = "#000000";
            }

            // Basic HTML shell - place converted markdown inside the body
            string page = $@"<!doctype html>
<html data-theme=""{strTheme}"">
<head>
<meta charset=""utf-8"">
<meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/@picocss/pico@2/css/pico.min.css"">
<style>
body {{ margin: 0; padding: 16px; }}
</style>
</head>
<body>
{convertedHtml}
</body>
</html>";

            // Ensure the WebView2 control is initialized before navigating; wait briefly for readiness.
            try
            {
                await EnsureWebViewReadyAsync();
            }
            catch
            {
                // Ignore any exceptions here; we'll still attempt to navigate below.
            }

            try
            {
                WebView.NavigateToString(page);
            }
            catch
            {
                // Ignore navigate failures
            }
        }
    }
}
