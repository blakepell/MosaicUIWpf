using System.Collections.Immutable;
using System.Collections.Specialized;

namespace Mosaic.UI.Wpf.Collections;

internal abstract class ObservableCollectionBase<T> : INotifyCollectionChanged, INotifyPropertyChanged
{
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private protected List<T> Items { get; }

    protected ObservableCollectionBase()
    {
        this.Items = new List<T>();
    }

    protected ObservableCollectionBase(IEnumerable<T>? items)
    {
        this.Items = items == null ? new List<T>() : new List<T>(items);
    }

    public void EnsureCapacity(int capacity)
    {
        this.Items.EnsureCapacity(capacity);
    }

    protected void ReplaceItem(int index, T item)
    {
        var oldItem = this.Items[index];
        this.Items[index] = item;

        this.OnIndexerPropertyChanged();
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index));
    }

    protected void InsertItem(int index, T item)
    {
        this.Items.Insert(index, item);

        this.OnCountPropertyChanged();
        this.OnIndexerPropertyChanged();
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    protected void InsertItems(int index, ImmutableList<T> items)
    {
        this.Items.InsertRange(index, items);

        this.OnCountPropertyChanged();
        this.OnIndexerPropertyChanged();
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, index));
    }

    protected void AddItem(T item)
    {
        var index = this.Items.Count;
        this.Items.Add(item);

        this.OnCountPropertyChanged();
        this.OnIndexerPropertyChanged();
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    protected void AddItems(ImmutableList<T> items)
    {
        var index = this.Items.Count;
        this.Items.AddRange(items);

        this.OnCountPropertyChanged();
        this.OnIndexerPropertyChanged();
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, index));
    }

    protected void RemoveItemAt(int index)
    {
        var item = this.Items[index];
        this.Items.RemoveAt(index);

        this.OnCountPropertyChanged();
        this.OnIndexerPropertyChanged();
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
    }

    protected bool RemoveItem(T item)
    {
        var index = this.Items.IndexOf(item);
        if (index < 0)
        {
            return false;
        }

        this.Items.RemoveAt(index);

        this.OnCountPropertyChanged();
        this.OnIndexerPropertyChanged();
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        return true;
    }

    protected void ClearItems()
    {
        this.Items.Clear();
        this.OnCountPropertyChanged();
        this.OnIndexerPropertyChanged();
        this.CollectionChanged?.Invoke(this, EventArgsCache.ResetCollectionChanged);
    }

    protected void Reset(ImmutableList<T> items)
    {
        this.Items.Clear();
        this.Items.AddRange(items);
        this.OnIndexerPropertyChanged();
        this.OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
    }

    private void OnCountPropertyChanged() => this.OnPropertyChanged(EventArgsCache.CountPropertyChanged);
    private void OnIndexerPropertyChanged() => this.OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs args) => this.PropertyChanged?.Invoke(this, args);
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args) => this.CollectionChanged?.Invoke(this, args);
}