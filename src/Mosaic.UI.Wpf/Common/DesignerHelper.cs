namespace Mosaic.UI.Wpf.Common
{
    /// <summary>
    /// Provides access to design-time state detection.
    /// </summary>
    public class DesignerHelper
    {
        private static bool? _isInDesignMode;

        /// <summary>
        /// Gets a value that indicates whether the current process is running in design mode.
        /// </summary>
        public static bool IsInDesignMode
        {
            get
            {
                if (!_isInDesignMode.HasValue)
                {
                    _isInDesignMode = (bool)DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty,
                        typeof(FrameworkElement)).Metadata.DefaultValue;
                }
                return _isInDesignMode.Value;
            }
        }
    }
}
