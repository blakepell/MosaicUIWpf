/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.Concurrent;
using System.Collections.ObjectModel;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A <see cref="ComboBox"/> that populates itself from the declared members of an enum type
    /// assigned to <see cref="EnumType"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each enum member's display text is resolved with the following precedence:
    /// <see cref="DisplayNameAttribute"/>, then <see cref="DescriptionAttribute"/>, then the enum
    /// member name.  Because C# does not permit <see cref="DisplayNameAttribute"/> directly on
    /// fields, apply <see cref="EnumDisplayNameAttribute"/> to enum members instead — it is
    /// resolved through the same base-type lookup.  The actual enum value is exposed through
    /// <see cref="System.Windows.Controls.Primitives.Selector.SelectedValue"/>, which is the
    /// intended binding surface for view models — <c>SelectedItem</c> exposes the internal
    /// <see cref="EnumComboBoxItem"/> wrapper instead.
    /// </para>
    /// <para>
    /// Nullable enum types are supported (the underlying enum type is resolved) and a nullable
    /// view-model property may hold <c>null</c>, which simply results in no selection.  Enums
    /// marked with <see cref="FlagsAttribute"/> are presented as their explicitly declared
    /// members only; this control binds a single declared value and is not a flags editor.
    /// </para>
    /// <para>
    /// The control owns its item generation: <see cref="EnumType"/> is the source of truth and
    /// <c>ItemsSource</c> should not be set by consumers.
    /// </para>
    /// <example>
    /// <code language="xml">
    /// &lt;mosaic:EnumComboBox
    ///     EnumType="{x:Type local:OrderStatus}"
    ///     SelectedValue="{Binding Status, Mode=TwoWay}" /&gt;
    /// </code>
    /// </example>
    /// </remarks>
    public class EnumComboBox : ComboBox
    {
        /// <summary>
        /// Cache of generated item lists keyed by the resolved (non-nullable) enum type.  Enum
        /// metadata is static for the lifetime of the application, so the reflection cost is paid
        /// once per enum type regardless of how many controls display it.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, IReadOnlyList<EnumComboBoxItem>> ItemCache = new();

        /// <summary>
        /// The enum <see cref="Type"/> whose declared members populate the drop-down.
        /// </summary>
        public static readonly DependencyProperty EnumTypeProperty = DependencyProperty.Register(
            nameof(EnumType), typeof(Type), typeof(EnumComboBox),
            new FrameworkPropertyMetadata(null, OnEnumTypeChanged), IsValidEnumType);

        /// <summary>
        /// Gets or sets the enum type whose declared members populate the drop-down.  Nullable
        /// enum types are resolved to their underlying enum type.  Setting <c>null</c> clears the
        /// generated items; assigning a non-enum type throws an <see cref="ArgumentException"/>.
        /// </summary>
        [Category("Common")]
        [Description("The enum type whose declared members populate the drop-down.")]
        public Type? EnumType
        {
            get => (Type?)GetValue(EnumTypeProperty);
            set => SetValue(EnumTypeProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EnumComboBox"/>.  Selection plumbing is
        /// preconfigured so <c>SelectedValue</c> reads and writes the actual enum value while the
        /// drop-down displays the friendly name.
        /// </summary>
        public EnumComboBox()
        {
            SelectedValuePath = nameof(EnumComboBoxItem.Value);
            DisplayMemberPath = nameof(EnumComboBoxItem.DisplayName);

            // Pick up the Mosaic native ComboBox style when it has been opted in (Native=true);
            // otherwise this resolves to the system ComboBox style, so the control always renders
            // exactly like the standard ComboBox in the current theme.
            SetResourceReference(StyleProperty, typeof(ComboBox));
        }

        /// <summary>
        /// Validates that a value assigned to <see cref="EnumTypeProperty"/> is either
        /// <c>null</c>, an enum type, or a nullable enum type.
        /// </summary>
        /// <param name="value">The candidate property value.</param>
        private static bool IsValidEnumType(object? value)
        {
            if (value == null)
            {
                return true;
            }

            return value is Type type && (Nullable.GetUnderlyingType(type) ?? type).IsEnum;
        }

        /// <summary>
        /// Regenerates the item list when <see cref="EnumType"/> changes.
        /// </summary>
        private static void OnEnumTypeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var control = (EnumComboBox)dependencyObject;
            control.RefreshEnumItems();
        }

        /// <summary>
        /// Rebuilds <c>ItemsSource</c> from the current <see cref="EnumType"/>.  When the type is
        /// <c>null</c> the items are cleared; otherwise the (cached) generated list is assigned.
        /// An existing <c>SelectedValue</c> that is valid for the new type is retained by WPF's
        /// normal selection synchronization; otherwise the control has no selection.
        /// </summary>
        private void RefreshEnumItems()
        {
            var enumType = EnumType;

            if (enumType == null)
            {
                ClearValue(ItemsSourceProperty);
                return;
            }

            var resolvedType = Nullable.GetUnderlyingType(enumType) ?? enumType;
            ItemsSource = ItemCache.GetOrAdd(resolvedType, CreateItems);
        }

        /// <summary>
        /// Creates one <see cref="EnumComboBoxItem"/> per declared enum field, in declaration
        /// order.  Fields are reflected directly (rather than via <see cref="Enum.GetValues"/>)
        /// so aliased members with duplicate numeric values keep their own name and attributes.
        /// </summary>
        /// <param name="enumType">The resolved, non-nullable enum type.</param>
        private static IReadOnlyList<EnumComboBoxItem> CreateItems(Type enumType)
        {
            var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            var items = new EnumComboBoxItem[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                items[i] = new EnumComboBoxItem(fields[i].GetValue(null)!, GetDisplayName(fields[i]));
            }

            return new ReadOnlyCollection<EnumComboBoxItem>(items);
        }

        /// <summary>
        /// Resolves the friendly display name for an enum field: <see cref="DisplayNameAttribute"/>
        /// first, then <see cref="DescriptionAttribute"/>, then the member name.
        /// </summary>
        /// <param name="field">The enum field to resolve a display name for.</param>
        private static string GetDisplayName(FieldInfo field)
        {
            var displayName = field.GetCustomAttribute<DisplayNameAttribute>();

            if (!string.IsNullOrEmpty(displayName?.DisplayName))
            {
                return displayName.DisplayName;
            }

            var description = field.GetCustomAttribute<DescriptionAttribute>();

            if (!string.IsNullOrEmpty(description?.Description))
            {
                return description.Description;
            }

            return field.Name;
        }
    }
}
