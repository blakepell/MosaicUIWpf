using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Argus.Memory;
using LanChat.Common;
using LanChat.Network;
using Mosaic.UI.Wpf.Cache;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Extensions;

namespace LanChat.Views
{
    public partial class ChatView
    {
        private ChatClient? _chatClient;
        private string _username = Environment.UserName;
        private bool _isReconnecting;

        public ObservableCollection<string> ServerList { get; set; } = new();

        public ObservableCollection<Message> Messages { get; set; } = new();

        public ChatView()
        {
            this.DataContext = this;
            InitializeComponent();
        }

        private void ChatView_OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var ip in Argus.Network.Utilities.GetLocalIpAddresses().OrderBy(ip => ip.ToString()))
            {
                string ipAddress = ip.ToString();

                if (!ServerList.Contains(ipAddress))
                {
                    ServerList.Add(ipAddress);
                }
            }

            if (!ServerList.Contains("127.0.0.1"))
            {
                ServerList.Add("127.0.0.1");
            }

            if (ServerList.Count > 0)
            {
                ServerAddress.SelectedIndex = 0;
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Get username from the input field
            var username = UsernameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Please enter a username.", "Username Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _username = username;

            try
            {
                // Disable the button while connecting
                if (sender is Button btn)
                {
                    btn.IsEnabled = false;
                }

                await ConnectAndLoginAsync();

                // Hide login UI and show chat UI
                LoginPanel.Visibility = Visibility.Collapsed;
                ChatThread.Visibility = Visibility.Visible;
                CommandTextBox.Visibility = Visibility.Visible;
                CommandTextBox.Focus();
            }
            catch (Exception ex)
            {
                // If connection fails, show error and keep login visible
                MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (sender is Button btn2)
                {
                    btn2.IsEnabled = true;
                }
            }
        }

        private async void ChatClient_Disconnected(object? sender, EventArgs e)
        {
            // If we are already trying to reconnect, do nothing.
            if (_isReconnecting)
            {
                return;
            }

            try
            {
                _isReconnecting = true;
                AddMessage(new Message
                {
                    Text = "Connection lost. Attempting to reconnect...",
                    From = "System",
                    Direction = MessageDirection.Received,
                    BackgroundBrush = Brushes.DarkOrange,
                    ForegroundBrush = Brushes.White,
                    Timestamp = DateTime.Now
                });

                // Keep trying to reconnect
                while (true)
                {
                    try
                    {
                        await Task.Delay(5000); // Wait 5 seconds before retrying
                        await ConnectAndLoginAsync();

                        // If we successfully connect and log in, break the loop.
                        if (_chatClient?.IsConnected == true)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to reconnect: {ex.Message}");
                        // The loop will continue and try again.
                    }
                }

                AddMessage(new Message
                {
                    Text = "Reconnected successfully.",
                    From = "System",
                    Direction = MessageDirection.Received,
                    BackgroundBrush = Brushes.DarkGreen,
                    ForegroundBrush = Brushes.White,
                    Timestamp = DateTime.Now
                });
            }
            finally
            {
                _isReconnecting = false;
            }
        }

        private async Task ConnectAndLoginAsync()
        {
            if (_chatClient != null)
            {
                _chatClient.Disconnected -= ChatClient_Disconnected;
                await _chatClient.DisposeAsync();
            }

            var vm = AppServices.GetRequiredService<AppViewModel>();
            _chatClient = new ChatClient();
            vm.ChatClient = _chatClient;
            _chatClient.Username = _username;

            // Wire up event handlers
            _chatClient.Disconnected += ChatClient_Disconnected;
            _chatClient.TextReceived += (s, e) =>
            {
                AddMessage(new Message()
                {
                    BackgroundBrush = ColorPaletteCache.GetBrush("#03467A"),
                    From = e.Username,
                    Timestamp = DateTime.Now,
                    Direction = MessageDirection.Received,
                    ForegroundBrush = Brushes.White,
                    Text = e.Text
                });

                Console.WriteLine($"[SERVER TEXT]: {e.Text}");
            };

            _chatClient.EnvelopeReceived += (s, e) =>
            {
                // Handle system messages
                if (!string.IsNullOrEmpty(e.Envelope.TypeName) &&
                    string.Equals(e.Envelope.TypeName, "SystemMessage", StringComparison.OrdinalIgnoreCase))
                {
                    var payload = e.Envelope.Deserialize<SystemMessagePayload>();

                    if (!string.IsNullOrWhiteSpace(payload?.Text))
                    {
                        AddMessage(new Message()
                        {
                            BackgroundBrush = Brushes.DarkGreen,
                            From = "System",
                            Timestamp = DateTime.Now,
                            Direction = MessageDirection.Received,
                            ForegroundBrush = Brushes.White,
                            Text = payload.Text
                        });
                    }
                    return;
                }

                // Handle chat messages
                if (!string.IsNullOrEmpty(e.Envelope.TypeName) &&
                    string.Equals(e.Envelope.TypeName, "ChatMessage", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var payload = e.Envelope.Deserialize<ChatMessagePayload>();

                        var from = payload?.DisplayName ?? payload?.Sender ?? "Unknown";

                        // Determine message direction
                        MessageDirection direction = MessageDirection.Received;
                        if (payload?.Sender == _username)
                        {
                            direction = MessageDirection.Sent;
                        }

                        AddMessage(new Message()
                        {
                            BackgroundBrush = ColorPaletteCache.GetBrush("#03467A"),
                            From = from,
                            Timestamp = DateTime.Now,
                            Direction = direction,
                            ForegroundBrush = Brushes.White,
                            Text = payload?.Text ?? string.Empty
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to parse ChatMessage envelope: {ex}");
                    }
                    return;
                }

                // Fallback: just log other envelopes
                Console.WriteLine($"[SERVER ENV]: {e.Envelope.TypeName} -> {e.Envelope.Json}");
            };

            // Connect to server
            await _chatClient.ConnectAsync(vm.AppSettings.IpAddress, vm.AppSettings.Port);

            // Login with username
            await _chatClient.LoginAsync(_username);
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

                    var messageText = CommandTextBox.Text;

                    // Send the actual user message, the server will echo it back to us.
                    try
                    {
                        await _chatClient!.SendTextAsync(messageText);
                        CommandTextBox.Text = "";
                    }
                    catch (Exception ex)
                    {
                        AddMessage(new()
                        {
                            Direction = MessageDirection.Received,
                            BackgroundBrush = Brushes.Maroon,
                            ForegroundBrush = Brushes.White,
                            From = "System",
                            Timestamp = DateTime.Now,
                            Text = $"Failed to send message: {ex.Message}"
                        });
                    }
                    break;
            }
        }

        /// <summary>
        /// Adds a message to the collection and updates the chat thread view.` 
        /// </summary>
        /// <remarks>This method ensures that the operation is performed on the UI thread. After adding
        /// the message, the chat thread view is scrolled to the end.</remarks>
        /// <param name="msg">The message to add. The message's <see cref="Message.PreviousMessageDirection"/> property will be set based
        /// on the direction of the last message in the collection, or <see cref="MessageDirection.Received"/> if the
        /// collection is empty.</param>
        public void AddMessage(Message msg)
        {
            Application.Current.Dispatcher.InvokeIfRequired(() =>
            {
                msg.PreviousMessageDirection = this.Messages.Count > 0 ? this.Messages[^1].Direction : MessageDirection.Received;
                this.Messages.Add(msg);
                ChatThread.ScrollConversationToEnd();
            });
        }
    }
}
