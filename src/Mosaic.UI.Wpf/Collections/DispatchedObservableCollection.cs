using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;

namespace Mosaic.UI.Wpf.Collections
{

    internal sealed class DispatchedObservableCollection<T> : ObservableCollectionBase<T>, IReadOnlyObservableCollection<T>, IList<T>, IList
    {
        private readonly ConcurrentQueue<PendingEvent<T>> _pendingEvents = new();
        private readonly ConcurrentObservableCollection<T> _collection;
        private readonly Dispatcher _dispatcher;

        private bool _isDispatcherPending;

        public DispatchedObservableCollection(ConcurrentObservableCollection<T> collection, Dispatcher dispatcher)
            : base(collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        private void AssertIsOnDispatcherThread()
        {
            if (!this.IsOnDispatcherThread())
            {
                var currentThreadId = Environment.CurrentManagedThreadId;
                throw new InvalidOperationException("The collection must be accessed from the dispatcher thread only. Current thread ID: " + currentThreadId.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void AssertType(object? value, string argumentName)
        {
            if (value is null || value is T)
            {
                return;
            }

            throw new ArgumentException($"value must be of type '{typeof(T).FullName}'", argumentName);
        }

        public int Count
        {
            get
            {
                this.AssertIsOnDispatcherThread();
                return this.Items.Count;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                this.AssertIsOnDispatcherThread();
                return ((ICollection<T>)_collection).IsReadOnly;
            }
        }

        int ICollection.Count
        {
            get
            {
                this.AssertIsOnDispatcherThread();
                return this.Count;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                this.AssertIsOnDispatcherThread();
                return ((ICollection)this.Items).SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                this.AssertIsOnDispatcherThread();
                return ((ICollection)this.Items).IsSynchronized;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                this.AssertIsOnDispatcherThread();
                return ((IList)this.Items).IsReadOnly;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                this.AssertIsOnDispatcherThread();
                return ((IList)this.Items).IsFixedSize;
            }
        }

        object? IList.this[int index]
        {
            get
            {
                this.AssertIsOnDispatcherThread();
                return this[index];
            }

            set
            {
                // it will immediately modify both collections as we are on the dispatcher thread
                AssertType(value, nameof(value));
                this.AssertIsOnDispatcherThread();
                _collection[index] = (T)value!;
            }
        }

        T IList<T>.this[int index]
        {
            get
            {
                this.AssertIsOnDispatcherThread();
                return this[index];
            }
            set
            {
                // it will immediately modify both collections as we are on the dispatcher thread
                this.AssertIsOnDispatcherThread();
                _collection[index] = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            this.AssertIsOnDispatcherThread();
            return this.Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.AssertIsOnDispatcherThread();
            this.Items.CopyTo(array, arrayIndex);
        }

        public int IndexOf(T item)
        {
            this.AssertIsOnDispatcherThread();
            return this.Items.IndexOf(item);
        }

        public bool Contains(T item)
        {
            this.AssertIsOnDispatcherThread();
            return this.Items.Contains(item);
        }

        public T this[int index]
        {
            get
            {
                this.AssertIsOnDispatcherThread();
                return this.Items[index];
            }
        }

        internal void EnqueueReplace(int index, T value)
        {
            this.EnqueueEvent(PendingEvent.Replace(index, value));
        }

        internal void EnqueueReset(System.Collections.Immutable.ImmutableList<T> items)
        {
            this.EnqueueEvent(PendingEvent.Reset(items));
        }

        internal void EnqueueAdd(T item)
        {
            this.EnqueueEvent(PendingEvent.Add(item));
        }

        internal void EnqueueAddRange(System.Collections.Immutable.ImmutableList<T> items)
        {
            this.EnqueueEvent(PendingEvent.AddRange(items));
        }

        internal bool EnqueueRemove(T item)
        {
            this.EnqueueEvent(PendingEvent.Remove(item));
            return true;
        }

        internal void EnqueueRemoveAt(int index)
        {
            this.EnqueueEvent(PendingEvent.RemoveAt<T>(index));
        }

        internal void EnqueueClear()
        {
            this.EnqueueEvent(PendingEvent.Clear<T>());
        }

        internal void EnqueueInsert(int index, T item)
        {
            this.EnqueueEvent(PendingEvent.Insert(index, item));
        }

        internal void EnqueueInsertRange(int index, System.Collections.Immutable.ImmutableList<T> items)
        {
            this.EnqueueEvent(PendingEvent.InsertRange(index, items));
        }

        private void EnqueueEvent(PendingEvent<T> @event)
        {
            _pendingEvents.Enqueue(@event);
            this.ProcessPendingEventsOrDispatch();
        }

        private void ProcessPendingEventsOrDispatch()
        {
            if (!this.IsOnDispatcherThread())
            {
                if (!_isDispatcherPending)
                {
                    _isDispatcherPending = true;
                    _ = _dispatcher.BeginInvoke(this.ProcessPendingEvents);
                }

                return;
            }

            this.ProcessPendingEvents();
        }

        private void ProcessPendingEvents()
        {
            _isDispatcherPending = false;
            while (_pendingEvents.TryDequeue(out var pendingEvent))
            {
                switch (pendingEvent.Type)
                {
                    case PendingEventType.Add:
                        this.AddItem(pendingEvent.Item);
                        break;

                    case PendingEventType.AddRange:
                        this.AddItems(pendingEvent.Items);
                        break;

                    case PendingEventType.Remove:
                        this.RemoveItem(pendingEvent.Item);
                        break;

                    case PendingEventType.Clear:
                        this.ClearItems();
                        break;

                    case PendingEventType.Insert:
                        this.InsertItem(pendingEvent.Index, pendingEvent.Item);
                        break;

                    case PendingEventType.InsertRange:
                        this.InsertItems(pendingEvent.Index, pendingEvent.Items);
                        break;

                    case PendingEventType.RemoveAt:
                        this.RemoveItemAt(pendingEvent.Index);
                        break;

                    case PendingEventType.Replace:
                        this.ReplaceItem(pendingEvent.Index, pendingEvent.Item);
                        break;

                    case PendingEventType.Reset:
                        this.Reset(pendingEvent.Items);
                        break;
                }
            }
        }

        private bool IsOnDispatcherThread()
        {
            return _dispatcher.Thread == Thread.CurrentThread;
        }

        void IList<T>.Insert(int index, T item)
        {
            // it will immediately modify both collections as we are on the dispatcher thread
            this.AssertIsOnDispatcherThread();
            _collection.Insert(index, item);
        }

        void IList<T>.RemoveAt(int index)
        {
            // it will immediately modify both collections as we are on the dispatcher thread
            this.AssertIsOnDispatcherThread();
            _collection.RemoveAt(index);
        }

        void ICollection<T>.Add(T item)
        {
            // it will immediately modify both collections as we are on the dispatcher thread
            this.AssertIsOnDispatcherThread();
            _collection.Add(item);
        }

        void ICollection<T>.Clear()
        {
            // it will immediately modify both collections as we are on the dispatcher thread
            this.AssertIsOnDispatcherThread();
            _collection.Clear();
        }

        bool ICollection<T>.Remove(T item)
        {
            // it will immediately modify both collections as we are on the dispatcher thread
            this.AssertIsOnDispatcherThread();
            return _collection.Remove(item);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)this.Items).CopyTo(array, index);
        }

        int IList.Add(object? value)
        {
            // it will immediately modify both collections as we are on the dispatcher thread
            AssertType(value, nameof(value));
            this.AssertIsOnDispatcherThread();
            return ((IList)_collection).Add(value);
        }

        bool IList.Contains(object? value)
        {
            // it will immediately modify both collections as we are on the dispatcher thread
            AssertType(value, nameof(value));
            this.AssertIsOnDispatcherThread();
            return ((IList)_collection).Contains(value);
        }

        void IList.Clear()
        {
            // it will immediately modify both collections as we are on the dispatcher thread
            this.AssertIsOnDispatcherThread();
            ((IList)_collection).Clear();
        }

        int IList.IndexOf(object? value)
        {
            // it will immediately modify both collections as we are on the dispatcher thread
            AssertType(value, nameof(value));
            this.AssertIsOnDispatcherThread();
            return this.Items.IndexOf((T)value!);
        }

        void IList.Insert(int index, object? value)
        {
            // it will immediately modify both collections as we are on the dispatcher thread
            AssertType(value, nameof(value));
            this.AssertIsOnDispatcherThread();
            ((IList)_collection).Insert(index, value);
        }

        void IList.Remove(object? value)
        {
            // it will immediately modify both collections as we are on the dispatcher thread
            AssertType(value, nameof(value));
            this.AssertIsOnDispatcherThread();
            ((IList)_collection).Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            // it will immediately modify both collections as we are on the dispatcher thread
            this.AssertIsOnDispatcherThread();
            ((IList)_collection).RemoveAt(index);
        }
    }
}