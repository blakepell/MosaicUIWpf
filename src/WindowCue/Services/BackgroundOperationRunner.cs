/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Diagnostics;

namespace WindowCue.Services
{
    /// <summary>
    /// Isolates blocking native/provider work on an MTA background thread. A timeout releases
    /// the caller, not the native call: the lane remains occupied until that call really exits.
    /// This prevents repeated timer ticks/clicks from accumulating hung workers or queued work.
    /// </summary>
    internal sealed class BackgroundOperationRunner
    {
        private int _busy;

        internal Task<BackgroundResult<T>> RunAsync<T>(Func<T> operation, TimeSpan? timeout = null) =>
            RunAsync(_ => operation(), timeout);

        internal async Task<BackgroundResult<T>> RunAsync<T>(Func<CancellationToken, T> operation,
            TimeSpan? timeout = null)
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
                return new(false, default);

            var completion = new TaskCompletionSource<BackgroundResult<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancellation = new CancellationTokenSource();
            var thread = new Thread(() =>
            {
                BackgroundResult<T> result;
                try { result = new(true, operation(cancellation.Token)); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"WindowCue background operation failed: {ex}");
                    result = new(false, default);
                }
                finally
                {
                    Interlocked.Exchange(ref _busy, 0);
                }
                completion.TrySetResult(result);
            }) { IsBackground = true, Name = "WindowCue native work" };
            try
            {
                thread.SetApartmentState(ApartmentState.MTA);
                thread.Start();
            }
            catch
            {
                Interlocked.Exchange(ref _busy, 0);
                cancellation.Dispose();
                throw;
            }
            try
            {
                return await completion.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                cancellation.Cancel();
                Debug.WriteLine("WindowCue background operation timed out; its lane remains occupied until it exits.");
                return new(false, default);
            }
            finally
            {
                // Native work may still be using this token after a timeout.
                _ = completion.Task.ContinueWith(_ => cancellation.Dispose(), CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }
    }

    /// <summary>A failed, busy or timed-out inspection carries no evidence of target closure.</summary>
    internal sealed record BackgroundResult<T>(bool Completed, T? Value);
}
