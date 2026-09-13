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
    /// Supplies completion descriptions and signature hints for a public .NET script member.
    /// This attribute is metadata and does not rename or restrict members in Topaz.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    public sealed class ScriptModuleMethodAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the function in the module (defaults to member name)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// An optional description that can be used for intellisense.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// An options auto complete verbiage hint for IDE's that implement it.
        /// </summary>
        public string? AutoCompleteHint { get; set; }

        /// <summary>
        /// A return hint so that autocomplete can infer what the DynValue is of the method.
        /// </summary>
        public string? ReturnTypeHint { get; set; }

        /// <summary>
        /// The number of parameters a function has.
        /// </summary>
        public int ParameterCount { get; set; }
    }
}
