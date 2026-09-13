/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Extensions;
using Mosaic.UI.Wpf.Scripting;
using Mouse = Argus.Windows.Mouse;

namespace Mosaic.UI.Wpf.Scripting.ScriptCommands
{
    /// <summary>
    /// Provides mouse position, button and wheel automation.
    /// </summary>
    [ScriptModule(Name = "mouse")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public partial class MouseScriptCommands
    {
        /// <summary>
        /// Sets the x and y position of the mouse pointer.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        [ScriptModuleMethod(Description = "Sets the x and y position of the mouse pointer.",
                               ParameterCount = 2)]
        public void SetPosition(int x, int y)
        {
            Application.Current.Dispatcher.InvokeIfRequired(() =>
            {
                Mouse.SetMousePos(x, y);
            });
        }

        /// <summary>
        /// Left-click the mouse button.
        /// </summary>
        [ScriptModuleMethod(Description = "Left-click the mouse button.")]
        public void LeftClick()
        {
            Application.Current.Dispatcher.InvokeIfRequired(Mouse.LeftClick);
        }

        /// <summary>
        /// Double-click the left mouse button.
        /// </summary>
        [ScriptModuleMethod(Description = "Double click the left mouse button.")]
        public void LeftDoubleClick()
        {
            Application.Current.Dispatcher.InvokeIfRequired(() =>
            {
                Mouse.LeftClick();
                Thread.Sleep(20);
                Mouse.LeftClick();
            });
        }

        /// <summary>
        /// Press the left mouse button down.
        /// </summary>
        [ScriptModuleMethod(Description = "Press the left mouse button down.")]
        public void LeftDown()
        {
            Application.Current.Dispatcher.InvokeIfRequired(Mouse.LeftDown);
        }

        /// <summary>
        /// Release the left mouse button.
        /// </summary>
        [ScriptModuleMethod(Description = "Release the left mouse button.")]
        public void LeftUp()
        {
            Application.Current.Dispatcher.InvokeIfRequired(Mouse.LeftUp);
        }

        /// <summary>
        /// Right-click the mouse button.
        /// </summary>
        [ScriptModuleMethod(Description = "Right click the mouse button.")]
        public void RightClick()
        {
            Application.Current.Dispatcher.InvokeIfRequired(Mouse.RightClick);
        }

        /// <summary>
        /// Press the right mouse button down.
        /// </summary>
        [ScriptModuleMethod(Description = "Press the right mouse button down.")]
        public void RightDown()
        {
            Application.Current.Dispatcher.InvokeIfRequired(Mouse.RightDown);
        }

        /// <summary>
        /// Release the right mouse button.
        /// </summary>
        [ScriptModuleMethod(Description = "Release the right mouse button.")]
        public void RightUp()
        {
            Application.Current.Dispatcher.InvokeIfRequired(Mouse.RightUp);
        }

        /// <summary>
        /// Middle-click the mouse button.
        /// </summary>
        [ScriptModuleMethod(Description = "Middle click the mouse button.")]
        public void MiddleClick()
        {
            Application.Current.Dispatcher.InvokeIfRequired(Mouse.MiddleClick);
        }

        /// <summary>
        /// Press the middle mouse button down.
        /// </summary>
        [ScriptModuleMethod(Description = "Press the middle mouse button down.")]
        public void MiddleDown()
        {
            Application.Current.Dispatcher.InvokeIfRequired(Mouse.MiddleDown);
        }

        /// <summary>
        /// Release the middle mouse button.
        /// </summary>
        [ScriptModuleMethod(Description = "Release the middle mouse button.")]
        public void MiddleUp()
        {
            Application.Current.Dispatcher.InvokeIfRequired(Mouse.MiddleUp);
        }

        /// <summary>
        /// The X coordinate of the mouse's current position.
        /// </summary>
        [Description("The X coordinate of the mouse's current position.")]
        public int X
        {
            get => Mouse.X();
            set
            {
                int y = Mouse.Y();
                Mouse.SetMousePos(value, y);
            }
        }

        /// <summary>
        /// The Y coordinate of the mouse's current position.
        /// </summary>
        [Description("The Y coordinate of the mouse's current position.")]
        public int Y
        {
            get => Mouse.Y();
            set
            {
                int x = Mouse.X();
                Mouse.SetMousePos(x, value);
            }
        }

        /// <summary>
        /// Scrolls up the specified number of clicks.
        /// </summary>
        /// <param name="clicks"></param>
        [ScriptModuleMethod(Description = "Scrolls up the specified number of clicks.",
                               ParameterCount = 1)]
        public void ScrollUp(int clicks)
        {
            Application.Current.Dispatcher.InvokeIfRequired(() =>
            {
                Mouse.ScrollUp(clicks);
            });
        }

        /// <summary>
        /// Scrolls down the specified number of clicks.
        /// </summary>
        /// <param name="clicks"></param>
        [ScriptModuleMethod(Description = "Scrolls down the specified number of clicks.",
                               ParameterCount = 1)]
        public void ScrollDown(int clicks)
        {
            Application.Current.Dispatcher.InvokeIfRequired(() =>
            {
                Mouse.ScrollDown(clicks);
            });
        }
    }
}
