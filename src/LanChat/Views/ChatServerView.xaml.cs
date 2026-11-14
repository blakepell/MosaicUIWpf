using Argus.Memory;
using LanChat.Common;
using LanChat.Network;
using System.Windows;

namespace LanChat.Views
{
    public partial class ChatServerView
    {
        private readonly ChatServer _server;

        public ChatServerView()
        {
            InitializeComponent();
            _server = AppServices.GetRequiredService<ChatServer>();
            this.SyncToggle();
        }

        private async void ServerIsOn_OnClick(object sender, RoutedEventArgs e)
        {
            var turnOn = ToggleServerIsOn.IsChecked == true;

            try
            {
                if (turnOn && !_server.IsListening)
                {
                    await _server.StartAsync();
                }
                else if (!turnOn && _server.IsListening)
                {
                    await _server.StopAsync();
                }
            }
            catch (Exception ex)
            {
                var vm = AppServices.GetRequiredService<AppViewModel>();
                vm.StatusText = $"Error: {ex.Message}";
            }
            finally
            {
                this.SyncToggle();
            }
        }

        private void SyncToggle()
        {
            ToggleServerIsOn.IsChecked = _server.IsListening;
            ToggleServerIsOn.Content = _server.IsListening ? "On" : "Off";
        }
    }
}
