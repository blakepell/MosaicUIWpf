/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Esprima.Utils;

namespace Esprima.Ast
{
    /// <summary>
    /// A Mosaic extension statement, <c>include System.Text</c>, that imports a .NET namespace so its
    /// types can be used by short name. It is not part of ECMAScript.
    /// </summary>
    public sealed class IncludeStatement : Statement
    {
        /// <summary>
        /// The full name of the namespace, eg: System.Collections.Generic.
        /// </summary>
        public readonly string Namespace;

        public IncludeStatement(string @namespace) : base(Nodes.IncludeStatement)
        {
            Namespace = @namespace;
        }

        public override NodeCollection ChildNodes => NodeCollection.Empty;

        protected internal override void Accept(AstVisitor visitor)
        {
        }
    }
}
