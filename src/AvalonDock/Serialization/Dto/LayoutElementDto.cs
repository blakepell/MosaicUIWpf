#nullable disable
using System.Xml.Serialization;

namespace AvalonDock.Serialization.Dto
{
    /// <summary>
    /// Abstract base DTO for all layout elements.
    /// </summary>
    [XmlInclude(typeof(LayoutContentDto))]
    [XmlInclude(typeof(LayoutGroupDto))]
    [XmlInclude(typeof(LayoutFloatingWindowDto))]
    [XmlInclude(typeof(LayoutRootDto))]
    public abstract class LayoutElementDto
    {
    }
}