using AvalonDock.Layout;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AvalonDock.Controls
{
    /// <summary>
    /// Represents the layout Anchor Group Control.
    /// </summary>
    public class LayoutAnchorGroupControl : Control, ILayoutControl
    {
        private ObservableCollection<LayoutAnchorControl> _childViews = new();
        private LayoutAnchorGroup _model;
        private bool _isSubscribed;

        /// <summary>
        /// Initializes static members of the <see cref="LayoutAnchorGroupControl"/> class.
        /// </summary>
        static LayoutAnchorGroupControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LayoutAnchorGroupControl), new FrameworkPropertyMetadata(typeof(LayoutAnchorGroupControl)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutAnchorGroupControl"/> class.
        /// </summary>
        /// <param name="model">The model.</param>
        internal LayoutAnchorGroupControl(LayoutAnchorGroup model)
        {
            _model = model;
            CreateChildrenViews();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        public ObservableCollection<LayoutAnchorControl> Children => _childViews;

        /// <summary>
        /// Gets the model.
        /// </summary>
        public ILayoutElement Model => _model;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SubscribeToModel();
            _childViews.Clear();
            CreateChildrenViews();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeFromModel();
        }

        private void SubscribeToModel()
        {
            if (_isSubscribed)
            {
                return;
            }

            _model.Children.CollectionChanged += OnModelChildrenCollectionChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeFromModel()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _model.Children.CollectionChanged -= OnModelChildrenCollectionChanged;
            _isSubscribed = false;
        }

        private void CreateChildrenViews()
        {
            var manager = _model.Root.Manager;
            foreach (var childModel in _model.Children)
            {
                var lac = new LayoutAnchorControl(childModel);
                lac.SetBinding(TemplateProperty, new Binding(DockingManager.AnchorTemplateProperty.Name) { Source = manager });
                _childViews.Add(lac);
            }
        }

        private void OnModelChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (e.OldItems != null)
                {
                    {
                        foreach (var childModel in e.OldItems)
                        {
                            _childViews.Remove(_childViews.First(cv => cv.Model == childModel));
                        }
                    }
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _childViews.Clear();
            }

            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                _childViews.Move(e.OldStartingIndex, e.NewStartingIndex);
            }

            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (e.NewItems != null)
                {
                    var manager = _model.Root.Manager;
                    int insertIndex = e.NewStartingIndex;
                    foreach (LayoutAnchorable childModel in e.NewItems)
                    {
                        var lac = new LayoutAnchorControl(childModel);
                        lac.SetBinding(TemplateProperty, new Binding(DockingManager.AnchorTemplateProperty.Name) { Source = manager });
                        _childViews.Insert(insertIndex++, lac);
                    }
                }
            }
        }
    }
}
