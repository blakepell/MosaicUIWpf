using System.Text.Json;

namespace LanChat.Network
{
    /// <summary>
    /// Represents a typed JSON message envelope for sending structured objects.
    /// </summary>
    public readonly record struct MessageEnvelope(int TypeId, string? TypeName, string Json)
    {
        public static MessageEnvelope Create<T>(T value, string? typeName = null, int typeId = (int)MessageKind.JsonEnvelope, JsonSerializerOptions? options = null) => new(typeId, typeName ?? typeof(T).Name, JsonSerializer.Serialize(value, options ?? DefaultJsonOptions));

        public T? Deserialize<T>(JsonSerializerOptions? options = null) => JsonSerializer.Deserialize<T>(Json, options ?? DefaultJsonOptions);

        public static readonly JsonSerializerOptions DefaultJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
    }
}