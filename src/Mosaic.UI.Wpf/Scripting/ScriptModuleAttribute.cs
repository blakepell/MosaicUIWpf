/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Scripting
{
    /// <summary>
    /// Marks a class as a script module or one that can be accessed from the scripting
    /// engine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ScriptModuleAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the module.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// A description of the script module.
        /// </summary>
        public string? Description { get; set; }
    }
}