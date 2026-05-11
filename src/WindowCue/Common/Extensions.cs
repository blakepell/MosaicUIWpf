/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;

namespace WindowCue.Common
{
    /// <summary>
    /// General -purpose extension methods.
    /// </summary>
    internal static class Extensions
    {
        internal static string? GetCommandLine(this Process? process)
        {
            if (process == null)
            {
                return null;
            }

            using var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
            using var objects = searcher.Get();
            return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
        }
    }
}
