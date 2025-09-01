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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class DebugExample
    {
        public ObservableCollection<Message> Messages { get; set; } = new();


        public DebugExample()
        {
            this.DataContext = this;
            InitializeComponent();

            Messages.Add(new()
            {
                PreviousMessageDirection = MessageDirection.Received,
                Direction = MessageDirection.Received,
                From = "System",
                Text = "This is the first message in the thread, respond using the text box below to add more content.",
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
                    int randomNumber = random.Next(1, 6);
                    string msg = string.Empty;

                    int delayMs = random.Next(1000, 3000);

                    await Task.Delay(delayMs);

                    switch (randomNumber)
                    {
                        case 1:
                            msg = "Cool cool.";
                            break;
                        case 2:
                            msg = "If you say so.";
                            break;
                        case 3:
                            msg = "Say that again?";
                            break;
                        case 4:
                            msg = "Interesting...";
                            break;
                        case 5:
                            msg = "Ok.";
                            break;
                    }

                    msg += $"\r\n\r\nI delayed {delayMs:N0}ms before responding.";

                    this.Messages.Add(new Message()
                    {
                        Direction = MessageDirection.Received,
                        Text = msg,
                        Timestamp = DateTime.Now,
                        From = Environment.UserName,
                        PreviousMessageDirection = this.Messages.Count > 0 ? this.Messages[^1].Direction : MessageDirection.Received
                    });

                    ChatThread.ScrollConversationToEnd();

                    break;
            }
        }
    }
}