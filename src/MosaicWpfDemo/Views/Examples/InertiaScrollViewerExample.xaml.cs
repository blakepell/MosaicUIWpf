/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Windows;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class InertiaScrollViewerExample
    {
        public static readonly DependencyProperty IsScrollAnimationEnabledProperty = DependencyProperty.Register(
            nameof(IsScrollAnimationEnabled), typeof(bool), typeof(InertiaScrollViewerExample), new PropertyMetadata(true));

        public bool IsScrollAnimationEnabled
        {
            get => (bool)GetValue(IsScrollAnimationEnabledProperty);
            set => SetValue(IsScrollAnimationEnabledProperty, value);
        }

        public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty.Register(
            nameof(AnimationDuration), typeof(int), typeof(InertiaScrollViewerExample), new PropertyMetadata(800));

        public int AnimationDuration
        {
            get => (int)GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            nameof(FrameRate), typeof(int), typeof(InertiaScrollViewerExample), new PropertyMetadata(40));

        public int FrameRate
        {
            get => (int)GetValue(FrameRateProperty);
            set => SetValue(FrameRateProperty, value);
        }

        public ObservableCollection<ContentItem> ContentItems { get; set; } = new();

        public InertiaScrollViewerExample()
        {
            this.DataContext = this;
            InitializeComponent();
            GenerateContent();
        }

        private void GenerateContent()
        {
            var items = new[]
            {
                new ContentItem { Title = "Item 1", Description = "This is the first item in the scrollable content. It demonstrates how the InertiaScrollViewer handles smooth scrolling when animation is enabled." },
                new ContentItem { Title = "Item 2", Description = "The second item shows more content. Notice how the scrolling feels different when animation is on versus off." },
                new ContentItem { Title = "Item 3", Description = "This item contains even more text to demonstrate the scrolling behavior. The inertia effect makes scrolling feel more natural and responsive." },
                new ContentItem { Title = "Item 4", Description = "Another item with descriptive text. The animation duration controls how long the scrolling animation takes to complete." },
                new ContentItem { Title = "Item 5", Description = "The frame rate setting affects how smooth the animation appears. Higher frame rates provide smoother motion but use more resources." },
                new ContentItem { Title = "Item 6", Description = "This is item number six. Try scrolling quickly with the mouse wheel to see the easing effect in action." },
                new ContentItem { Title = "Item 7", Description = "Item seven demonstrates that the content can be of varying lengths and the scrolling animation adapts accordingly." },
                new ContentItem { Title = "Item 8", Description = "The eighth item shows how the InertiaScrollViewer maintains smooth performance even with multiple items." },
                new ContentItem { Title = "Item 9", Description = "This is the ninth item. The cubic easing function provides a natural deceleration that feels intuitive to users." },
                new ContentItem { Title = "Item 10", Description = "The tenth and final item. You can scroll back up to see how the animation works in both directions." }
            };

            foreach (var item in items)
            {
                ContentItems.Add(item);
            }
        }
    }

    public class ContentItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}