using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;

namespace LanChat.Network
{
    /// <summary>
    /// Provides helper methods for framing and transmitting messages over a <see cref="NetworkStream"/>.
    /// </summary>
    /// <remarks>This class implements a message framing protocol where each message consists of a 5-byte header
    /// followed by a payload: <list type="bullet"> <item> <description>The header includes a 4-byte little-endian integer
    /// representing the total frame length (including the kind byte) and a 1-byte message kind.</description> </item>
    /// <item> <description>The payload contains the message data, which may be a UTF-8 string or JSON, depending on the
    /// message kind.</description> </item> </list> The class provides methods to write and read framed messages
    /// asynchronously, ensuring proper framing and payload handling.</remarks>
    internal static class Framing
    {
        // Message framing: [4-byte length LE][1-byte kind][payload bytes]
        // If kind == Text: payload is UTF-8 string bytes
        // If kind == JsonEnvelope: payload is UTF-8 JSON for MessageEnvelope
        public static async ValueTask WriteAsync(NetworkStream stream, MessageKind kind, ReadOnlyMemory<byte> payload, CancellationToken ct)
        {
            Span<byte> header = stackalloc byte[5];
            BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length + 1); // include kind byte
            header[4] = (byte)kind;
            await stream.WriteAsync(header.ToArray(), ct).ConfigureAwait(false);
            if (!payload.IsEmpty)
            {
                await stream.WriteAsync(payload, ct).ConfigureAwait(false);
            }

            await stream.FlushAsync(ct).ConfigureAwait(false);
        }

        public static async ValueTask<(MessageKind Kind, byte[] Payload)> ReadAsync(NetworkStream stream, CancellationToken ct)
        {
            // Read 5-byte header
            var header = new byte[5];
            await FillBufferAsync(stream, header, ct).ConfigureAwait(false);
            var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0, 4));
            if (length <= 0 || length > 8 * 1024 * 1024) // 8 MB sanity cap
            {
                throw new InvalidOperationException($"Invalid frame length: {length}");
            }

            var kind = (MessageKind)header[4];
            var payload = new byte[length - 1];
            if (payload.Length > 0)
            {
                await FillBufferAsync(stream, payload, ct).ConfigureAwait(false);
            }

            return (kind, payload);
        }

        private static async Task FillBufferAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
        {
            var read = 0;
            while (read < buffer.Length)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(read, buffer.Length - read), ct).ConfigureAwait(false);
                if (n == 0)
                {
                    throw new IOException("Remote closed connection.");
                }

                read += n;
            }
        }
    }
}