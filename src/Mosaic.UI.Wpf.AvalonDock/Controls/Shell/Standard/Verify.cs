/**************************************************************************\
	Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

// This file contains general utilities to aid in development.
// Classes here generally shouldn't be exposed publicly since
// they're not particular to any library functionality.
// Because the classes here are internal, it's likely this file
// might be included in multiple assemblies.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Mosaic.UI.Wpf.AvalonDock.Controls.Shell.Standard
{
    /// <summary>
	/// Provides helper members for verify.
	/// </summary>
	internal static class Verify
    {
        /// <summary>
        /// Executes the is Not Null operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="obj">The obj.</param>
        /// <param name="name">The name.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DebuggerStepThrough]
        public static void IsNotNull<T>(T obj, string name)
            where T : class
        {
            if (obj == null)
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}