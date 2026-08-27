/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Runtime.ExceptionServices;
using System.Windows;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Guards the assembly theme dictionaries (<c>Themes/Generic.xaml</c>) against values that WPF
    /// cannot freeze.
    /// </summary>
    /// <remarks>
    /// A <see cref="ComponentResourceKey"/> or <see cref="SystemResourceKey"/> lookup that misses the
    /// element and application resource trees falls through to
    /// <c>SystemResources.FindDictionaryResource</c>, which calls <see cref="Freezable.Freeze"/> on the
    /// value it finds in the owning assembly's theme dictionary. A <see cref="Freezable"/> whose
    /// property was set with a <c>DynamicResource</c> or a <c>Binding</c> cannot be frozen, so such a
    /// value crashes the process with "This Freezable cannot be frozen" the first time a lookup falls
    /// through - which happens, for example, while a template subtree is torn down during the resize
    /// that follows an RDP reconnect.
    /// </remarks>
    public class ThemeDictionaryFreezeTests
    {
        /// <summary>
        /// Runs the test body on an STA thread, which WPF requires.
        /// </summary>
        private static void RunSta(Action action)
        {
            Exception? failure = null;

            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        /// <summary>
        /// Asserts every <see cref="Freezable"/> reachable from a theme dictionary can be frozen, the
        /// same way <c>SystemResources</c> would freeze it during a resource lookup.
        /// </summary>
        /// <param name="packUri">The pack URI of the theme dictionary to walk.</param>
        private static void AssertThemeDictionaryIsFreezable(string packUri)
        {
            // Touching Application runs its static initializer, which registers the "pack" URI scheme.
            _ = Application.ResourceAssembly;

            var dictionary = new ResourceDictionary { Source = new Uri(packUri) };
            var offenders = new List<string>();

            Walk(dictionary);

            void Walk(ResourceDictionary d)
            {
                foreach (var merged in d.MergedDictionaries)
                {
                    Walk(merged);
                }

                foreach (var key in d.Keys.Cast<object>().ToList())
                {
                    // Only Type and ResourceKey entries are reachable through the theme dictionary
                    // lookup that does the freezing; anything else is resolved from the element tree.
                    if (key is not Type && key is not ResourceKey)
                    {
                        continue;
                    }

                    object? value;

                    try
                    {
                        value = d[key];
                    }
                    catch (Exception ex)
                    {
                        offenders.Add($"{key} could not be realized: {ex.Message}");
                        continue;
                    }

                    if (value is Freezable freezable && !freezable.CanFreeze)
                    {
                        offenders.Add($"{key} ({value.GetType().Name})");
                    }
                }
            }

            Assert.True(offenders.Count == 0,
                $"{offenders.Count} unfreezable value(s) in {packUri}. WPF freezes theme dictionary "
                + "values during resource lookup, so these will throw \"This Freezable cannot be "
                + "frozen\" at runtime: "
                + string.Join(", ", offenders.Take(10))
                + (offenders.Count > 10 ? ", ..." : string.Empty));
        }

        [Fact]
        public void AvalonDockThemeDictionaryContainsNoUnfreezableValues()
        {
            RunSta(() => AssertThemeDictionaryIsFreezable("pack://application:,,,/Mosaic.UI.Wpf.AvalonDock;component/Themes/Generic.xaml"));
        }

        [Fact]
        public void MosaicThemeDictionaryContainsNoUnfreezableValues()
        {
            RunSta(() => AssertThemeDictionaryIsFreezable("pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Generic.xaml"));
        }
    }
}
