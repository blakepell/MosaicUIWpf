/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Esprima.Ast;
using Mosaic.UI.Scripting.Core;
using Mosaic.UI.Scripting.ErrorHandling;

namespace Mosaic.UI.Scripting.Statements
{
    /// <summary>
    /// Executes <c>include System.Text</c> by importing the namespace into the engine,
    /// the same as the host calling <see cref="ScriptEngine.Imports(string[])"/>.
    /// </summary>
    internal static class IncludeStatementHandler
    {
        internal static object Execute(ScriptExecutor scriptExecutor, Node statement)
        {
            var include = (IncludeStatement)statement;
            var engine = scriptExecutor.ScriptEngine;
            var filter = engine.Options.IncludeFilter;
            if (filter != null && !filter(include.Namespace))
            {
                Exceptions.ThrowIncludeIsNotAllowed(include.Namespace);
            }

            engine.Imports(include.Namespace);
            return scriptExecutor.GetNullOrUndefined();
        }
    }
}
