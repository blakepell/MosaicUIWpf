using Mosaic.UI.Wpf.AvalonDock.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Mosaic.UI.Wpf.AvalonDock.Controls
{
    /// <summary>
    /// Provides helper members for focus Element Manager.
    /// </summary>
    internal static class FocusElementManager
    {
        private static readonly List<DockingManager> _managers = [];
        private static readonly FullWeakDictionary<ILayoutElement, IInputElement> _modelFocusedElement = new();
        private static readonly WeakDictionary<ILayoutElement, IntPtr> _modelFocusedWindowHandle = new();
        private static WindowHookHandler? _windowHandler;

        /// <summary>
        /// Sets the setup Focus Management.
        /// </summary>
        /// <param name="manager">The manager.</param>
        internal static void SetupFocusManagement(DockingManager manager)
        {
            if (_managers.Count == 0)
            {
                _windowHandler = new WindowHookHandler();
                _windowHandler.FocusChanged += WindowFocusChanging;
                _windowHandler.Attach();
                if (Application.Current != null)
                {
                    var disp = Application.Current.Dispatcher;
                    Action subscribeToExitAction = () => Application.Current.Exit += Current_Exit;
                    if (disp.CheckAccess())
                    {
                        // if we are already on the dispatcher thread we don't need to call Invoke/BeginInvoke
                        subscribeToExitAction();
                    }
                    else
                    {
                        // For resolve issue "System.InvalidOperationException: Cannot perform this operation while dispatcher processing is suspended." make async subscribing instead of sync subscribing.
                        int disableProcessingCount = (int?)typeof(Dispatcher).GetField("_disableProcessingCount", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(disp) ?? 0;

                        if (disableProcessingCount == 0)
                        {
                            disp.Invoke(subscribeToExitAction);
                        }
                        else
                        {
                            disp.BeginInvoke(subscribeToExitAction);
                        }
                    }
                }
            }

            manager.PreviewGotKeyboardFocus += manager_PreviewGotKeyboardFocus;
            _managers.Add(manager);
        }

        /// <summary>
        /// Executes the finalize Focus Management operation.
        /// </summary>
        /// <param name="manager">The manager.</param>
        internal static void FinalizeFocusManagement(DockingManager manager)
        {
            manager.PreviewGotKeyboardFocus -= manager_PreviewGotKeyboardFocus;
            _managers.Remove(manager);

            if (_managers.Count == 0)
            {
                if (_windowHandler != null)
                {
                    _windowHandler.FocusChanged -= WindowFocusChanging;
                    _windowHandler.Detach();
                    _windowHandler = null;
                }
            }
        }

        /// <summary>
        /// Sets the set Focus On Last Element.
        /// </summary>
        /// <param name="model">The model.</param>
        internal static void SetFocusOnLastElement(ILayoutElement model)
        {
            bool focused = false;
            if (_modelFocusedElement.GetValue(model, out var objectToFocus))
            {
                focused = objectToFocus == Keyboard.Focus(objectToFocus);
            }

            if (_modelFocusedWindowHandle.GetValue(model, out var handleToFocus))
            {
                focused = IntPtr.Zero != Win32Helper.SetFocus(handleToFocus);
            }

        }

        private static void Current_Exit(object? sender, ExitEventArgs e)
        {
            if (Application.Current != null)
            {
                Application.Current.Exit -= Current_Exit;
            }

            if (_windowHandler != null)
            {
                _windowHandler.FocusChanged -= WindowFocusChanging;
                _windowHandler.Detach();
                _windowHandler = null;
            }
        }

        private static void manager_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus is Visual focusedElement &&
                !(focusedElement is LayoutAnchorableTabItem || focusedElement is LayoutDocumentTabItem))
            // Avoid tracking focus for elements like this
            {
                var parentAnchorable = focusedElement.FindVisualAncestor<LayoutAnchorableControl>();
                if (parentAnchorable != null)
                {
                    _modelFocusedElement[parentAnchorable.Model] = e.NewFocus;
                }
                else
                {
                    var parentDocument = focusedElement.FindVisualAncestor<LayoutDocumentControl>();
                    if (parentDocument != null)
                    {
                        _modelFocusedElement[parentDocument.Model] = e.NewFocus;
                    }
                }
            }
        }

        private static void WindowFocusChanging(object? sender, FocusChangeEventArgs e)
        {
            foreach (var manager in _managers)
            {
                var hostContainingFocusedHandle = manager.FindLogicalChildren<HwndHost>().FirstOrDefault(hw => Win32Helper.IsChild(hw.Handle, e.GotFocusWinHandle));

                if (hostContainingFocusedHandle != null)
                {
                    var parentAnchorable = hostContainingFocusedHandle.FindVisualAncestor<LayoutAnchorableControl>();
                    if (parentAnchorable != null)
                    {
                        _modelFocusedWindowHandle[parentAnchorable.Model] = e.GotFocusWinHandle;
                        if (parentAnchorable.Model != null)
                        {
                            parentAnchorable.Model.IsActive = true;
                        }
                    }
                    else
                    {
                        var parentDocument = hostContainingFocusedHandle.FindVisualAncestor<LayoutDocumentControl>();
                        if (parentDocument != null)
                        {
                            _modelFocusedWindowHandle[parentDocument.Model] = e.GotFocusWinHandle;
                            if (parentDocument.Model != null)
                            {
                                parentDocument.Model.IsActive = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
