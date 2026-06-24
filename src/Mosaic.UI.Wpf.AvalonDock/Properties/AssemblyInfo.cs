using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
//[assembly: CLSCompliant(true)]

// In order to begin building localizable applications, set 
// <UICulture>CultureYouAreCodingWith</UICulture> in your .csproj file
// inside a <PropertyGroup>.  For example, if you are using US english
// in your source files, set the <UICulture> to en-US.  Then uncomment
// the NeutralResourceLanguage attribute below.  Update the "en-US" in
// the line below to match the UICulture setting in the project file.

// [assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, // where theme specific resource dictionaries are located
                                     // (used if a resource is not found in the page, 
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly) // where the generic resource dictionary is located
                                               // (used if a resource is not found in the page, 
                                               // app, or any theme specific resource dictionaries)
]

[assembly: XmlnsPrefix("https://github.com/blakepell/MosaicUIWpf", "avalondock")]
[assembly: XmlnsDefinition("https://github.com/blakepell/MosaicUIWpf", "Mosaic.UI.Wpf.AvalonDock")]
[assembly: XmlnsDefinition("https://github.com/blakepell/MosaicUIWpf", "Mosaic.UI.Wpf.AvalonDock.Controls")]
[assembly: XmlnsDefinition("https://github.com/blakepell/MosaicUIWpf", "Mosaic.UI.Wpf.AvalonDock.Converters")]
[assembly: XmlnsDefinition("https://github.com/blakepell/MosaicUIWpf", "Mosaic.UI.Wpf.AvalonDock.Layout")]
[assembly: XmlnsDefinition("https://github.com/blakepell/MosaicUIWpf", "Mosaic.UI.Wpf.AvalonDock.Themes")]
[assembly: XmlnsPrefix("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "avalondock")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Mosaic.UI.Wpf.AvalonDock")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Mosaic.UI.Wpf.AvalonDock.Controls")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Mosaic.UI.Wpf.AvalonDock.Converters")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Mosaic.UI.Wpf.AvalonDock.Layout")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Mosaic.UI.Wpf.AvalonDock.Themes")]
