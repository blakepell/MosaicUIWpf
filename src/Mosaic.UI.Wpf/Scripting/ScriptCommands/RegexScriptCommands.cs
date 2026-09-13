/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Scripting;

namespace Mosaic.UI.Wpf.Scripting.ScriptCommands
{
    /// <summary>
    /// Regular Expression Commands
    /// </summary>
    [ScriptModule(Name = "regex")]
    public class RegexScriptCommands
    {
        /// <summary>
        /// A static instance of this class.
        /// </summary>
        public static readonly RegexScriptCommands Instance = new();

        /// <summary>
        /// If a regular expression pattern matches the provided input.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        [ScriptModuleMethod(Name = nameof(IsMatch),
            Description = "If a regular expression pattern matches the provided input.",
            ParameterCount = 2)]
        public bool IsMatch(string input, string pattern)
        {
            return Regex.IsMatch(input, pattern);
        }

        /// <summary>
        /// If a RegEx match list contains a specific value.
        /// </summary>
        /// <param name="regexString"></param>
        /// <param name="value"></param>
        [ScriptModuleMethod(Name = nameof(ListContainsValue),
            Description = "If a regular expression list contains a specific value.",
            ParameterCount = 2)]
        public bool ListContainsValue(string? regexString, string value)
        {
            if (string.IsNullOrEmpty(regexString) || string.IsNullOrEmpty(value))
            {
                return false;
            }

            var pattern = $@"\b{Regex.Escape(value)}\b";
            return Regex.IsMatch(regexString, pattern);
        }

        /// <summary>
        /// Remove a value from a RegEx match list if it's found.
        /// </summary>
        /// <param name="regexString"></param>
        /// <param name="value"></param>
        [ScriptModuleMethod(Name = nameof(ListRemoveValue),
            Description = "Removes a value from a regular expression list.",
            ParameterCount = 2)]
        public string? ListRemoveValue(string? regexString, string value)
        {
            if (string.IsNullOrEmpty(regexString) || string.IsNullOrEmpty(value))
            {
                return regexString;
            }

            // Use regex to remove the value and any trailing or leading "|"
            var pattern = $@"(\|)?\b{Regex.Escape(value)}\b(\|)?";
            var updatedString = Regex.Replace(regexString, pattern, match => match.Value.StartsWith("|") && match.Value.EndsWith("|") ? "|" : string.Empty);

            // Clean up redundant "|" characters
            updatedString = updatedString.Trim('|');

            // Remove any existing leading and trailing parentheses
            updatedString = updatedString.Trim('(', ')');

            // Return the result with exactly one set of parentheses
            return $"({updatedString})";
        }

        /// <summary>
        /// Adds a value to a RegEx list if it doesn't exist.
        /// </summary>
        /// <param name="regexString"></param>
        /// <param name="value"></param>
        [ScriptModuleMethod(Name = nameof(IsMatch),
            Description = "Adds a value to a regular expression list if it does not exist.",
            ParameterCount = 2)]
        public string? ListAddValue(string? regexString, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return regexString;
            }

            if (ListContainsValue(regexString, value))
            {
                return regexString;
            }

            // Remove surrounding parentheses to modify the list
            var innerValues = (regexString ?? string.Empty).Trim('(', ')');

            // Add the new value
            if (!string.IsNullOrEmpty(innerValues))
            {
                innerValues += $"|{value}";
            }
            else
            {
                innerValues = value;
            }

            return $"({innerValues})";
        }
    }
}
