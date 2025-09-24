using System.Windows.Markup;

namespace Mosaic.UI.Wpf.Common
{
    /// <summary>
    /// A markup extension that provides an instance of a specified type that has been registered with
    /// the <see cref="AppServices"/> service provider.  If no type is found, null is returned.
    /// </summary>
    /// <remarks>
    /// <example>  
    /// <![CDATA[  
    /// <Window xmlns:vm="clr-namespace:Continuum.Common"
    ///         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    ///         DataContext="{StaticResource AppSettings}">  
    ///     <Window.Resources>
    ///         <common:AppServiceLocator Type="{x:Type vm:AppSettings}" x:Key="AppSettings" />
    ///     </Window.Resources>  
    ///     <Grid DataContext="{StaticResource AppSettings}">  
    ///         <!-- Your UI elements here -->  
    ///     </Grid>  
    /// </Window>  
    /// ]]>  
    /// </example>  
    /// </remarks>
    public class AppServiceLocator : MarkupExtension
    {
        /// <summary>
        /// The type of the object to locate in the <see cref="AppServices"/> container.
        /// </summary>
        public Type? Type { get; set; }

        /// <summary>
        /// Returns an instance of <see cref="LocalSettings"/> from the service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>An instance of <see cref="LocalSettings"/>.</returns>
        public override object? ProvideValue(IServiceProvider serviceProvider)
        {
            if (this.Type == null)
            {
                return null;
            }

            return AppServices.GetService(this.Type);
        }
    }
}