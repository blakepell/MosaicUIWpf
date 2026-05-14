/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.IO.HotKey
{
    /// <summary>
    /// Represents system-wide hot key.
    /// </summary>
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public class HotKey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HotKey"/> class.
        /// </summary>
        /// <param name="key">The primary key.</param>
        /// <param name="modifiers">The primary key modifiers.</param>
        public HotKey(Key key, ModifierKeys modifiers)
        {
            this.Key = key;
            this.Modifiers = modifiers;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HotKey"/> class.
        /// </summary>
        public HotKey()
        {

        }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        public Key Key { get; set; }

        /// <summary>
        /// Gets or sets the key modifiers.
        /// </summary>
        /// <value>The key modifiers.</value>
        public ModifierKeys Modifiers { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="HotKey"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="HotKey"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="HotKey"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(HotKey other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(other.Key, this.Key) && Equals(other.Modifiers, this.Modifiers);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj) || ReferenceEquals(this, obj))
            {
                return false;
            }

            return obj.GetType() == typeof(HotKey) && this.Equals((HotKey)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures
        /// like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (this.Key.GetHashCode() * 397) ^ this.Modifiers.GetHashCode();
            }
        }
    }
}