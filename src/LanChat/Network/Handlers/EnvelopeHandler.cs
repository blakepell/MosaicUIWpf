namespace LanChat.Network.Handlers
{
    /// <summary>
    /// Defines a contract for handling envelopes of a specific type in a chat system.
    /// </summary>
    /// <remarks>Implementations of this interface are responsible for processing envelopes associated with a
    /// specific type, as identified by the <see cref="TypeName"/> property. This interface is typically used in
    /// scenarios where different types of messages or data packets (envelopes) need to be handled by distinct
    /// handlers.</remarks>
    public interface IEnvelopeHandler
    {
        /// <summary>
        /// Gets the name of the handler.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Processes the provided envelope for the specified user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="envelope"></param>
        /// <param name="ct"></param>
        public Task HandleAsync(ChatUser user, object envelope, CancellationToken ct);
    }
}
