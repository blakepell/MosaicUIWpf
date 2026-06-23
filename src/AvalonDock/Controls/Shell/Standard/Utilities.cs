/**************************************************************************\
	Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

// This file contains general utilities to aid in development.
// Classes here generally shouldn't be exposed publicly since
// they're not particular to any library functionality.
// Because the classes here are internal, it's likely this file
// might be included in multiple assemblies.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace AvalonDock.Controls.Shell.Standard
{
    /// <summary>Represents the Utility class.</summary>
	internal static class Utility
    {
        /// <summary>The _osVersion value.</summary>
        private static readonly Version _osVersion = Environment.OSVersion.Version;

        /// <summary>The _presentationFrameworkVersion value.</summary>
        private static readonly Version _presentationFrameworkVersion = Assembly.GetAssembly(typeof(Window)).GetName().Version;

        /// <summary>Performs the ColorFromArgbDword operation.</summary>
        /// <param name="color">The color value.</param>
        /// <returns>The result of the operation.</returns>
        public static Color ColorFromArgbDword(uint color) => Color.FromArgb((byte)((color & 0xFF000000) >> 24), (byte)((color & 0x00FF0000) >> 16), (byte)((color & 0x0000FF00) >> 8), (byte)((color & 0x000000FF) >> 0));

        /// <summary>Performs the GET_X_LPARAM operation.</summary>
        /// <param name="lParam">The lParam value.</param>
        /// <returns>The result of the operation.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int GET_X_LPARAM(IntPtr lParam) => LOWORD(lParam.ToInt32());

        /// <summary>Performs the GET_Y_LPARAM operation.</summary>
        /// <param name="lParam">The lParam value.</param>
        /// <returns>The result of the operation.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int GET_Y_LPARAM(IntPtr lParam) => HIWORD(lParam.ToInt32());

        /// <summary>Performs the HIWORD operation.</summary>
        /// <param name="i">The i value.</param>
        /// <returns>The result of the operation.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int HIWORD(int i) => (short)(i >> 16);

        /// <summary>Performs the LOWORD operation.</summary>
        /// <param name="i">The i value.</param>
        /// <returns>The result of the operation.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static int LOWORD(int i) => (short)(i & 0xFFFF);

        /// <summary>Performs the IsFlagSet operation.</summary>
        /// <param name="value">The value value.</param>
        /// <param name="mask">The mask value.</param>
        /// <returns>The result of the operation.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsFlagSet(int value, int mask) => (value & mask) != 0;

        /// <summary>Gets a value indicating whether IsOSVistaOrNewer.</summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsOSVistaOrNewer => _osVersion >= new Version(6, 0);

        /// <summary>Gets a value indicating whether IsOSWindows7OrNewer.</summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsOSWindows7OrNewer => _osVersion >= new Version(6, 1);

        /// <summary>Gets a value indicating whether IsOSWindows8OrNewer.</summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsOSWindows8OrNewer => _osVersion >= new Version(6, 2);

        /// <summary>Gets a value indicating whether IsPresentationFrameworkVersionLessThan4.</summary>
        public static bool IsPresentationFrameworkVersionLessThan4 => _presentationFrameworkVersion < new Version(4, 0);

        /// <summary>Performs the SafeDeleteObject operation.</summary>
        /// <param name="gdiObject">The gdiObject value.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void SafeDeleteObject(ref IntPtr gdiObject)
        {
            var p = gdiObject;
            gdiObject = IntPtr.Zero;
            if (p != IntPtr.Zero)
            {
                NativeMethods.DeleteObject(p);
            }
        }

        /// <summary>Performs the SafeDestroyWindow operation.</summary>
        /// <param name="hwnd">The hwnd value.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void SafeDestroyWindow(ref IntPtr hwnd)
        {
            var p = hwnd;
            hwnd = IntPtr.Zero;
            if (NativeMethods.IsWindow(p))
            {
                NativeMethods.DestroyWindow(p);
            }
        }

        /// <summary>Performs the SafeDispose operation.</summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="disposable">The disposable value.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void SafeDispose<T>(ref T disposable)
            where T : IDisposable
        {
            // Dispose can safely be called on an object multiple times.
            IDisposable t = disposable;
            disposable = default;
            t?.Dispose();
        }

        /// <summary>Performs the SafeFreeHGlobal operation.</summary>
        /// <param name="hglobal">The hglobal value.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static void SafeFreeHGlobal(ref IntPtr hglobal)
        {
            var p = hglobal;
            hglobal = IntPtr.Zero;
            if (p != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(p);
            }
        }

        /// <summary>Performs the SafeRelease operation.</summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="comObject">The comObject value.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static void SafeRelease<T>(ref T comObject)
            where T : class
        {
            var t = comObject;
            comObject = default(T);
            if (t == null)
            {
                return;
            }

            Assert.IsTrue(Marshal.IsComObject(t));
            Marshal.ReleaseComObject(t);
        }

        /// <summary>Represents the _UrlDecoder class.</summary>
        private class _UrlDecoder
        {
            /// <summary>The _encoding value.</summary>
            private readonly Encoding _encoding;

            /// <summary>The _charBuffer value.</summary>
            private readonly char[] _charBuffer;

            /// <summary>The _byteBuffer value.</summary>
            private readonly byte[] _byteBuffer;

            /// <summary>The _byteCount value.</summary>
            private int _byteCount;

            /// <summary>The _charCount value.</summary>
            private int _charCount;

            /// <summary>Initializes a new instance of the <see cref="_UrlDecoder"/> class.</summary>
            /// <param name="size">The size value.</param>
            /// <param name="encoding">The encoding value.</param>
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public _UrlDecoder(int size, Encoding encoding)
            {
                _encoding = encoding;
                _charBuffer = new char[size];
                _byteBuffer = new byte[size];
            }

            /// <summary>Performs the AddByte operation.</summary>
            /// <param name="b">The b value.</param>
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void AddByte(byte b) => _byteBuffer[_byteCount++] = b;

            /// <summary>Performs the AddChar operation.</summary>
            /// <param name="ch">The ch value.</param>
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void AddChar(char ch)
            {
                _FlushBytes();
                _charBuffer[_charCount++] = ch;
            }

            /// <summary>Performs the _FlushBytes operation.</summary>
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            private void _FlushBytes()
            {
                if (_byteCount <= 0)
                {
                    return;
                }

                _charCount += _encoding.GetChars(_byteBuffer, 0, _byteCount, _charBuffer, _charCount);
                _byteCount = 0;
            }

            /// <summary>Performs the GetString operation.</summary>
            /// <returns>The result of the operation.</returns>
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public string GetString()
            {
                _FlushBytes();
                return _charCount > 0 ? new string(_charBuffer, 0, _charCount) : string.Empty;
            }
        }

        /// <summary>Performs the AddDependencyPropertyChangeListener operation.</summary>
        /// <param name="component">The component value.</param>
        /// <param name="property">The property value.</param>
        /// <param name="listener">The listener value.</param>
        public static void AddDependencyPropertyChangeListener(object component, DependencyProperty property, EventHandler listener)
        {
            if (component == null)
            {
                return;
            }

            Assert.IsNotNull(property);
            Assert.IsNotNull(listener);
            var dpd = DependencyPropertyDescriptor.FromProperty(property, component.GetType());
            dpd.AddValueChanged(component, listener);
        }

        /// <summary>Performs the RemoveDependencyPropertyChangeListener operation.</summary>
        /// <param name="component">The component value.</param>
        /// <param name="property">The property value.</param>
        /// <param name="listener">The listener value.</param>
        public static void RemoveDependencyPropertyChangeListener(object component, DependencyProperty property, EventHandler listener)
        {
            if (component == null)
            {
                return;
            }

            Assert.IsNotNull(property);
            Assert.IsNotNull(listener);
            var dpd = DependencyPropertyDescriptor.FromProperty(property, component.GetType());
            dpd.RemoveValueChanged(component, listener);
        }

        /// <summary>Performs the IsThicknessNonNegative operation.</summary>
        /// <param name="thickness">The thickness value.</param>
        /// <returns>The result of the operation.</returns>
        public static bool IsThicknessNonNegative(Thickness thickness)
        {
            if (!IsDoubleFiniteAndNonNegative(thickness.Top))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(thickness.Left))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(thickness.Bottom))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(thickness.Right))
            {
                return false;
            }

            return true;
        }

        /// <summary>Performs the IsCornerRadiusValid operation.</summary>
        /// <param name="cornerRadius">The cornerRadius value.</param>
        /// <returns>The result of the operation.</returns>
        public static bool IsCornerRadiusValid(CornerRadius cornerRadius)
        {
            if (!IsDoubleFiniteAndNonNegative(cornerRadius.TopLeft))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(cornerRadius.TopRight))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(cornerRadius.BottomLeft))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(cornerRadius.BottomRight))
            {
                return false;
            }

            return true;
        }

        /// <summary>Performs the IsDoubleFiniteAndNonNegative operation.</summary>
        /// <param name="d">The d value.</param>
        /// <returns>The result of the operation.</returns>
        public static bool IsDoubleFiniteAndNonNegative(double d) => !double.IsNaN(d) && !double.IsInfinity(d) && !(d < 0);
    }
}