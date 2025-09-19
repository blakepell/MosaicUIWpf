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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using ServerInfo = LanChat.Common.SeverInfo;

namespace LanChat.Views
{
    public partial class ChatView
    {
        private ChatClient? _chatClient;
        private string _username = Environment.UserName;
        private bool _isReconnecting;

        public ObservableCollection<ServerInfo> ServerList { get; set; } = new();
        public ObservableCollection<Message> Messages { get; set; } = new();

        private CancellationTokenSource? _discoverCts;

        public ChatView()
        {
            this.DataContext = this;
            InitializeComponent();
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
            var vm2 = AppServices.GetRequiredService<AppViewModel>();

            // Prefer the selected server in the ComboBox; fallback to settings
            string host = vm2.AppSettings.IpAddress;
            int port = vm2.AppSettings.Port;
            if (ServerAddress != null && ServerAddress.SelectedItem is ServerInfo selected)
            {
                host = selected.IpAddress;
                port = selected.Port;
            }

            await _chatClient.ConnectAsync(host, port);

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

        private async void DiscoverServers_Click(object sender, RoutedEventArgs e)
        {
            // Toggle discovery
            if (_discoverCts != null)
            {
                _discoverCts.Cancel();
                _discoverCts = null;
                DiscoverButton.Content = "Discover";
                DiscoverButton.IsEnabled = true;
                return;
            }

            DiscoverButton.IsEnabled = false;
            DiscoverButton.Content = "Scanning...";

            _discoverCts = new CancellationTokenSource();
            var ct = _discoverCts.Token;

            try
            {
                var vm = AppServices.GetRequiredService<AppViewModel>();
                int port = vm.AppSettings.Port;

                var localIps = Argus.Network.Utilities.GetLocalIpAddresses()
                    .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Select(ip => ip.ToString())
                    .ToList();

                // Changed to hold discovered ServerInfo entries
                var found = new List<ServerInfo>();

                // Improved scan: for each local IP, try the /24 subnet but perform a lightweight discovery handshake
                var tasks = new List<Task>();
                var sem = new SemaphoreSlim(50); // limit concurrency

                foreach (var local in localIps)
                {
                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }

                    var parts = local.Split('.');
                    if (parts.Length != 4)
                    {
                        continue;
                    }

                    var prefix = string.Join('.', parts[0], parts[1], parts[2]);

                    for (int i = 1; i < 255; i++)
                    {
                        var candidate = $"{prefix}.{i}";
                        if (found.Any(f => f.IpAddress == candidate))
                        {
                            continue;
                        }

                        await sem.WaitAsync(ct);
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                using var tcp = new TcpClient();
                                var connectTask = tcp.ConnectAsync(candidate, port);
                                var delay = Task.Delay(300, ct); // 300ms timeout for connect
                                var completed = await Task.WhenAny(connectTask, delay);
                                if (completed != connectTask || !tcp.Connected)
                                {
                                    return; // not open
                                }

                                // Connected — perform a quick discovery handshake
                                try
                                {
                                    using var stream = tcp.GetStream();
                                    // Send a discovery request envelope
                                    var reqEnv = MessageEnvelope.Create(new { }, typeName: "DiscoveryRequest");
                                    var reqBytes = JsonSerializer.SerializeToUtf8Bytes(reqEnv, MessageEnvelope.DefaultJsonOptions);
                                    await Framing.WriteAsync(stream, MessageKind.JsonEnvelope, reqBytes, ct).ConfigureAwait(false);

                                    // Wait for a short response
                                    var readTask = Framing.ReadAsync(stream, ct).AsTask();
                                    var readDelay = Task.Delay(400, ct);
                                    var done = await Task.WhenAny(readTask, readDelay);
                                    if (done != readTask)
                                    {
                                        return; // no timely response
                                    }

                                    var (kind, payload) = await readTask.ConfigureAwait(false);
                                    if (kind != MessageKind.JsonEnvelope)
                                    {
                                        return;
                                    }

                                    var respEnv = JsonSerializer.Deserialize<MessageEnvelope?>(payload, MessageEnvelope.DefaultJsonOptions);
                                    if (respEnv is null)
                                    {
                                        return;
                                    }

                                    if (!string.IsNullOrEmpty(respEnv.Value.TypeName) && string.Equals(respEnv.Value.TypeName, "DiscoveryResponse", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            var doc = JsonSerializer.Deserialize<JsonElement>(respEnv.Value.Json, MessageEnvelope.DefaultJsonOptions);
                                            if (doc.ValueKind == JsonValueKind.Object && doc.TryGetProperty("isChatServer", out var prop) && prop.GetBoolean())
                                            {
                                                var serverName = candidate;
                                                if (doc.ValueKind == JsonValueKind.Object && doc.TryGetProperty("serverName", out var name) && name.ValueKind == JsonValueKind.String)
                                                {
                                                    var nameStr = name.GetString();
                                                    if (!string.IsNullOrWhiteSpace(nameStr))
                                                    {
                                                        serverName = nameStr!;
                                                    }
                                                }

                                                var info = new ServerInfo
                                                {
                                                    IpAddress = candidate,
                                                    Port = port,
                                                    ServerName = serverName,
                                                };

                                                lock (found)
                                                {
                                                    if (!found.Any(f => f.IpAddress == candidate))
                                                    {
                                                        found.Add(info);
                                                    }
                                                }
                                            }
                                        }
                                        catch { /* ignore malformed */ }
                                    }
                                }
                                catch { /* ignore */ }
                            }
                            catch { /* ignore */ }
                            finally
                            {
                                sem.Release();
                            }
                        }, ct));

                        if (tasks.Count > 0 && tasks.Count % 200 == 0)
                        {
                            // periodically await to avoid memory buildup
                            try
                            {
                                await Task.WhenAll(tasks.ToArray());
                            }
                            catch { /* ignore */ }

                            tasks.Clear();
                        }

                        if (ct.IsCancellationRequested)
                        {
                            break;
                        }
                    }

                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }
                }

                try
                {
                    await Task.WhenAll(tasks.ToArray());
                } 
                catch { /* ignore */ }

                // Merge results into the ServerList on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var info in found.OrderBy(x => x.IpAddress))
                    {
                        if (!ServerList.Any(s => s.IpAddress == info.IpAddress))
                        {
                            ServerList.Add(info);
                        }
                        else
                        {
                            // update LastSeen/Name if already present
                            var existing = ServerList.First(s => s.IpAddress == info.IpAddress);
                            existing.ServerName = info.ServerName;
                            existing.Port = info.Port;
                        }
                    }

                    if (ServerList.Count > 0 && ServerAddress.SelectedIndex < 0)
                    {
                        ServerAddress.SelectedIndex = 0;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // canceled
            }
            finally
            {
                DiscoverButton.Content = "Discover";
                DiscoverButton.IsEnabled = true;
                _discoverCts = null;
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
