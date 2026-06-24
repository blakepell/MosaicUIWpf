using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mosaic.UI.Wpf.AvalonDock.Serialization.Dto
{
    /// <summary>
    /// Abstract base DTO for all layout elements.
    /// </summary>
    /// <remarks>
    /// The DTOs in this file are plain data carriers for the JSON layout format and
    /// deliberately have no initializers: the serializer omits default values
    /// (<c>false</c>, <c>0</c>, <c>null</c>) when writing, so a non-default initializer
    /// would corrupt the round trip when the property is omitted. The
    /// <see cref="LayoutDtoMapper"/> sets every property explicitly in both directions.
    /// </remarks>
    public abstract class LayoutElementDto
    {
    }

    /// <summary>
    /// Abstract DTO base for content items (documents and anchorables).
    /// </summary>
    [JsonPolymorphic]
    [JsonDerivedType(typeof(LayoutDocumentDto), "LayoutDocument")]
    [JsonDerivedType(typeof(LayoutAnchorableDto), "LayoutAnchorable")]
    public abstract class LayoutContentDto : LayoutElementDto
    {
        /// <summary>Gets or sets the title.</summary>
        public string? Title { get; set; }

        /// <summary>Gets or sets the content identifier.</summary>
        public string? ContentId { get; set; }

        /// <summary>Gets or sets a value indicating whether this item is selected.</summary>
        public bool IsSelected { get; set; }

        /// <summary>Gets or sets a value indicating whether this is the last focused document.</summary>
        public bool IsLastFocusedDocument { get; set; }

        /// <summary>Gets or sets the tooltip text.</summary>
        public string? ToolTip { get; set; }

        /// <summary>Gets or sets the floating left position.</summary>
        public double FloatingLeft { get; set; }

        /// <summary>Gets or sets the floating top position.</summary>
        public double FloatingTop { get; set; }

        /// <summary>Gets or sets the floating width.</summary>
        public double FloatingWidth { get; set; }

        /// <summary>Gets or sets the floating height.</summary>
        public double FloatingHeight { get; set; }

        /// <summary>Gets or sets a value indicating whether this item is maximized.</summary>
        public bool IsMaximized { get; set; }

        /// <summary>Gets or sets a value indicating whether this item can be closed.</summary>
        public bool CanClose { get; set; }

        /// <summary>Gets or sets a value indicating whether this item can float.</summary>
        public bool CanFloat { get; set; }

        /// <summary>Gets or sets the last activation timestamp.</summary>
        public DateTime? LastActivationTimeStamp { get; set; }

        /// <summary>Gets or sets a value indicating whether this item can show on hover.</summary>
        public bool CanShowOnHover { get; set; }

        /// <summary>Gets or sets the previous container identifier.</summary>
        public string? PreviousContainerId { get; set; }

        /// <summary>Gets or sets the previous container index.</summary>
        public int PreviousContainerIndex { get; set; }
    }

    /// <summary>
    /// DTO for a layout document.
    /// </summary>
    public class LayoutDocumentDto : LayoutContentDto
    {
        /// <summary>Gets or sets the description.</summary>
        public string? Description { get; set; }

        /// <summary>Gets or sets a value indicating whether this document can be moved.</summary>
        public bool CanMove { get; set; }
    }

    /// <summary>
    /// DTO for a layout anchorable (tool window).
    /// </summary>
    public class LayoutAnchorableDto : LayoutContentDto
    {
        /// <summary>Gets or sets a value indicating whether this anchorable can be hidden.</summary>
        public bool CanHide { get; set; }

        /// <summary>Gets or sets a value indicating whether this anchorable can auto-hide.</summary>
        public bool CanAutoHide { get; set; }

        /// <summary>Gets or sets the auto-hide width.</summary>
        public double AutoHideWidth { get; set; }

        /// <summary>Gets or sets the auto-hide height.</summary>
        public double AutoHideHeight { get; set; }

        /// <summary>Gets or sets the auto-hide minimum width.</summary>
        public double AutoHideMinWidth { get; set; }

        /// <summary>Gets or sets the auto-hide minimum height.</summary>
        public double AutoHideMinHeight { get; set; }

        /// <summary>Gets or sets a value indicating whether this anchorable can dock as a tabbed document.</summary>
        public bool CanDockAsTabbedDocument { get; set; }

        /// <summary>Gets or sets a value indicating whether this anchorable can be moved.</summary>
        public bool CanMove { get; set; }
    }

    /// <summary>
    /// Abstract DTO base for positionable groups (panels, panes, pane groups).
    /// </summary>
    [JsonPolymorphic]
    [JsonDerivedType(typeof(LayoutPanelDto), "LayoutPanel")]
    [JsonDerivedType(typeof(LayoutDocumentPaneGroupDto), "LayoutDocumentPaneGroup")]
    [JsonDerivedType(typeof(LayoutDocumentPaneDto), "LayoutDocumentPane")]
    [JsonDerivedType(typeof(LayoutAnchorablePaneGroupDto), "LayoutAnchorablePaneGroup")]
    [JsonDerivedType(typeof(LayoutAnchorablePaneDto), "LayoutAnchorablePane")]
    public abstract class LayoutPositionableGroupDto : LayoutElementDto
    {
        /// <summary>Gets or sets the dock width as a string (e.g. "1*", "200").</summary>
        public string? DockWidth { get; set; }

        /// <summary>Gets or sets the dock height as a string (e.g. "1*", "200").</summary>
        public string? DockHeight { get; set; }

        /// <summary>Gets or sets the dock minimum width.</summary>
        public double DockMinWidth { get; set; }

        /// <summary>Gets or sets the dock minimum height.</summary>
        public double DockMinHeight { get; set; }

        /// <summary>Gets or sets the floating width.</summary>
        public double FloatingWidth { get; set; }

        /// <summary>Gets or sets the floating height.</summary>
        public double FloatingHeight { get; set; }

        /// <summary>Gets or sets the floating left position.</summary>
        public double FloatingLeft { get; set; }

        /// <summary>Gets or sets the floating top position.</summary>
        public double FloatingTop { get; set; }

        /// <summary>Gets or sets a value indicating whether this group is maximized.</summary>
        public bool IsMaximized { get; set; }
    }

    /// <summary>
    /// DTO for a layout panel.
    /// </summary>
    public class LayoutPanelDto : LayoutPositionableGroupDto
    {
        /// <summary>Gets or sets the orientation as a string ("Horizontal" or "Vertical").</summary>
        public string? Orientation { get; set; }

        /// <summary>Gets or sets a value indicating whether docking is allowed.</summary>
        public bool CanDock { get; set; }

        /// <summary>Gets or sets the child elements.</summary>
        public List<LayoutPositionableGroupDto> Children { get; set; } = [];
    }

    /// <summary>
    /// DTO for a document pane group.
    /// </summary>
    public class LayoutDocumentPaneGroupDto : LayoutPositionableGroupDto
    {
        /// <summary>Gets or sets the orientation as a string ("Horizontal" or "Vertical").</summary>
        public string? Orientation { get; set; }

        /// <summary>Gets or sets the child document panes and pane groups.</summary>
        public List<LayoutPositionableGroupDto> Children { get; set; } = [];
    }

    /// <summary>
    /// DTO for a document pane.
    /// </summary>
    public class LayoutDocumentPaneDto : LayoutPositionableGroupDto
    {
        /// <summary>Gets or sets the pane identifier.</summary>
        public string? Id { get; set; }

        /// <summary>Gets or sets a value indicating whether to show the header.</summary>
        public bool ShowHeader { get; set; }

        /// <summary>Gets or sets the child content items.</summary>
        public List<LayoutContentDto> Children { get; set; } = [];
    }

    /// <summary>
    /// DTO for an anchorable pane group.
    /// </summary>
    public class LayoutAnchorablePaneGroupDto : LayoutPositionableGroupDto
    {
        /// <summary>Gets or sets the orientation as a string ("Horizontal" or "Vertical").</summary>
        public string? Orientation { get; set; }

        /// <summary>Gets or sets the child anchorable panes and pane groups.</summary>
        public List<LayoutPositionableGroupDto> Children { get; set; } = [];
    }

    /// <summary>
    /// DTO for an anchorable pane.
    /// </summary>
    public class LayoutAnchorablePaneDto : LayoutPositionableGroupDto
    {
        /// <summary>Gets or sets the pane identifier.</summary>
        public string? Id { get; set; }

        /// <summary>Gets or sets the pane name.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the child anchorables.</summary>
        public List<LayoutAnchorableDto> Children { get; set; } = [];
    }

    /// <summary>
    /// DTO for a layout anchor side (auto-hide side).
    /// </summary>
    public class LayoutAnchorSideDto : LayoutElementDto
    {
        /// <summary>Gets or sets the child anchor groups.</summary>
        public List<LayoutAnchorGroupDto> Children { get; set; } = [];
    }

    /// <summary>
    /// DTO for a layout anchor group.
    /// </summary>
    public class LayoutAnchorGroupDto : LayoutElementDto
    {
        /// <summary>Gets or sets the group identifier.</summary>
        public string? Id { get; set; }

        /// <summary>Gets or sets the previous container identifier.</summary>
        public string? PreviousContainerId { get; set; }

        /// <summary>Gets or sets the child anchorables.</summary>
        public List<LayoutAnchorableDto> Children { get; set; } = [];
    }

    /// <summary>
    /// Abstract DTO base for floating windows.
    /// </summary>
    [JsonPolymorphic]
    [JsonDerivedType(typeof(LayoutDocumentFloatingWindowDto), "LayoutDocumentFloatingWindow")]
    [JsonDerivedType(typeof(LayoutAnchorableFloatingWindowDto), "LayoutAnchorableFloatingWindow")]
    public abstract class LayoutFloatingWindowDto : LayoutElementDto
    {
    }

    /// <summary>
    /// DTO for a document floating window.
    /// </summary>
    public class LayoutDocumentFloatingWindowDto : LayoutFloatingWindowDto
    {
        /// <summary>Gets or sets the root panel.</summary>
        public LayoutDocumentPaneGroupDto? RootPanel { get; set; }
    }

    /// <summary>
    /// DTO for an anchorable floating window.
    /// </summary>
    public class LayoutAnchorableFloatingWindowDto : LayoutFloatingWindowDto
    {
        /// <summary>Gets or sets the root panel.</summary>
        public LayoutAnchorablePaneGroupDto? RootPanel { get; set; }
    }

    /// <summary>
    /// DTO for the layout root.
    /// </summary>
    public class LayoutRootDto : LayoutElementDto
    {
        /// <summary>Gets or sets the root panel.</summary>
        public LayoutPanelDto? RootPanel { get; set; }

        /// <summary>Gets or sets the top side.</summary>
        public LayoutAnchorSideDto? TopSide { get; set; }

        /// <summary>Gets or sets the right side.</summary>
        public LayoutAnchorSideDto? RightSide { get; set; }

        /// <summary>Gets or sets the left side.</summary>
        public LayoutAnchorSideDto? LeftSide { get; set; }

        /// <summary>Gets or sets the bottom side.</summary>
        public LayoutAnchorSideDto? BottomSide { get; set; }

        /// <summary>Gets or sets the floating windows.</summary>
        public List<LayoutFloatingWindowDto> FloatingWindows { get; set; } = [];

        /// <summary>Gets or sets the hidden anchorables.</summary>
        public List<LayoutAnchorableDto> Hidden { get; set; } = [];
    }
}
