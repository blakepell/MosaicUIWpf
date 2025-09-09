using System.Collections;
using System.Collections.Immutable;

namespace Mosaic.UI.Wpf.Collections
{
    /// <summary>
    /// Thread-safe collection. You can safely bind it to a WPF control using the property <see cref="AsObservable"/>.
    /// </summary>
    public sealed class ConcurrentObservableCollection<T> : IList<T>, IReadOnlyList<T>, IList
    {
        private readonly Dispatcher _dispatcher;
        private readonly object _lock = new();

        private ImmutableList<T> _items = ImmutableList<T>.Empty;
        private DispatchedObservableCollection<T>? _observableCollection;

        /// <summary>
        /// Thread-safe collection. You can safely bind it to a WPF control using the property <see cref="AsObservable"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        public ConcurrentObservableCollection() : this(GetCurrentDispatcher())
        {
        }

        /// <summary>
        /// Thread-safe collection. You can safely bind it to a WPF control using the property <see cref="AsObservable"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        public ConcurrentObservableCollection(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        /// <summary>
        /// Retrieves the current dispatcher for the application.
        /// </summary>
        /// <remarks>
        /// The dispatcher is responsible for managing the execution of work items in the UI thread.
        /// </remarks>
        /// <returns>The current dispatcher for the application.</returns>
        private static Dispatcher GetCurrentDispatcher()
        {
            return Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        /// <summary>
        /// When set to <see langword="true"/> AddRange and InsertRange methods raise NotifyCollectionChanged with all items instead of one event per item.
        /// </summary>
        /// <remarks>Most WPF controls doesn't support batch modifications</remarks>
        public bool SupportRangeNotifications { get; set; }

        /// <summary>
        /// Represents a thread-safe collection that can be safely bound to a WPF control using the property <see cref="AsObservable"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        public IReadOnlyObservableCollection<T> AsObservable
        {
            get
            {
                if (_observableCollection == null)
                {
                    lock (_lock)
                    {
                        _observableCollection ??= new DispatchedObservableCollection<T>(this, _dispatcher);
                    }
                }

                return _observableCollection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        /// <remarks>
        /// This property returns <c>false</c> for the <see cref="ConcurrentObservableCollection{T}"/> class,
        /// indicating that the collection can be modified.
        /// </remarks>
        /// <value><c>false</c> for the <see cref="ConcurrentObservableCollection{T}"/> class.</value>
        bool ICollection<T>.IsReadOnly => false;

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ConcurrentObservableCollection{T}"/>.
        /// </summary>
        /// <remarks>
        /// This property returns the number of elements currently in the collection.
        /// </remarks>
        public int Count => _items.Count;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        /// <remarks>
        /// This property determines whether the collection can be modified or not. If the value is <c>true</c>,
        /// the collection is read-only and cannot be modified. If the value is <c>false</c>, the collection is
        /// not read-only and can be modified.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the collection is read-only; otherwise, <c>false</c>.
        /// </returns>
        bool IList.IsReadOnly => false;

        /// <summary>
        /// Gets a value indicating whether the collection has a fixed size.
        /// </summary>
        /// <remarks>
        /// The IsFixedSize property indicates whether the collection has a fixed size. A fixed-size collection does not allow
        /// elements to be added or removed once the collection is created. The value of this property is specific to the
        /// ConcurrentObservableCollection class.
        /// </remarks>
        bool IList.IsFixedSize => false;

        /// <summary>
        /// Gets the number of elements in the <see cref="ConcurrentObservableCollection{T}"/>.
        /// </summary>
        /// <value>
        /// The number of elements in the <see cref="ConcurrentObservableCollection{T}"/>.
        /// </value>
        int ICollection.Count => this.Count;

        /// <summary>
        /// Gets an object that can be used to synchronize access to the collection.
        /// </summary>
        /// <remarks>
        /// The SyncRoot property returns an object that can be used to synchronize access to the collection. This property is provided for compatibility with the <see cref="System.Collections
        /// .ICollection.SyncRoot"/> property.
        /// In the case of the <see cref="ConcurrentObservableCollection{T}"/> class, the <see cref="SyncRoot"/> property always returns the object itself, since the collection is already thread
        /// -safe and does not require additional synchronization.
        /// </remarks>
        object ICollection.SyncRoot => ((ICollection)_items).SyncRoot;

        /// <summary>
        /// Gets a value indicating whether access to the collection is synchronized (thread-safe).
        /// </summary>
        /// <remarks>
        /// This property returns true if the collection is thread-safe; otherwise, false. A thread-safe collection
        /// can be accessed by multiple threads concurrently, and it guarantees that no corruption will occur as a result
        /// of concurrent access. However, you still need to ensure proper synchronization when accessing
        /// the elements of the collection to avoid unpredictable results.
        /// </remarks>
        bool ICollection.IsSynchronized => ((ICollection)_items).IsSynchronized;

        /// <summary>
        /// Thread-safe collection. You can safely bind it to a WPF control using the property <see cref="AsObservable"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        object? IList.this[int index]
        {
            get => this[index];
            set
            {
                AssertType(value, nameof(value));
                this[index] = (T)value!;
            }
        }

        /// <summary>
        /// Thread-safe collection. You can safely bind it to a WPF control using the property <see cref="AsObservable"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        public T this[int index]
        {
            get => _items[index];
            set
            {
                lock (_lock)
                {
                    _items = _items.SetItem(index, value);
                    _observableCollection?.EnqueueReplace(index, value);
                }
            }
        }

        /// <summary>
        /// Adds an item to the <see cref="ConcurrentObservableCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            lock (_lock)
            {
                _items = _items.Add(item);
                _observableCollection?.EnqueueAdd(item);
            }
        }

        /// <summary>
        /// Adds a range of items to the <see cref="ConcurrentObservableCollection{T}"/>.
        /// </summary>
        /// <param name="items">The items to add.</param>
        public void AddRange(params T[] items)
        {
            this.AddRange((IEnumerable<T>)items);
        }

        /// <summary>
        /// Adds a range of elements to the collection.
        /// </summary>
        /// <param name="items">The elements to add to the collection.</param>
        public void AddRange(IEnumerable<T> items)
        {
            lock (_lock)
            {
                var count = _items.Count;
                _items = _items.AddRange(items);
                if (this.SupportRangeNotifications)
                {
                    _observableCollection?.EnqueueAddRange(_items.GetRange(count, _items.Count - count));
                }
                else
                {
                    if (_observableCollection != null)
                    {
                        for (var i = count; i < _items.Count; i++)
                        {
                            _observableCollection.EnqueueAdd(_items[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Inserts a range of items into the collection at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert the items.</param>
        /// <param name="items">The items to be inserted.</param>
        public void InsertRange(int index, IEnumerable<T> items)
        {
            lock (_lock)
            {
                var count = _items.Count;
                _items = _items.InsertRange(index, items);
                var addedItemsCount = _items.Count - count;
                if (this.SupportRangeNotifications)
                {
                    _observableCollection?.EnqueueInsertRange(index, _items.GetRange(index, addedItemsCount));
                }
                else
                {
                    if (_observableCollection != null)
                    {
                        for (var i = index; i < index + addedItemsCount; i++)
                        {
                            _observableCollection.EnqueueInsert(i, _items[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _items = _items.Clear();
                _observableCollection?.EnqueueClear();
            }
        }

        /// <summary>
        /// Inserts an item at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which the item should be inserted.</param>
        /// <param name="item">The item to insert.</param>
        public void Insert(int index, T item)
        {
            lock (_lock)
            {
                _items = _items.Insert(index, item);
                _observableCollection?.EnqueueInsert(index, item);
            }
        }

        /// <summary>
        /// Removes the specified item from the collection.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>True if the item was successfully removed; otherwise, false.</returns>
        public bool Remove(T item)
        {
            lock (_lock)
            {
                var newList = _items.Remove(item);
                if (_items != newList)
                {
                    _items = newList;
                    _observableCollection?.EnqueueRemove(item);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Removes the element at the specified index from the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            lock (_lock)
            {
                _items = _items.RemoveAt(index);
                _observableCollection?.EnqueueRemoveAt(index);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ConcurrentObservableCollection{T}"/>.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ConcurrentObservableCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Finds the index of the specified item in the collection.
        /// </summary>
        /// <param name="item">The item to locate in the collection.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of the item in the collection,
        /// if found; otherwise, -1.
        /// </returns>
        public int IndexOf(T item)
        {
            return _items.IndexOf(item);
        }

        /// <summary>
        /// Determines whether the collection contains a specific element.
        /// </summary>
        /// <param name="item">The object to locate in the collection. The value can be null for reference types.</param>
        /// <returns>true if the collection contains the specified <paramref name="item"/>; otherwise, false.</returns>
        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Sorts the elements in the collection in ascending order using the default comparer.
        /// </summary>
        public void Sort()
        {
            this.Sort(comparer: null);
        }

        /// <summary>
        /// Sorts the elements in the collection in ascending order.
        /// </summary>
        public void Sort(IComparer<T>? comparer)
        {
            lock (_lock)
            {
                _items = _items.Sort(comparer);
                _observableCollection?.EnqueueReset(_items);
            }
        }

        /// <summary>
        /// Sorts the items in the <see cref="ConcurrentObservableCollection{T}"/> using a stable sorting algorithm.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="comparer">The comparer to use for comparing elements. If null, the default comparer for the type is used.</param>
        public void StableSort()
        {
            this.StableSort(comparer: null);
        }

        /// <summary>
        /// Sorts the collection in a stable manner.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="comparer">The comparer to use for sorting the elements. If null, the default comparer is used.</param>
        public void StableSort(IComparer<T>? comparer)
        {
            lock (_lock)
            {
                _items = ImmutableList.CreateRange(_items.OrderBy(item => item, comparer));
                _observableCollection?.EnqueueReset(_items);
            }
        }

        /// <summary>
        /// Adds an item to the ConcurrentObservableCollection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        int IList.Add(object? value)
        {
            AssertType(value, nameof(value));
            var item = (T)value!;
            lock (_lock)
            {
                var index = _items.Count;
                _items = _items.Add(item);
                _observableCollection?.EnqueueAdd(item);
                return index;
            }
        }

        /// <summary>
        /// Determines whether the collection contains a specific element.
        /// </summary>
        /// <param name="item">The object to locate in the collection. The value can be null for reference types.</param>
        /// <returns>true if the collection contains the specified <paramref name="item"/>; otherwise, false.</returns>
        bool IList.Contains(object? value)
        {
            AssertType(value, nameof(value));
            return this.Contains((T)value!);
        }

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        void IList.Clear()
        {
            this.Clear();
        }

        /// <summary>
        /// Finds the index of the specified item in the collection.
        /// </summary>
        /// <param name="item">The item to locate in the collection.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of the item in the collection,
        /// if found; otherwise, -1.
        /// </returns>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="item">The item to locate in the collection.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of the item in the collection,
        /// if found; otherwise, -1.
        /// </returns>
        int IList.IndexOf(object? value)
        {
            AssertType(value, nameof(value));
            return this.IndexOf((T)value!);
        }

        /// <summary>
        /// Inserts an item at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which the item should be inserted.</param>
        /// <param name="item">The item to insert.</param>
        void IList.Insert(int index, object? value)
        {
            AssertType(value, nameof(value));
            this.Insert(index, (T)value!);
        }

        void IList.Remove(object? value)
        {
            AssertType(value, nameof(value));
            this.Remove((T)value!);
        }

        /// <summary>
        /// Removes the element at the specified index from the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        void IList.RemoveAt(int index)
        {
            this.RemoveAt(index);
        }

        /// <summary>
        /// Copies the elements of the concurrent observable collection to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_items).CopyTo(array, index);
        }

        /// <summary>
        /// Asserts that the given value is of the specified type.
        /// </summary>
        /// <param name="value">The value to assert the type of.</param>
        /// <param name="argumentName">The name of the argument being asserted.</param>
        /// <exception cref="ArgumentException">Thrown if the value is not of the specified type.</exception>
        private static void AssertType(object? value, string argumentName)
        {
            if (value is null || value is T)
            {
                return;
            }

            throw new ArgumentException($"value must be of type '{typeof(T).FullName}'", argumentName);
        }
    }
}
