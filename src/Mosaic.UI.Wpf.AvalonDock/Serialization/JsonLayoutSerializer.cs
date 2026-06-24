using Mosaic.UI.Wpf.AvalonDock.Interfaces;
using Mosaic.UI.Wpf.AvalonDock.Serialization.Dto;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mosaic.UI.Wpf.AvalonDock.Serialization
{
    /// <summary>
    /// JSON implementation of <see cref="ILayoutSerializer"/> based on System.Text.Json.
    /// Extends <see cref="LayoutSerializerBase"/> for layout-aware deserialization
    /// with fixup (reconnecting content, previous containers, callbacks).
    /// </summary>
    public class JsonLayoutSerializer : LayoutSerializerBase
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonLayoutSerializer"/> class.
        /// </summary>
        /// <param name="manager">The docking manager whose layout is serialized.</param>
        public JsonLayoutSerializer(IDockingManager manager)
            : base(manager)
        {
        }

        /// <inheritdoc/>
        protected override void SerializeCore(Stream stream, LayoutRootDto dto)
        {
            JsonSerializer.Serialize(stream, dto, SerializerOptions);
        }

        /// <inheritdoc/>
        protected override LayoutRootDto DeserializeCore(Stream stream)
        {
            return JsonSerializer.Deserialize<LayoutRootDto>(stream, SerializerOptions)
                ?? throw new InvalidDataException("The stream does not contain a valid layout.");
        }
    }
}
