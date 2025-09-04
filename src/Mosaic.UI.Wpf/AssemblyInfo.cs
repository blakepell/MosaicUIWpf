/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Markup;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]

[assembly: XmlnsPrefix("http://schemas.apexgate.net/wpf/mosaic-ui", "mosaic")]
[assembly: XmlnsDefinition("http://schemas.apexgate.net/wpf/mosaic-ui", "Mosaic.UI.Wpf.Controls")]
[assembly: XmlnsDefinition("http://schemas.apexgate.net/wpf/mosaic-ui", "Mosaic.UI.Wpf.Converters")]
[assembly: XmlnsDefinition("http://schemas.apexgate.net/wpf/mosaic-ui", "Mosaic.UI.Wpf.Behaviors")]
[assembly: XmlnsDefinition("http://schemas.apexgate.net/wpf/mosaic-ui", "Mosaic.UI.Wpf.Themes")]
