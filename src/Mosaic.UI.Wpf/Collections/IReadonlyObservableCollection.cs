using System.Collections.Specialized;

namespace Mosaic.UI.Wpf.Collections
{
    public interface IReadOnlyObservableCollection<T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
    }
}
