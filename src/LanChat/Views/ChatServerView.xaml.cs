using Argus.Memory;
using LanChat.Common;
using LanChat.Network;
using Mosaic.UI.Wpf.Controls;

namespace LanChat.Views
{
    public partial class ChatServerView
    {
        private readonly ChatServer _server;

        public ChatServerView()
        {
            InitializeComponent();
            _server = AppServices.GetRequiredService<ChatServer>();
        }

        private async void ServerIsOn_OnChanged(object? sender, ToggleSwitch.ToggleSwitchChangedEventArgs e)
        {
            try
            {
                if (e.IsOn)
                {
                    await _server.StartAsync();
                }
                else
                {
                    await _server.StopAsync();
                }
            }
            catch (Exception ex)
            {
                ToggleServerIsOn.IsOn = false;
                var vm = AppServices.GetRequiredService<AppViewModel>();
                vm.StatusText = $"Error: {ex.Message}";
            }
        }
    }
}
