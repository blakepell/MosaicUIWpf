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
using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A chat thread control which shows sent and received messages in a single thread format.
    /// </summary>
    /// <remarks>
    /// This code is based on https://github.com/SamKr/ChatBubbles.
    /// </remarks>
    public partial class ChatThread
    {
        #region Private Fields

        /// <summary>
        /// Represents the storyboard used to animate the ScrollViewer.
        /// </summary>
        /// <remarks>
        /// This field is used internally to manage animations for the ScrollViewer.
        /// </remarks>
        private readonly Storyboard _scrollViewerStoryboard;

        /// <summary>
        /// Represents a double animation used to scroll a ScrollViewer to its end position.
        /// </summary>
        /// <remarks>
        /// This animation is used to smoothly transition the ScrollViewer's vertical or horizontal
        /// offset to its maximum value.
        /// </remarks>
        private readonly DoubleAnimation _scrollViewerScrollToEndAnim;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Messages"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MessagesProperty = DependencyProperty.Register(
            nameof(Messages), typeof(ObservableCollection<Message>), typeof(ChatThread), new PropertyMetadata(new ObservableCollection<Message>()));

        /// <summary>
        /// Gets or sets the collection of messages.
        /// </summary>
        /// <remarks>Changes to the collection will automatically update any bindings to this property, as
        /// it uses an <see cref="ObservableCollection{T}"/>.</remarks>
        public ObservableCollection<Message> Messages
        {
            get => (ObservableCollection<Message>)GetValue(MessagesProperty);
            set => SetValue(MessagesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="VerticalOffset"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.Register(
            nameof(VerticalOffset), typeof(double), typeof(ChatThread), new PropertyMetadata(0.0, OnVerticalOffsetChanged));

        /// <summary>
        /// Gets or sets the vertical offset of the element.
        /// </summary>
        public double VerticalOffset
        {
            get => (double)GetValue(VerticalOffsetProperty);
            set => SetValue(VerticalOffsetProperty, value);
        }

        /// <summary>
        /// Called when the value of the vertical offset property changes.
        /// </summary>
        /// <param name="d">The object on which the property value has changed. This must be a <see cref="ChatThread"/> instance.</param>
        /// <param name="e">The event data containing information about the property change.</param>
        /// <remarks>
        /// This static calls into the implementation which is why there are two methods.
        /// </remarks>
        public static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChatThread app)
            {
                app.OnVerticalOffsetChanged(e);
            }
        }

        /// <summary>
        /// Handles changes to the vertical offset of the scroll viewer.
        /// </summary>
        /// <param name="e">The event data containing information about the change, including the new value of the vertical offset.</param>
        private void OnVerticalOffsetChanged(DependencyPropertyChangedEventArgs e)
        {
            MessagesScrollViewer.ScrollToVerticalOffset((double)e.NewValue);
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatThread"/> class.
        /// </summary>
        public ChatThread()
        {
            InitializeComponent();

            _scrollViewerScrollToEndAnim = new DoubleAnimation()
            {
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new SineEase()
            };

            Storyboard.SetTarget(_scrollViewerScrollToEndAnim, this);
            Storyboard.SetTargetProperty(_scrollViewerScrollToEndAnim, new PropertyPath(VerticalOffsetProperty));

            _scrollViewerStoryboard = new Storyboard();
            _scrollViewerStoryboard.Children.Add(_scrollViewerScrollToEndAnim);
        }

        /// <summary>
        /// Scrolls the conversation view to the end of the message list via an animation.
        /// </summary>
        public void ScrollConversationToEnd()
        {
            _scrollViewerScrollToEndAnim.From = MessagesScrollViewer.VerticalOffset;
            _scrollViewerScrollToEndAnim.To = MessagesScrollViewer.ExtentHeight;
            _scrollViewerStoryboard.Begin();
        }
    }
}
