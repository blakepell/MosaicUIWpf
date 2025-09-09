/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Extensions
{
    /// <summary>
    /// Provides a set of commonly used Dispatcher extension methods
    /// </summary>
    public static class DispatcherExtensions
    {
        /// <summary>
        /// A simple threading extension method, to invoke a delegate with a priority
        /// on the correct thread if it is not currently on the correct thread
        /// which can be used with DispatcherObject types
        /// </summary>
        /// <param name="dispatcher">Dispatcher object on which to perform the Invoke</param>
        /// <param name="action">The delegate to run</param>
        /// <param name="priority">The DispatcherPriority for the invoke</param>
        public static void InvokeIfRequired(this Dispatcher dispatcher, Action action, DispatcherPriority priority)
        {
            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(priority, action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// A simple threading extension method, to invoke a delegate
        /// on the correct thread if it is not currently on the correct thread
        /// which can be used with DispatcherObject types
        /// </summary>
        /// <param name="dispatcher">The Dispatcher object on which to perform the Invoke</param>
        /// <param name="action">The delegate to run</param>
        public static void InvokeIfRequired(this Dispatcher dispatcher, Action action)
        {
            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(DispatcherPriority.Normal, action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// A simple threading extension method, to invoke a delegate with an argument
        /// on the correct thread if it is not currently on the correct thread
        /// which can be used with DispatcherObject types
        /// </summary>
        /// <typeparam name="T">Delegate argument type</typeparam>
        /// <param name="dispatcher">The Dispatcher object on which to perform the Invoke</param>
        /// <param name="action">The delegate to run</param>
        /// <param name="arg">Delegate argument</param>
        public static void InvokeIfRequired<T>(this Dispatcher dispatcher, Action<T> action, T arg)
        {
            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(DispatcherPriority.Normal, action, arg);
            }
            else
            {
                action(arg);
            }
        }

        /// <summary>
        /// A simple threading extension method, to invoke a delegate with two arguments
        /// on the correct thread if it is not currently on the correct thread
        /// which can be used with DispatcherObject types
        /// </summary>
        /// <typeparam name="T1">First delegate argument type</typeparam>
        /// <typeparam name="T2">Second delegate argument type</typeparam>
        /// <param name="dispatcher">The Dispatcher object on which to perform the Invoke</param>
        /// <param name="action">The delegate to run</param>
        /// <param name="arg1">First delegate argument</param>
        /// <param name="arg2">Second delegate argument</param>
        public static void InvokeIfRequired<T1, T2>(this Dispatcher dispatcher, Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(DispatcherPriority.Normal, action, arg1, arg2);
            }
            else
            {
                action(arg1, arg2);
            }
        }

        /// <summary>
        /// A simple threading extension method, to invoke a delegate with three arguments
        /// on the correct thread if it is not currently on the correct thread
        /// which can be used with DispatcherObject types
        /// </summary>
        /// <typeparam name="T1">First delegate argument type</typeparam>
        /// <typeparam name="T2">Second delegate argument type</typeparam>
        /// <typeparam name="T3">Third delegate argument type</typeparam>
        /// <param name="dispatcher">The Dispatcher object on which to perform the Invoke</param>
        /// <param name="action">The delegate to run</param>
        /// <param name="arg1">First delegate argument</param>
        /// <param name="arg2">Second delegate argument</param>
        /// <param name="arg3">Third delegate argument</param>
        public static void InvokeIfRequired<T1, T2, T3>(this Dispatcher dispatcher, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(DispatcherPriority.Normal, action, arg1, arg2, arg3);
            }
            else
            {
                action(arg1, arg2, arg3);
            }
        }

        /// <summary>
        /// A simple threading extension method, to invoke a delegate with a return value
        /// on the correct thread if it is not currently on the correct thread
        /// which can be used with DispatcherObject types
        /// </summary>
        /// <typeparam name="TResult">Type of return value</typeparam>
        /// <param name="dispatcher">The Dispatcher object on which to perform the Invoke</param>
        /// <param name="action">The delegate to run</param>
        /// <returns>Delegate return value</returns>
        public static TResult InvokeIfRequired<TResult>(this Dispatcher dispatcher, Func<TResult> action)
        {
            if (!dispatcher.CheckAccess())
            {
                return (TResult)dispatcher.Invoke(DispatcherPriority.Normal, action);
            }

            return action();
        }

        /// <summary>
        /// A simple threading extension method, to invoke a delegate with an argument and return value
        /// on the correct thread if it is not currently on the correct thread which can be used with DispatcherObject types.
        /// This overload can be used to avoid closures by passing the argument as a parameter.
        /// </summary>
        /// <typeparam name="T">Delegate argument type</typeparam>
        /// <typeparam name="TResult">Type of return value</typeparam>
        /// <param name="dispatcher">The Dispatcher object on which to perform the Invoke</param>
        /// <param name="action">The delegate to run</param>
        /// <param name="arg">Delegate argument</param>
        /// <returns>Delegate return value</returns>
        public static TResult InvokeIfRequired<T, TResult>(this Dispatcher dispatcher, Func<T, TResult> action, T arg)
        {
            if (!dispatcher.CheckAccess())
            {
                return (TResult)dispatcher.Invoke(DispatcherPriority.Normal, action, arg);
            }

            return action(arg);
        }

        /// <summary>
        /// A simple threading extension method, to invoke a delegate with an argument and return value
        /// on the correct thread if it is not currently on the correct thread which can be used with DispatcherObject types.
        /// This overload can be used to avoid closures by passing the argument as a parameter.
        /// </summary>
        /// <typeparam name="T1">Delegate argument type</typeparam>
        /// <typeparam name="T2">Delegate argument type</typeparam>
        /// <typeparam name="TResult">Type of return value</typeparam>
        /// <param name="dispatcher">The Dispatcher object on which to perform the Invoke</param>
        /// <param name="action">The delegate to run</param>
        /// <param name="arg1">Delegate argument</param>
        /// <param name="arg2">Delegate argument</param>
        /// <returns>Delegate return value</returns>
        public static TResult InvokeIfRequired<T1, T2, TResult>(this Dispatcher dispatcher, Func<T1, T2, TResult> action, T1 arg1, T2 arg2)
        {
            if (!dispatcher.CheckAccess())
            {
                return (TResult)dispatcher.Invoke(DispatcherPriority.Normal, action, arg1, arg2);
            }

            return action(arg1, arg2);
        }

        /// <summary>
        /// A simple threading extension method, to invoke a delegate asynchronously with a priority
        /// on the correct thread if it is not currently on the correct thread
        /// which can be used with DispatcherObject types
        /// </summary>
        /// <param name="dispatcher">Dispatcher object on which to perform the Invoke</param>
        /// <param name="action">The delegate to run</param>
        /// <param name="priority">DispatcherPriority for the invoke</param>
        public static void BeginInvokeIfRequired(this Dispatcher dispatcher,
            Action action, DispatcherPriority priority)
        {
            if (!dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(priority, action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// A simple threading extension method, to invoke a delegate asynchronously
        /// on the correct thread if it is not currently on the correct thread
        /// which can be used with DispatcherObject types
        /// </summary>
        /// <param name="dispatcher">Dispatcher object on which to perform the Invoke</param>
        /// <param name="action">The delegate to run</param>
        public static void BeginInvokeIfRequired(this Dispatcher dispatcher, Action action)
        {
            if (!dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// A simple threading extension method, to invoke a delegate asynchronously with an argument
        /// on the correct thread if it is not currently on the correct thread
        /// which can be used with DispatcherObject types
        /// </summary>
        /// <typeparam name="T">Delegate argument type</typeparam>
        /// <param name="dispatcher">Dispatcher object on which to perform the Invoke</param>
        /// <param name="action">The delegate to run</param>
        /// <param name="arg">Delegate argument</param>
        public static void BeginInvokeIfRequired<T>(this Dispatcher dispatcher, Action<T> action, T arg)
        {
            if (!dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(DispatcherPriority.Normal, action, arg);
            }
            else
            {
                action(arg);
            }
        }

        /// <summary>
        /// Wait on Dispatcher until it invokes a function of a given priority
        /// </summary>
        /// <param name="dispatcher">Dispatcher object on which to wait</param>
        /// <param name="priority">DispatcherPriority for the invoke</param>
        public static void WaitForPriority(this Dispatcher dispatcher, DispatcherPriority priority)
        {
            var frame = new DispatcherFrame();
            var dispatcherOperation = dispatcher.BeginInvoke(priority, new DispatcherOperationCallback(ExitFrameOperation), frame);
            Dispatcher.PushFrame(frame);
            if (dispatcherOperation.Status != DispatcherOperationStatus.Completed)
            {
                dispatcherOperation.Abort();
            }
        }

        private static object? ExitFrameOperation(object obj)
        {
            ((DispatcherFrame)obj).Continue = false;
            return null;
        }
    }
}
