/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Themes
{
    /// <summary>
    /// Provides strongly typed resource keys for Mosaic's theme-independent asset palette.
    /// </summary>
    /// <remarks>
    /// Each color family contains background color and brush resources from 100 (lightest)
    /// through 900 (darkest), with the supplied base color at 500.
    /// </remarks>
    /// <example>
    /// <code language="xaml">
    /// Background="{DynamicResource {x:Static themes:AssetResourceKeys.Blue500BackgroundBrushKey}}"
    /// </code>
    /// </example>
    public static class AssetResourceKeys
    {
        // Blue

        /// <summary>
        /// Gets the resource key for the blue 100 background color.
        /// </summary>
        /// <value>The component resource key for the blue 100 background color.</value>
        public static ComponentResourceKey Blue100BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue100BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the blue 100 background brush.
        /// </summary>
        /// <value>The component resource key for the blue 100 background brush.</value>
        public static ComponentResourceKey Blue100BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue100BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the blue 200 background color.
        /// </summary>
        /// <value>The component resource key for the blue 200 background color.</value>
        public static ComponentResourceKey Blue200BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue200BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the blue 200 background brush.
        /// </summary>
        /// <value>The component resource key for the blue 200 background brush.</value>
        public static ComponentResourceKey Blue200BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue200BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the blue 300 background color.
        /// </summary>
        /// <value>The component resource key for the blue 300 background color.</value>
        public static ComponentResourceKey Blue300BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue300BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the blue 300 background brush.
        /// </summary>
        /// <value>The component resource key for the blue 300 background brush.</value>
        public static ComponentResourceKey Blue300BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue300BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the blue 400 background color.
        /// </summary>
        /// <value>The component resource key for the blue 400 background color.</value>
        public static ComponentResourceKey Blue400BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue400BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the blue 400 background brush.
        /// </summary>
        /// <value>The component resource key for the blue 400 background brush.</value>
        public static ComponentResourceKey Blue400BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue400BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the blue 500 background color.
        /// </summary>
        /// <value>The component resource key for the blue 500 background color.</value>
        public static ComponentResourceKey Blue500BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue500BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the blue 500 background brush.
        /// </summary>
        /// <value>The component resource key for the blue 500 background brush.</value>
        public static ComponentResourceKey Blue500BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue500BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the blue 600 background color.
        /// </summary>
        /// <value>The component resource key for the blue 600 background color.</value>
        public static ComponentResourceKey Blue600BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue600BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the blue 600 background brush.
        /// </summary>
        /// <value>The component resource key for the blue 600 background brush.</value>
        public static ComponentResourceKey Blue600BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue600BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the blue 700 background color.
        /// </summary>
        /// <value>The component resource key for the blue 700 background color.</value>
        public static ComponentResourceKey Blue700BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue700BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the blue 700 background brush.
        /// </summary>
        /// <value>The component resource key for the blue 700 background brush.</value>
        public static ComponentResourceKey Blue700BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue700BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the blue 800 background color.
        /// </summary>
        /// <value>The component resource key for the blue 800 background color.</value>
        public static ComponentResourceKey Blue800BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue800BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the blue 800 background brush.
        /// </summary>
        /// <value>The component resource key for the blue 800 background brush.</value>
        public static ComponentResourceKey Blue800BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue800BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the blue 900 background color.
        /// </summary>
        /// <value>The component resource key for the blue 900 background color.</value>
        public static ComponentResourceKey Blue900BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue900BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the blue 900 background brush.
        /// </summary>
        /// <value>The component resource key for the blue 900 background brush.</value>
        public static ComponentResourceKey Blue900BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Blue900BackgroundBrushKey));

        // Indigo

        /// <summary>
        /// Gets the resource key for the indigo 100 background color.
        /// </summary>
        /// <value>The component resource key for the indigo 100 background color.</value>
        public static ComponentResourceKey Indigo100BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo100BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the indigo 100 background brush.
        /// </summary>
        /// <value>The component resource key for the indigo 100 background brush.</value>
        public static ComponentResourceKey Indigo100BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo100BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the indigo 200 background color.
        /// </summary>
        /// <value>The component resource key for the indigo 200 background color.</value>
        public static ComponentResourceKey Indigo200BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo200BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the indigo 200 background brush.
        /// </summary>
        /// <value>The component resource key for the indigo 200 background brush.</value>
        public static ComponentResourceKey Indigo200BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo200BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the indigo 300 background color.
        /// </summary>
        /// <value>The component resource key for the indigo 300 background color.</value>
        public static ComponentResourceKey Indigo300BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo300BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the indigo 300 background brush.
        /// </summary>
        /// <value>The component resource key for the indigo 300 background brush.</value>
        public static ComponentResourceKey Indigo300BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo300BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the indigo 400 background color.
        /// </summary>
        /// <value>The component resource key for the indigo 400 background color.</value>
        public static ComponentResourceKey Indigo400BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo400BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the indigo 400 background brush.
        /// </summary>
        /// <value>The component resource key for the indigo 400 background brush.</value>
        public static ComponentResourceKey Indigo400BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo400BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the indigo 500 background color.
        /// </summary>
        /// <value>The component resource key for the indigo 500 background color.</value>
        public static ComponentResourceKey Indigo500BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo500BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the indigo 500 background brush.
        /// </summary>
        /// <value>The component resource key for the indigo 500 background brush.</value>
        public static ComponentResourceKey Indigo500BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo500BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the indigo 600 background color.
        /// </summary>
        /// <value>The component resource key for the indigo 600 background color.</value>
        public static ComponentResourceKey Indigo600BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo600BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the indigo 600 background brush.
        /// </summary>
        /// <value>The component resource key for the indigo 600 background brush.</value>
        public static ComponentResourceKey Indigo600BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo600BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the indigo 700 background color.
        /// </summary>
        /// <value>The component resource key for the indigo 700 background color.</value>
        public static ComponentResourceKey Indigo700BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo700BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the indigo 700 background brush.
        /// </summary>
        /// <value>The component resource key for the indigo 700 background brush.</value>
        public static ComponentResourceKey Indigo700BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo700BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the indigo 800 background color.
        /// </summary>
        /// <value>The component resource key for the indigo 800 background color.</value>
        public static ComponentResourceKey Indigo800BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo800BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the indigo 800 background brush.
        /// </summary>
        /// <value>The component resource key for the indigo 800 background brush.</value>
        public static ComponentResourceKey Indigo800BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo800BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the indigo 900 background color.
        /// </summary>
        /// <value>The component resource key for the indigo 900 background color.</value>
        public static ComponentResourceKey Indigo900BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo900BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the indigo 900 background brush.
        /// </summary>
        /// <value>The component resource key for the indigo 900 background brush.</value>
        public static ComponentResourceKey Indigo900BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Indigo900BackgroundBrushKey));

        // Purple

        /// <summary>
        /// Gets the resource key for the purple 100 background color.
        /// </summary>
        /// <value>The component resource key for the purple 100 background color.</value>
        public static ComponentResourceKey Purple100BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple100BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the purple 100 background brush.
        /// </summary>
        /// <value>The component resource key for the purple 100 background brush.</value>
        public static ComponentResourceKey Purple100BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple100BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the purple 200 background color.
        /// </summary>
        /// <value>The component resource key for the purple 200 background color.</value>
        public static ComponentResourceKey Purple200BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple200BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the purple 200 background brush.
        /// </summary>
        /// <value>The component resource key for the purple 200 background brush.</value>
        public static ComponentResourceKey Purple200BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple200BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the purple 300 background color.
        /// </summary>
        /// <value>The component resource key for the purple 300 background color.</value>
        public static ComponentResourceKey Purple300BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple300BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the purple 300 background brush.
        /// </summary>
        /// <value>The component resource key for the purple 300 background brush.</value>
        public static ComponentResourceKey Purple300BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple300BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the purple 400 background color.
        /// </summary>
        /// <value>The component resource key for the purple 400 background color.</value>
        public static ComponentResourceKey Purple400BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple400BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the purple 400 background brush.
        /// </summary>
        /// <value>The component resource key for the purple 400 background brush.</value>
        public static ComponentResourceKey Purple400BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple400BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the purple 500 background color.
        /// </summary>
        /// <value>The component resource key for the purple 500 background color.</value>
        public static ComponentResourceKey Purple500BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple500BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the purple 500 background brush.
        /// </summary>
        /// <value>The component resource key for the purple 500 background brush.</value>
        public static ComponentResourceKey Purple500BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple500BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the purple 600 background color.
        /// </summary>
        /// <value>The component resource key for the purple 600 background color.</value>
        public static ComponentResourceKey Purple600BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple600BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the purple 600 background brush.
        /// </summary>
        /// <value>The component resource key for the purple 600 background brush.</value>
        public static ComponentResourceKey Purple600BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple600BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the purple 700 background color.
        /// </summary>
        /// <value>The component resource key for the purple 700 background color.</value>
        public static ComponentResourceKey Purple700BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple700BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the purple 700 background brush.
        /// </summary>
        /// <value>The component resource key for the purple 700 background brush.</value>
        public static ComponentResourceKey Purple700BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple700BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the purple 800 background color.
        /// </summary>
        /// <value>The component resource key for the purple 800 background color.</value>
        public static ComponentResourceKey Purple800BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple800BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the purple 800 background brush.
        /// </summary>
        /// <value>The component resource key for the purple 800 background brush.</value>
        public static ComponentResourceKey Purple800BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple800BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the purple 900 background color.
        /// </summary>
        /// <value>The component resource key for the purple 900 background color.</value>
        public static ComponentResourceKey Purple900BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple900BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the purple 900 background brush.
        /// </summary>
        /// <value>The component resource key for the purple 900 background brush.</value>
        public static ComponentResourceKey Purple900BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Purple900BackgroundBrushKey));

        // Pink

        /// <summary>
        /// Gets the resource key for the pink 100 background color.
        /// </summary>
        /// <value>The component resource key for the pink 100 background color.</value>
        public static ComponentResourceKey Pink100BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink100BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the pink 100 background brush.
        /// </summary>
        /// <value>The component resource key for the pink 100 background brush.</value>
        public static ComponentResourceKey Pink100BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink100BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the pink 200 background color.
        /// </summary>
        /// <value>The component resource key for the pink 200 background color.</value>
        public static ComponentResourceKey Pink200BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink200BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the pink 200 background brush.
        /// </summary>
        /// <value>The component resource key for the pink 200 background brush.</value>
        public static ComponentResourceKey Pink200BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink200BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the pink 300 background color.
        /// </summary>
        /// <value>The component resource key for the pink 300 background color.</value>
        public static ComponentResourceKey Pink300BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink300BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the pink 300 background brush.
        /// </summary>
        /// <value>The component resource key for the pink 300 background brush.</value>
        public static ComponentResourceKey Pink300BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink300BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the pink 400 background color.
        /// </summary>
        /// <value>The component resource key for the pink 400 background color.</value>
        public static ComponentResourceKey Pink400BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink400BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the pink 400 background brush.
        /// </summary>
        /// <value>The component resource key for the pink 400 background brush.</value>
        public static ComponentResourceKey Pink400BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink400BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the pink 500 background color.
        /// </summary>
        /// <value>The component resource key for the pink 500 background color.</value>
        public static ComponentResourceKey Pink500BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink500BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the pink 500 background brush.
        /// </summary>
        /// <value>The component resource key for the pink 500 background brush.</value>
        public static ComponentResourceKey Pink500BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink500BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the pink 600 background color.
        /// </summary>
        /// <value>The component resource key for the pink 600 background color.</value>
        public static ComponentResourceKey Pink600BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink600BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the pink 600 background brush.
        /// </summary>
        /// <value>The component resource key for the pink 600 background brush.</value>
        public static ComponentResourceKey Pink600BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink600BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the pink 700 background color.
        /// </summary>
        /// <value>The component resource key for the pink 700 background color.</value>
        public static ComponentResourceKey Pink700BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink700BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the pink 700 background brush.
        /// </summary>
        /// <value>The component resource key for the pink 700 background brush.</value>
        public static ComponentResourceKey Pink700BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink700BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the pink 800 background color.
        /// </summary>
        /// <value>The component resource key for the pink 800 background color.</value>
        public static ComponentResourceKey Pink800BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink800BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the pink 800 background brush.
        /// </summary>
        /// <value>The component resource key for the pink 800 background brush.</value>
        public static ComponentResourceKey Pink800BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink800BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the pink 900 background color.
        /// </summary>
        /// <value>The component resource key for the pink 900 background color.</value>
        public static ComponentResourceKey Pink900BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink900BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the pink 900 background brush.
        /// </summary>
        /// <value>The component resource key for the pink 900 background brush.</value>
        public static ComponentResourceKey Pink900BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Pink900BackgroundBrushKey));

        // Red

        /// <summary>
        /// Gets the resource key for the red 100 background color.
        /// </summary>
        /// <value>The component resource key for the red 100 background color.</value>
        public static ComponentResourceKey Red100BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Red100BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the red 100 background brush.
        /// </summary>
        /// <value>The component resource key for the red 100 background brush.</value>
        public static ComponentResourceKey Red100BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Red100BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the red 200 background color.
        /// </summary>
        /// <value>The component resource key for the red 200 background color.</value>
        public static ComponentResourceKey Red200BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Red200BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the red 200 background brush.
        /// </summary>
        /// <value>The component resource key for the red 200 background brush.</value>
        public static ComponentResourceKey Red200BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Red200BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the red 300 background color.
        /// </summary>
        /// <value>The component resource key for the red 300 background color.</value>
        public static ComponentResourceKey Red300BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Red300BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the red 300 background brush.
        /// </summary>
        /// <value>The component resource key for the red 300 background brush.</value>
        public static ComponentResourceKey Red300BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Red300BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the red 400 background color.
        /// </summary>
        /// <value>The component resource key for the red 400 background color.</value>
        public static ComponentResourceKey Red400BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Red400BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the red 400 background brush.
        /// </summary>
        /// <value>The component resource key for the red 400 background brush.</value>
        public static ComponentResourceKey Red400BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Red400BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the red 500 background color.
        /// </summary>
        /// <value>The component resource key for the red 500 background color.</value>
        public static ComponentResourceKey Red500BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Red500BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the red 500 background brush.
        /// </summary>
        /// <value>The component resource key for the red 500 background brush.</value>
        public static ComponentResourceKey Red500BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Red500BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the red 600 background color.
        /// </summary>
        /// <value>The component resource key for the red 600 background color.</value>
        public static ComponentResourceKey Red600BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Red600BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the red 600 background brush.
        /// </summary>
        /// <value>The component resource key for the red 600 background brush.</value>
        public static ComponentResourceKey Red600BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Red600BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the red 700 background color.
        /// </summary>
        /// <value>The component resource key for the red 700 background color.</value>
        public static ComponentResourceKey Red700BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Red700BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the red 700 background brush.
        /// </summary>
        /// <value>The component resource key for the red 700 background brush.</value>
        public static ComponentResourceKey Red700BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Red700BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the red 800 background color.
        /// </summary>
        /// <value>The component resource key for the red 800 background color.</value>
        public static ComponentResourceKey Red800BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Red800BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the red 800 background brush.
        /// </summary>
        /// <value>The component resource key for the red 800 background brush.</value>
        public static ComponentResourceKey Red800BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Red800BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the red 900 background color.
        /// </summary>
        /// <value>The component resource key for the red 900 background color.</value>
        public static ComponentResourceKey Red900BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Red900BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the red 900 background brush.
        /// </summary>
        /// <value>The component resource key for the red 900 background brush.</value>
        public static ComponentResourceKey Red900BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Red900BackgroundBrushKey));

        // Orange

        /// <summary>
        /// Gets the resource key for the orange 100 background color.
        /// </summary>
        /// <value>The component resource key for the orange 100 background color.</value>
        public static ComponentResourceKey Orange100BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange100BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the orange 100 background brush.
        /// </summary>
        /// <value>The component resource key for the orange 100 background brush.</value>
        public static ComponentResourceKey Orange100BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange100BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the orange 200 background color.
        /// </summary>
        /// <value>The component resource key for the orange 200 background color.</value>
        public static ComponentResourceKey Orange200BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange200BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the orange 200 background brush.
        /// </summary>
        /// <value>The component resource key for the orange 200 background brush.</value>
        public static ComponentResourceKey Orange200BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange200BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the orange 300 background color.
        /// </summary>
        /// <value>The component resource key for the orange 300 background color.</value>
        public static ComponentResourceKey Orange300BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange300BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the orange 300 background brush.
        /// </summary>
        /// <value>The component resource key for the orange 300 background brush.</value>
        public static ComponentResourceKey Orange300BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange300BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the orange 400 background color.
        /// </summary>
        /// <value>The component resource key for the orange 400 background color.</value>
        public static ComponentResourceKey Orange400BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange400BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the orange 400 background brush.
        /// </summary>
        /// <value>The component resource key for the orange 400 background brush.</value>
        public static ComponentResourceKey Orange400BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange400BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the orange 500 background color.
        /// </summary>
        /// <value>The component resource key for the orange 500 background color.</value>
        public static ComponentResourceKey Orange500BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange500BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the orange 500 background brush.
        /// </summary>
        /// <value>The component resource key for the orange 500 background brush.</value>
        public static ComponentResourceKey Orange500BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange500BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the orange 600 background color.
        /// </summary>
        /// <value>The component resource key for the orange 600 background color.</value>
        public static ComponentResourceKey Orange600BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange600BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the orange 600 background brush.
        /// </summary>
        /// <value>The component resource key for the orange 600 background brush.</value>
        public static ComponentResourceKey Orange600BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange600BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the orange 700 background color.
        /// </summary>
        /// <value>The component resource key for the orange 700 background color.</value>
        public static ComponentResourceKey Orange700BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange700BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the orange 700 background brush.
        /// </summary>
        /// <value>The component resource key for the orange 700 background brush.</value>
        public static ComponentResourceKey Orange700BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange700BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the orange 800 background color.
        /// </summary>
        /// <value>The component resource key for the orange 800 background color.</value>
        public static ComponentResourceKey Orange800BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange800BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the orange 800 background brush.
        /// </summary>
        /// <value>The component resource key for the orange 800 background brush.</value>
        public static ComponentResourceKey Orange800BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange800BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the orange 900 background color.
        /// </summary>
        /// <value>The component resource key for the orange 900 background color.</value>
        public static ComponentResourceKey Orange900BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange900BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the orange 900 background brush.
        /// </summary>
        /// <value>The component resource key for the orange 900 background brush.</value>
        public static ComponentResourceKey Orange900BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Orange900BackgroundBrushKey));

        // Yellow

        /// <summary>
        /// Gets the resource key for the yellow 100 background color.
        /// </summary>
        /// <value>The component resource key for the yellow 100 background color.</value>
        public static ComponentResourceKey Yellow100BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow100BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the yellow 100 background brush.
        /// </summary>
        /// <value>The component resource key for the yellow 100 background brush.</value>
        public static ComponentResourceKey Yellow100BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow100BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the yellow 200 background color.
        /// </summary>
        /// <value>The component resource key for the yellow 200 background color.</value>
        public static ComponentResourceKey Yellow200BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow200BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the yellow 200 background brush.
        /// </summary>
        /// <value>The component resource key for the yellow 200 background brush.</value>
        public static ComponentResourceKey Yellow200BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow200BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the yellow 300 background color.
        /// </summary>
        /// <value>The component resource key for the yellow 300 background color.</value>
        public static ComponentResourceKey Yellow300BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow300BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the yellow 300 background brush.
        /// </summary>
        /// <value>The component resource key for the yellow 300 background brush.</value>
        public static ComponentResourceKey Yellow300BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow300BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the yellow 400 background color.
        /// </summary>
        /// <value>The component resource key for the yellow 400 background color.</value>
        public static ComponentResourceKey Yellow400BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow400BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the yellow 400 background brush.
        /// </summary>
        /// <value>The component resource key for the yellow 400 background brush.</value>
        public static ComponentResourceKey Yellow400BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow400BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the yellow 500 background color.
        /// </summary>
        /// <value>The component resource key for the yellow 500 background color.</value>
        public static ComponentResourceKey Yellow500BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow500BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the yellow 500 background brush.
        /// </summary>
        /// <value>The component resource key for the yellow 500 background brush.</value>
        public static ComponentResourceKey Yellow500BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow500BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the yellow 600 background color.
        /// </summary>
        /// <value>The component resource key for the yellow 600 background color.</value>
        public static ComponentResourceKey Yellow600BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow600BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the yellow 600 background brush.
        /// </summary>
        /// <value>The component resource key for the yellow 600 background brush.</value>
        public static ComponentResourceKey Yellow600BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow600BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the yellow 700 background color.
        /// </summary>
        /// <value>The component resource key for the yellow 700 background color.</value>
        public static ComponentResourceKey Yellow700BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow700BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the yellow 700 background brush.
        /// </summary>
        /// <value>The component resource key for the yellow 700 background brush.</value>
        public static ComponentResourceKey Yellow700BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow700BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the yellow 800 background color.
        /// </summary>
        /// <value>The component resource key for the yellow 800 background color.</value>
        public static ComponentResourceKey Yellow800BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow800BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the yellow 800 background brush.
        /// </summary>
        /// <value>The component resource key for the yellow 800 background brush.</value>
        public static ComponentResourceKey Yellow800BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow800BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the yellow 900 background color.
        /// </summary>
        /// <value>The component resource key for the yellow 900 background color.</value>
        public static ComponentResourceKey Yellow900BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow900BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the yellow 900 background brush.
        /// </summary>
        /// <value>The component resource key for the yellow 900 background brush.</value>
        public static ComponentResourceKey Yellow900BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Yellow900BackgroundBrushKey));

        // Green

        /// <summary>
        /// Gets the resource key for the green 100 background color.
        /// </summary>
        /// <value>The component resource key for the green 100 background color.</value>
        public static ComponentResourceKey Green100BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Green100BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the green 100 background brush.
        /// </summary>
        /// <value>The component resource key for the green 100 background brush.</value>
        public static ComponentResourceKey Green100BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Green100BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the green 200 background color.
        /// </summary>
        /// <value>The component resource key for the green 200 background color.</value>
        public static ComponentResourceKey Green200BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Green200BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the green 200 background brush.
        /// </summary>
        /// <value>The component resource key for the green 200 background brush.</value>
        public static ComponentResourceKey Green200BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Green200BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the green 300 background color.
        /// </summary>
        /// <value>The component resource key for the green 300 background color.</value>
        public static ComponentResourceKey Green300BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Green300BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the green 300 background brush.
        /// </summary>
        /// <value>The component resource key for the green 300 background brush.</value>
        public static ComponentResourceKey Green300BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Green300BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the green 400 background color.
        /// </summary>
        /// <value>The component resource key for the green 400 background color.</value>
        public static ComponentResourceKey Green400BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Green400BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the green 400 background brush.
        /// </summary>
        /// <value>The component resource key for the green 400 background brush.</value>
        public static ComponentResourceKey Green400BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Green400BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the green 500 background color.
        /// </summary>
        /// <value>The component resource key for the green 500 background color.</value>
        public static ComponentResourceKey Green500BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Green500BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the green 500 background brush.
        /// </summary>
        /// <value>The component resource key for the green 500 background brush.</value>
        public static ComponentResourceKey Green500BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Green500BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the green 600 background color.
        /// </summary>
        /// <value>The component resource key for the green 600 background color.</value>
        public static ComponentResourceKey Green600BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Green600BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the green 600 background brush.
        /// </summary>
        /// <value>The component resource key for the green 600 background brush.</value>
        public static ComponentResourceKey Green600BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Green600BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the green 700 background color.
        /// </summary>
        /// <value>The component resource key for the green 700 background color.</value>
        public static ComponentResourceKey Green700BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Green700BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the green 700 background brush.
        /// </summary>
        /// <value>The component resource key for the green 700 background brush.</value>
        public static ComponentResourceKey Green700BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Green700BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the green 800 background color.
        /// </summary>
        /// <value>The component resource key for the green 800 background color.</value>
        public static ComponentResourceKey Green800BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Green800BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the green 800 background brush.
        /// </summary>
        /// <value>The component resource key for the green 800 background brush.</value>
        public static ComponentResourceKey Green800BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Green800BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the green 900 background color.
        /// </summary>
        /// <value>The component resource key for the green 900 background color.</value>
        public static ComponentResourceKey Green900BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Green900BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the green 900 background brush.
        /// </summary>
        /// <value>The component resource key for the green 900 background brush.</value>
        public static ComponentResourceKey Green900BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Green900BackgroundBrushKey));

        // Teal

        /// <summary>
        /// Gets the resource key for the teal 100 background color.
        /// </summary>
        /// <value>The component resource key for the teal 100 background color.</value>
        public static ComponentResourceKey Teal100BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal100BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the teal 100 background brush.
        /// </summary>
        /// <value>The component resource key for the teal 100 background brush.</value>
        public static ComponentResourceKey Teal100BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal100BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the teal 200 background color.
        /// </summary>
        /// <value>The component resource key for the teal 200 background color.</value>
        public static ComponentResourceKey Teal200BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal200BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the teal 200 background brush.
        /// </summary>
        /// <value>The component resource key for the teal 200 background brush.</value>
        public static ComponentResourceKey Teal200BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal200BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the teal 300 background color.
        /// </summary>
        /// <value>The component resource key for the teal 300 background color.</value>
        public static ComponentResourceKey Teal300BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal300BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the teal 300 background brush.
        /// </summary>
        /// <value>The component resource key for the teal 300 background brush.</value>
        public static ComponentResourceKey Teal300BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal300BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the teal 400 background color.
        /// </summary>
        /// <value>The component resource key for the teal 400 background color.</value>
        public static ComponentResourceKey Teal400BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal400BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the teal 400 background brush.
        /// </summary>
        /// <value>The component resource key for the teal 400 background brush.</value>
        public static ComponentResourceKey Teal400BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal400BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the teal 500 background color.
        /// </summary>
        /// <value>The component resource key for the teal 500 background color.</value>
        public static ComponentResourceKey Teal500BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal500BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the teal 500 background brush.
        /// </summary>
        /// <value>The component resource key for the teal 500 background brush.</value>
        public static ComponentResourceKey Teal500BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal500BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the teal 600 background color.
        /// </summary>
        /// <value>The component resource key for the teal 600 background color.</value>
        public static ComponentResourceKey Teal600BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal600BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the teal 600 background brush.
        /// </summary>
        /// <value>The component resource key for the teal 600 background brush.</value>
        public static ComponentResourceKey Teal600BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal600BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the teal 700 background color.
        /// </summary>
        /// <value>The component resource key for the teal 700 background color.</value>
        public static ComponentResourceKey Teal700BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal700BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the teal 700 background brush.
        /// </summary>
        /// <value>The component resource key for the teal 700 background brush.</value>
        public static ComponentResourceKey Teal700BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal700BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the teal 800 background color.
        /// </summary>
        /// <value>The component resource key for the teal 800 background color.</value>
        public static ComponentResourceKey Teal800BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal800BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the teal 800 background brush.
        /// </summary>
        /// <value>The component resource key for the teal 800 background brush.</value>
        public static ComponentResourceKey Teal800BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal800BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the teal 900 background color.
        /// </summary>
        /// <value>The component resource key for the teal 900 background color.</value>
        public static ComponentResourceKey Teal900BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal900BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the teal 900 background brush.
        /// </summary>
        /// <value>The component resource key for the teal 900 background brush.</value>
        public static ComponentResourceKey Teal900BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Teal900BackgroundBrushKey));

        // Cyan

        /// <summary>
        /// Gets the resource key for the cyan 100 background color.
        /// </summary>
        /// <value>The component resource key for the cyan 100 background color.</value>
        public static ComponentResourceKey Cyan100BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan100BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the cyan 100 background brush.
        /// </summary>
        /// <value>The component resource key for the cyan 100 background brush.</value>
        public static ComponentResourceKey Cyan100BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan100BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the cyan 200 background color.
        /// </summary>
        /// <value>The component resource key for the cyan 200 background color.</value>
        public static ComponentResourceKey Cyan200BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan200BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the cyan 200 background brush.
        /// </summary>
        /// <value>The component resource key for the cyan 200 background brush.</value>
        public static ComponentResourceKey Cyan200BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan200BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the cyan 300 background color.
        /// </summary>
        /// <value>The component resource key for the cyan 300 background color.</value>
        public static ComponentResourceKey Cyan300BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan300BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the cyan 300 background brush.
        /// </summary>
        /// <value>The component resource key for the cyan 300 background brush.</value>
        public static ComponentResourceKey Cyan300BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan300BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the cyan 400 background color.
        /// </summary>
        /// <value>The component resource key for the cyan 400 background color.</value>
        public static ComponentResourceKey Cyan400BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan400BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the cyan 400 background brush.
        /// </summary>
        /// <value>The component resource key for the cyan 400 background brush.</value>
        public static ComponentResourceKey Cyan400BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan400BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the cyan 500 background color.
        /// </summary>
        /// <value>The component resource key for the cyan 500 background color.</value>
        public static ComponentResourceKey Cyan500BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan500BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the cyan 500 background brush.
        /// </summary>
        /// <value>The component resource key for the cyan 500 background brush.</value>
        public static ComponentResourceKey Cyan500BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan500BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the cyan 600 background color.
        /// </summary>
        /// <value>The component resource key for the cyan 600 background color.</value>
        public static ComponentResourceKey Cyan600BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan600BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the cyan 600 background brush.
        /// </summary>
        /// <value>The component resource key for the cyan 600 background brush.</value>
        public static ComponentResourceKey Cyan600BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan600BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the cyan 700 background color.
        /// </summary>
        /// <value>The component resource key for the cyan 700 background color.</value>
        public static ComponentResourceKey Cyan700BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan700BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the cyan 700 background brush.
        /// </summary>
        /// <value>The component resource key for the cyan 700 background brush.</value>
        public static ComponentResourceKey Cyan700BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan700BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the cyan 800 background color.
        /// </summary>
        /// <value>The component resource key for the cyan 800 background color.</value>
        public static ComponentResourceKey Cyan800BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan800BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the cyan 800 background brush.
        /// </summary>
        /// <value>The component resource key for the cyan 800 background brush.</value>
        public static ComponentResourceKey Cyan800BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan800BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the cyan 900 background color.
        /// </summary>
        /// <value>The component resource key for the cyan 900 background color.</value>
        public static ComponentResourceKey Cyan900BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan900BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the cyan 900 background brush.
        /// </summary>
        /// <value>The component resource key for the cyan 900 background brush.</value>
        public static ComponentResourceKey Cyan900BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Cyan900BackgroundBrushKey));

        // Gray

        /// <summary>
        /// Gets the resource key for the gray 100 background color.
        /// </summary>
        /// <value>The component resource key for the gray 100 background color.</value>
        public static ComponentResourceKey Gray100BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray100BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the gray 100 background brush.
        /// </summary>
        /// <value>The component resource key for the gray 100 background brush.</value>
        public static ComponentResourceKey Gray100BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray100BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the gray 200 background color.
        /// </summary>
        /// <value>The component resource key for the gray 200 background color.</value>
        public static ComponentResourceKey Gray200BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray200BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the gray 200 background brush.
        /// </summary>
        /// <value>The component resource key for the gray 200 background brush.</value>
        public static ComponentResourceKey Gray200BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray200BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the gray 300 background color.
        /// </summary>
        /// <value>The component resource key for the gray 300 background color.</value>
        public static ComponentResourceKey Gray300BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray300BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the gray 300 background brush.
        /// </summary>
        /// <value>The component resource key for the gray 300 background brush.</value>
        public static ComponentResourceKey Gray300BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray300BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the gray 400 background color.
        /// </summary>
        /// <value>The component resource key for the gray 400 background color.</value>
        public static ComponentResourceKey Gray400BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray400BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the gray 400 background brush.
        /// </summary>
        /// <value>The component resource key for the gray 400 background brush.</value>
        public static ComponentResourceKey Gray400BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray400BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the gray 500 background color.
        /// </summary>
        /// <value>The component resource key for the gray 500 background color.</value>
        public static ComponentResourceKey Gray500BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray500BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the gray 500 background brush.
        /// </summary>
        /// <value>The component resource key for the gray 500 background brush.</value>
        public static ComponentResourceKey Gray500BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray500BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the gray 600 background color.
        /// </summary>
        /// <value>The component resource key for the gray 600 background color.</value>
        public static ComponentResourceKey Gray600BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray600BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the gray 600 background brush.
        /// </summary>
        /// <value>The component resource key for the gray 600 background brush.</value>
        public static ComponentResourceKey Gray600BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray600BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the gray 700 background color.
        /// </summary>
        /// <value>The component resource key for the gray 700 background color.</value>
        public static ComponentResourceKey Gray700BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray700BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the gray 700 background brush.
        /// </summary>
        /// <value>The component resource key for the gray 700 background brush.</value>
        public static ComponentResourceKey Gray700BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray700BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the gray 800 background color.
        /// </summary>
        /// <value>The component resource key for the gray 800 background color.</value>
        public static ComponentResourceKey Gray800BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray800BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the gray 800 background brush.
        /// </summary>
        /// <value>The component resource key for the gray 800 background brush.</value>
        public static ComponentResourceKey Gray800BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray800BackgroundBrushKey));

        /// <summary>
        /// Gets the resource key for the gray 900 background color.
        /// </summary>
        /// <value>The component resource key for the gray 900 background color.</value>
        public static ComponentResourceKey Gray900BackgroundColorKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray900BackgroundColorKey));

        /// <summary>
        /// Gets the resource key for the gray 900 background brush.
        /// </summary>
        /// <value>The component resource key for the gray 900 background brush.</value>
        public static ComponentResourceKey Gray900BackgroundBrushKey { get; } = new(typeof(AssetResourceKeys), nameof(Gray900BackgroundBrushKey));
    }
}
