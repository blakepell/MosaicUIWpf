using Mosaic.UI.Wpf.AvalonDock.Serialization.Dto;

namespace Mosaic.UI.Wpf.AvalonDock.Interfaces
{
    /// <summary>
    /// Maps between the WPF layout tree and serialization DTOs.
    /// </summary>
    public interface ILayoutDtoMapper
    {
        /// <summary>Converts a layout root to a DTO.</summary>
        /// <param name="layout">The layout root to convert.</param>
        /// <returns>The DTO representation.</returns>
        LayoutRootDto ToDto(ISerializableLayoutRoot layout);

        /// <summary>Converts a DTO back to a layout root.</summary>
        /// <param name="dto">The DTO to convert.</param>
        /// <returns>The layout root.</returns>
        ISerializableLayoutRoot FromDto(LayoutRootDto dto);
    }
}
