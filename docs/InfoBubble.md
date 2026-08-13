# InfoBubble

A content wrapper that overlays a count, status dot, or custom notification indicator on any element. The indicator can sit at a corner or the center of any edge, and its default colors follow the Mosaic theme.

## Usage

```xml
<mosaic:InfoBubble
    InfoBubbleContent="7"
    InfoBubbleMargin="-8"
    InfoBubblePlacementMode="TopRight">
    <Button Content="Inbox" />
</mosaic:InfoBubble>
```

Negative `InfoBubbleMargin` values move the indicator outside the content while reserving enough layout space to avoid clipping. Positive values move it inward.

## Counts and status dots

```xml
<!-- Displays 99+ when the bound count is greater than 99. -->
<mosaic:InfoBubble
    HideWhenZero="True"
    InfoBubbleContent="{Binding UnreadCount}"
    InfoBubbleMaxValue="99">
    <Image Source="mail.png" />
</mosaic:InfoBubble>

<!-- Content-free status indicator. -->
<mosaic:InfoBubble
    IsDot="True"
    InfoBubbleBackground="{DynamicResource {x:Static themes:MosaicTheme.SuccessBrush}}"
    InfoBubblePlacementMode="BottomLeft">
    <mosaic:Avatar />
</mosaic:InfoBubble>
```

`HideWhenZero` recognizes numeric zero values and the string `"0"`. `InfoBubbleMaxValue` only caps content that can be converted to an integer; other content is displayed unchanged.

## Properties

| Property | Default | Description |
|---|---|---|
| `InfoBubbleContent` | `null` | Indicator content. |
| `InfoBubbleContentTemplate` | `null` | Template for custom indicator content. |
| `InfoBubblePlacementMode` | `TopRight` | `TopLeft`, `Top`, `TopRight`, `Right`, `BottomRight`, `Bottom`, `BottomLeft`, or `Left`. |
| `InfoBubbleMargin` | `0` | Fine-tunes position; negative components create overhang. |
| `IsInfoBubbleVisible` | `true` | Explicit visibility switch for the indicator. |
| `IsDot` | `false` | Renders a small dot and suppresses indicator content. |
| `InfoBubbleMaxValue` | `0` | Highest integer shown directly; `0` disables capping. |
| `HideWhenZero` | `false` | Hides the indicator when its content represents zero. |
| `InfoBubbleBackground` / `InfoBubbleForeground` | theme brushes | Indicator colors. |
| `InfoBubbleBorderBrush` / `InfoBubbleBorderThickness` | transparent / `0` | Indicator border. |
| `InfoBubbleCornerRadius` | `8` | Indicator corner radius. |
| `InfoBubbleFontFamily` / `InfoBubbleFontSize` / `InfoBubbleFontWeight` | theme font / `10` / `Normal` | Indicator typography. |
| `InfoBubbleMinWidth` / `InfoBubbleMinHeight` | `16` / `16` | Minimum indicator dimensions. |
| `InfoBubblePadding` | `4,2` | Space around indicator content. |

`DisplayInfoBubbleContent` and `ComputedInfoBubbleVisibility` are read-only dependency properties containing the formatted content and final visibility decision. `InfoBubbleOverhangMargin` and `InfoBubbleIndicatorMargin` expose the layout values derived from `InfoBubbleMargin`.
