/*
 * LanChat
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.ComponentModel;
using LanChat.Network;

namespace LanChat.Common
{
    /// <summary>
    /// A static instance application view model that can be used for observable
    /// app settings.  The AppViewModel is generally not persisted beyond the execution
    /// run of the app.  Prefer the <see cref="AppSettings"/> class for settings or state
    /// that needs to be saved and reloaded.
    /// </summary>
    public partial class AppViewModel : ObservableObject
    {
        /// <summary>
        /// A reference to the <see cref="AppSettings"/> for the project so that the application
        /// view model can be bound to the base windows/controls and we don't have to manage some
        /// things being bound to different things.
        /// </summary>
        [ObservableProperty]
        private AppSettings _appSettings = new();

        /// <summary>
        /// The title of the application
        /// </summary>
        [ObservableProperty]
        private string _title = "LanChat";

        /// <summary>
        /// Status text for the application that's shown in the status bar.
        /// </summary>
        [ObservableProperty]
        private string _statusText = "Ready";

        /// <summary>
        /// Gets or sets the local IP address associated with the current instance.
        /// </summary>
        [ObservableProperty]
        private string _lanIpAddress = "";

        /// <summary>
        /// Gets or sets the number of users currently connected.
        /// </summary>
        [ObservableProperty]
        private int _usersConnected = 0;

        /// <summary>
        /// Gets or sets the chat server instance used for hosting a chat session.
        /// </summary>
        [ObservableProperty]
        private ChatServer _chatServer = null!;

        /// <summary>
        /// Gets or sets the chat client instance used for connecting to a chat session.
        /// </summary>
        [ObservableProperty]
        private ChatClient _chatClient = null!;
    }
}
