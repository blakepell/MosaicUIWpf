using System.Collections.Immutable;

namespace Mosaic.UI.Wpf.Collections
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct PendingEvent<T>
    {
        public PendingEvent(PendingEventType type)
        {
            this.Type = type;
            this.Item = default!;
            this.Index = -1;
            this.Items = default;
        }

        public PendingEvent(PendingEventType type, int index)
        {
            this.Type = type;
            this.Item = default!;
            this.Index = index;
            this.Items = default;
        }

        public PendingEvent(PendingEventType type, T item)
        {
            this.Type = type;
            this.Item = item;
            this.Index = -1;
            this.Items = default;
        }

        public PendingEvent(PendingEventType type, T item, int index)
        {
            this.Type = type;
            this.Item = item;
            this.Index = index;
            this.Items = default;
        }

        public PendingEvent(PendingEventType type, ImmutableList<T> items)
        {
            this.Type = type;
            this.Items = items;
            this.Item = default!;
            this.Index = default;
        }

        public PendingEvent(PendingEventType type, ImmutableList<T> items, int index)
        {
            this.Type = type;
            this.Items = items;
            this.Item = default!;
            this.Index = index;
        }

        public PendingEventType Type { get; }
        public T Item { get; }
        public int Index { get; }
        public ImmutableList<T>? Items { get; }
    }

    internal enum PendingEventType
    {
        Add,
        AddRange,
        Insert,
        InsertRange,
        Remove,
        RemoveAt,
        Replace,
        Clear,
        Reset,
    }

    internal static class PendingEvent
    {
        public static PendingEvent<T> Add<T>(T item) => new(PendingEventType.Add, item);

        public static PendingEvent<T> AddRange<T>(ImmutableList<T> items) => new(PendingEventType.AddRange, items);

        public static PendingEvent<T> Insert<T>(int index, T item) => new(PendingEventType.Insert, item, index);

        public static PendingEvent<T> InsertRange<T>(int index, ImmutableList<T> items) => new(PendingEventType.InsertRange, items, index);

        public static PendingEvent<T> Remove<T>(T item) => new(PendingEventType.Remove, item);

        public static PendingEvent<T> RemoveAt<T>(int index) => new(PendingEventType.RemoveAt, index);

        public static PendingEvent<T> Replace<T>(int index, T item) => new(PendingEventType.Replace, item, index);

        public static PendingEvent<T> Clear<T>() => new(PendingEventType.Clear);

        public static PendingEvent<T> Reset<T>(ImmutableList<T> items) => new(PendingEventType.Reset, items);
    }
}
