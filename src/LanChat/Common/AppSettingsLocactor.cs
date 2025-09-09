/*
 * LanChat
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Markup;
using Argus.Memory;
using Mosaic.UI.Wpf.Common;

namespace LanChat.Common
{
    /// <summary>
    /// A markup extension that provides an instance of <see cref="AppSettings"/>.
    /// </summary>
    /// <remarks>
    /// <example>  
    /// <![CDATA[  
    /// <Window xmlns:vm="clr-namespace:Continuum.Common"  
    ///         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">  
    ///     <Window.Resources>  
    ///         <vm:AppSettingsLocator x:Key="AppSettings" />  
    ///     </Window.Resources>  
    ///     <Grid DataContext="{StaticResource AppSettings}">  
    ///         <!-- Your UI elements here -->  
    ///     </Grid>  
    /// </Window>  
    /// ]]>  
    /// </example>  
    /// </remarks>
    public class AppSettingsLocator : MarkupExtension
    {
        /// <summary>
        /// Returns an instance of <see cref="LocalSettings"/> from the service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>An instance of <see cref="LocalSettings"/>.</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return AppServices.GetRequiredService<AppSettings>();
        }
    }
}
