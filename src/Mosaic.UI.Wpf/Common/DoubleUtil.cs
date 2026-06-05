/*
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Common
{
    /// <summary>
    /// Provides helpers for comparing floating-point values with tolerance.
    /// </summary>
    internal static class DoubleUtil
    {
        /// <devdoc>Smallest such that 1.0+DBL_EPSILON! = 1.0</devdoc>
        internal const double DBL_EPSILON = 2.2204460492503131e-016;

        /// <summary>
        /// Number close to zero, where float.MinValue is -float.MaxValue
        /// </summary>
        internal const float FLT_MIN = 1.175494351e-38F;

        /// <summary>
        /// Determines whether the specified value is close to zero.
        /// </summary>
        /// <param name="value">The value to evaluate.</param>
        /// <returns>
        /// <see langword="true" /> if the value is within the zero tolerance; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsZero(double value) => Math.Abs(value) < 10.0 * DBL_EPSILON;

        /// <summary>
        ///   Returns whether two doubles are "close". That is, whether
        ///   they are within epsilon of each other. Note that this epsilon
        ///   is proportional to the numbers themselves to that AreClose survives
        ///   scalar multiplication.
        /// </summary>
        /// <param name="value1">The first double to compare.</param>
        /// <param name="value2">The second double to compare.</param>
        /// <returns>bool - the result of the AreClose comparision.</returns>
        /// <remarks>
        /// This is based on code from https://github.com/gix/PresentationTheme.Aero available via the MIT License
        /// </remarks>
        public static bool AreClose(double value1, double value2)
        {
            //in case they are Infinities (then epsilon check does not work)
            if (value1 == value2) return true;
            // This computes (|value1-value2| / (|value1| + |value2| + 10.0)) < DBL_EPSILON
            double eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * DBL_EPSILON;
            double delta = value1 - value2;
            return -eps < delta && eps > delta;
        }

        /// <summary>
        ///   LessThan - Returns whether the first double is less than
        ///   the second double. That is, whether the first is strictly
        ///   less than *and* not within epsilon of the other number. Note that
        ///   this epsilon is proportional to the numbers themselves to that
        ///   AreClose survives scalar multiplication.
        /// </summary>
        /// <param name="value1">The first value to compare.</param>
        /// <param name="value2">The second value to compare.</param>
        /// <returns>The result of the comparison.</returns>
        public static bool LessThan(double value1, double value2)
        {
            return value1 < value2 && !AreClose(value1, value2);
        }

        /// <summary>
        ///   Returns whether the first double is greater than the second
        ///   double. That is, whether the first is strictly greater than
        ///   *and* not within epsilon of the other number. Note that this epsilon
        ///   is proportional to the numbers themselves to that AreClose survives
        ///   scalar multiplication.
        /// </summary>
        /// <param name="value1">The first value to compare.</param>
        /// <param name="value2">The second value to compare.</param>
        /// <returns>The result of the comparison.</returns>
        public static bool GreaterThan(double value1, double value2)
        {
            return value1 > value2 && !AreClose(value1, value2);
        }

        /// <summary>
        ///   Returns whether the first double is less than or close to
        ///   the second double. That is, whether the first is strictly
        ///   less than or within epsilon of the other number. Note that this
        ///   epsilon is proportional to the numbers themselves to that AreClose
        ///   survives scalar multiplication.
        /// </summary>
        /// <param name="value1">The first value to compare.</param>
        /// <param name="value2">The second value to compare.</param>
        /// <returns>The result of the comparison.</returns>
        public static bool LessThanOrClose(double value1, double value2)
        {
            return value1 < value2 || AreClose(value1, value2);
        }

        /// <summary>
        ///   GreaterThanOrClose - Returns whether the first double is
        ///   greater than or close to the second double. That is, whether or
        ///   not the first is strictly greater than or within epsilon of the
        ///   other number. Note that this epsilon is proportional to the numbers
        ///   themselves to that AreClose survives scalar multiplication.
        /// </summary>
        /// <param name="value1">The first value to compare.</param>
        /// <param name="value2">The second value to compare.</param>
        /// <returns>The result of the GreaterThanOrClose comparision.</returns>
        public static bool GreaterThanOrClose(double value1, double value2)
        {
            return value1 > value2 || AreClose(value1, value2);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct NanUnion
        {
            [FieldOffset(0)]
            internal double DoubleValue;
            [FieldOffset(0)]
            internal UInt64 UintValue;
        }

        /// <summary>
        /// Determines whether the specified value is not a number.
        /// </summary>
        /// <param name="value">The value to evaluate.</param>
        /// <returns>
        /// <see langword="true" /> if the value is not a number; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsNaN(double value)
        {
            NanUnion t = new NanUnion();
            t.DoubleValue = value;

            UInt64 exp = t.UintValue & 0xfff0000000000000;
            UInt64 man = t.UintValue & 0x000fffffffffffff;

            return (exp == 0x7ff0000000000000 || exp == 0xfff0000000000000) && (man != 0);
        }

        /// <summary>
        /// Determines whether the specified value is close to one.
        /// </summary>
        /// <param name="value">The value to evaluate.</param>
        /// <returns>
        /// <see langword="true" /> if the value is within the one tolerance; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsOne(double value)
        {
            return Math.Abs(value - 1.0) < 10.0 * DBL_EPSILON;
        }
    }
}