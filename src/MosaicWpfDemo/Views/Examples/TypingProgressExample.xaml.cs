/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class TypingProgressExample
    {
        public ObservableCollection<Message> Messages { get; set; } = new();

        public TypingProgressExample()
        {
            this.DataContext = this;
            InitializeComponent();

            Messages.Add(new()
            {
                PreviousMessageDirection = MessageDirection.Received,
                Direction = MessageDirection.Received,
                From = "System",
                Text = "Type a message into the command box and press enter, then you will see a typing progress indicator show while I formulate my response.",
                Timestamp = DateTime.Now
            });
        }

        private async void CommandTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    e.Handled = true;

                    // Don't do anything if there isn't any text.
                    if (string.IsNullOrWhiteSpace(CommandTextBox.Text))
                    {
                        return;
                    }

                    this.Messages.Add(new Message()
                    {
                        Direction = MessageDirection.Sent,
                        Text = CommandTextBox.Text,
                        Timestamp = DateTime.Now,
                        From = Environment.UserName,
                        PreviousMessageDirection = this.Messages.Count > 0 ? this.Messages[^1].Direction : MessageDirection.Received
                    });

                    CommandTextBox.Text = "";

                    ChatThread.ScrollConversationToEnd();

                    // Generate a random number between 1 and 5
                    var random = new Random();

                    TypingProgress.IsRunning = true;

                    int delayMs = random.Next(1000, 3000);
                    await Task.Delay(delayMs);

                    TypingProgress.IsRunning = false;

                    this.Messages.Add(new Message()
                    {
                        Direction = MessageDirection.Received,
                        Text = "I am done typing now.",
                        Timestamp = DateTime.Now,
                        From = "System",
                        PreviousMessageDirection = this.Messages.Count > 0 ? this.Messages[^1].Direction : MessageDirection.Received
                    });

                    ChatThread.ScrollConversationToEnd();

                    break;
            }
        }
    }
}