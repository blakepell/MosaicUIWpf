using System;
using System.Collections.Generic;

namespace AvalonDock.Controls
{
    /// <summary>
    /// Represents the weak dictionary.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    internal sealed class WeakDictionary<TKey, TValue>
        where TKey : class
    {
        private readonly List<WeakReference<TKey>> _keys = new();
        private readonly List<TValue> _values = new();

        /// <summary>
        /// Gets or sets the value associated with the specified index.
        /// </summary>
        /// <param name="key">The key.</param>
        public TValue this[TKey key]
        {
            get
            {
                if (!GetValue(key, out var value))
                {
                    throw new ArgumentException();
                }

                return value;
            }
            set => SetValue(key, value);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void SetValue(TKey key, TValue value)
        {
            CollectGarbage();
            var index = FindKeyIndex(key);
            if (index >= 0)
            {
                _values[index] = value;
            }
            else
            {
                _values.Add(value);
                _keys.Add(new WeakReference<TKey>(key));
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if the operation for get value succeeds; otherwise, false.</returns>
        public bool GetValue(TKey key, out TValue value)
        {
            CollectGarbage();
            var index = FindKeyIndex(key);
            if (index < 0)
            {
                value = default!;
                return false;
            }

            value = _values[index];
            return true;
        }

        private int FindKeyIndex(TKey key)
        {
            return _keys.FindIndex(reference =>
                reference.TryGetTarget(out var target) && ReferenceEquals(target, key));
        }

        /// <summary>
        /// Removes all entries where the key has already been garbage collected.
        /// </summary>
        private void CollectGarbage()
        {
            for (var index = _keys.Count - 1; index >= 0; index--)
            {
                if (!_keys[index].TryGetTarget(out _))
                {
                    _keys.RemoveAt(index);
                    _values.RemoveAt(index);
                }
            }
        }
    }
}
