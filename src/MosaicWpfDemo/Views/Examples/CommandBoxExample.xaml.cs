/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls;
using System;
using System.Windows;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class CommandBoxExample
    {
        public CommandBoxExample()
        {
            InitializeComponent();

            // Seed the history so Up/Down have something to walk on the first visit.
            this.Input.ImportHistory(["help", "time", "echo hello"]);

            this.Write("Mosaic CommandBox demo. Type 'help' for the list of commands.");
        }

        /// <summary>
        /// Handles a command dispatched from the <see cref="CommandBox"/>. This is the whole contract:
        /// the control normalizes the text, applies its commit behavior and records the history, and
        /// the host decides what the command means.
        /// </summary>
        private void Input_CommandExecuted(object? sender, CommandExecutedEventArgs e)
        {
            this.Write($"> {e.Command}");

            string[] parts = e.Command.Split(' ', 2);

            switch (parts[0].ToLowerInvariant())
            {
                case "help":
                    this.Write("  help           Show this text.");
                    this.Write("  time           Print the current time.");
                    this.Write("  echo <text>    Echo the rest of the line back.");
                    this.Write("  history        List the recorded commands.");
                    this.Write("  clear          Clear this output pane.");
                    break;

                case "time":
                    this.Write($"  {DateTime.Now:F}");
                    break;

                case "echo":
                    this.Write($"  {(parts.Length > 1 ? parts[1] : string.Empty)}");
                    break;

                case "history":
                    if (this.Input.History.Count == 0)
                    {
                        this.Write("  (empty)");
                        break;
                    }

                    for (int i = 0; i < this.Input.History.Count; i++)
                    {
                        this.Write($"  {i + 1,3}  {this.Input.History.Items[i]}");
                    }

                    break;

                case "clear":
                    this.Output.Clear();
                    break;

                default:
                    this.Write($"  Unknown command '{parts[0]}'. Try 'help'.");
                    break;
            }
        }

        /// <summary>
        /// Empties the command history so the arrow keys start over.
        /// </summary>
        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            this.Input.ClearHistory();
            this.Write("  History cleared.");
        }

        /// <summary>
        /// Appends a line to the output pane and scrolls it into view.
        /// </summary>
        /// <param name="text">The line to append.</param>
        private void Write(string text)
        {
            this.Output.AppendText(text + Environment.NewLine);
            this.Output.ScrollToEnd();
        }
    }
}
