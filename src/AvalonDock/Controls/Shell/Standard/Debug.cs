/**************************************************************************\
	Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

// Conditional to use more aggressive fail-fast behaviors when debugging.
#define DEV_DEBUG

using System.Diagnostics;

// This file contains general utilities to aid in development.
// It is distinct from unit test Assert classes.
// Classes here generally shouldn't be exposed publicly since
// they're not particular to any library functionality.
// Because the classes here are internal, it's likely this file
// might be included in multiple assemblies.
namespace AvalonDock.Controls.Shell.Standard
{
    /// <summary>
	/// Provides helper members for assert.
	/// </summary>
	internal static class Assert
    {
        /// <summary>
        /// Executes the break operation.
        /// </summary>
        private static void _Break()
        {
#if DEV_DEBUG
            Debugger.Break();
#else
            Debug.Assert(false);
#endif
        }

        /// <summary>
        /// Executes the are Equal operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="expected">The expected.</param>
        /// <param name="actual">The actual.</param>
        [Conditional("DEBUG")]
        public static void AreEqual<T>(T expected, T actual)
        {
            if (expected == null)
            {
                // Two nulls are considered equal, regardless of type semantics.
                if (actual != null && !actual.Equals(expected))
                {
                    _Break();
                }
            }
            else if (!expected.Equals(actual))
            {
                _Break();
            }
        }

        /// <summary>
        /// Executes the implies operation.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="result">The result.</param>
        [Conditional("DEBUG")]
        public static void Implies(bool condition, bool result)
        {
            if (condition && !result)
            {
                _Break();
            }
        }

        /// <summary>
        /// Executes the is Neither Null Nor Empty operation.
        /// </summary>
        /// <param name="value">The value.</param>
        [Conditional("DEBUG")]
        public static void IsNeitherNullNorEmpty(string value)
        {
            IsFalse(string.IsNullOrEmpty(value));
        }

        /// <summary>
        /// Executes the is Not Null operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="value">The value.</param>
        [Conditional("DEBUG")]
        public static void IsNotNull<T>(T? value)
            where T : class
        {
            if (value == null)
            {
                _Break();
            }
        }

        /// <summary>
        /// Executes the is Not Default operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="value">The value.</param>
        [Conditional("DEBUG")]
        public static void IsNotDefault<T>(T value)
            where T : struct
        {
            if (value.Equals(default(T)))
            {
                Fail();
            }
        }

        /// <summary>
        /// Executes the is False operation.
        /// </summary>
        /// <param name="condition">The condition.</param>
        [Conditional("DEBUG")]
        public static void IsFalse(bool condition)
        {
            if (condition)
            {
                _Break();
            }
        }

        /// <summary>
        /// Executes the is True operation.
        /// </summary>
        /// <param name="condition">The condition.</param>
        [Conditional("DEBUG")]
        public static void IsTrue(bool condition)
        {
            if (!condition)
            {
                _Break();
            }
        }

        /// <summary>
        /// Executes the fail operation.
        /// </summary>
        [Conditional("DEBUG")]
        public static void Fail() => _Break();

        /// <summary>
        /// Executes the fail operation.
        /// </summary>
        /// <param name="message">The message.</param>
        [Conditional("DEBUG")]
        public static void Fail(string message) => _Break();

        /// <summary>
        /// Executes the is Null operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="item">The item.</param>
        [Conditional("DEBUG")]
        public static void IsNull<T>(T? item)
            where T : class
        {
            if (item != null)
            {
                _Break();
            }
        }

        /// <summary>
        /// Executes the bounded Integer operation.
        /// </summary>
        /// <param name="lowerBoundInclusive">The lower Bound Inclusive.</param>
        /// <param name="value">The value.</param>
        /// <param name="upperBoundExclusive">The upper Bound Exclusive.</param>
        [Conditional("DEBUG")]
        public static void BoundedInteger(int lowerBoundInclusive, int value, int upperBoundExclusive)
        {
            if (value < lowerBoundInclusive || value >= upperBoundExclusive)
            {
                _Break();
            }
        }

        /// <summary>
        /// Executes the nullable Is Null operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="value">The value.</param>
        [Conditional("DEBUG")]
        public static void NullableIsNull<T>(T? value)
            where T : struct
        {
            if (value != null)
            {
                _Break();
            }
        }
    }
}