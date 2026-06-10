using AvalonDock.Core;
using AvalonDock.Interfaces;
using AvalonDock.Layout;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace AvalonDock
{
    /// <summary>
    /// Represents the layout Sync Bridge.
    /// </summary>
    internal sealed class LayoutSyncBridge
    {
        private readonly DockingManager _manager;
        private readonly IRootDock _rootDock;
        private ObservableCollection<object> _documentModels;
        private ObservableCollection<object> _anchorableModels;
        private bool _isSyncing;
        private readonly Dictionary<object, AnchorSide> _contentToSide = new(ReferenceEqualityComparer.Default);
        private readonly List<(INotifyCollectionChanged Source, NotifyCollectionChangedEventHandler Handler)> _collectionSubscriptions = [];
        private readonly List<INotifyPropertyChanged> _dockPropertySubscriptions = [];

        /// <summary>
        /// Gets a read-only view of the content-to-side mapping populated during tree walk.
        /// </summary>
        internal IReadOnlyDictionary<object, AnchorSide> ContentToSideMap => _contentToSide;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutSyncBridge"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="rootDock">The root Dock.</param>
        public LayoutSyncBridge(DockingManager manager, IRootDock rootDock)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _rootDock = rootDock ?? throw new ArgumentNullException(nameof(rootDock));
        }

        /// <summary>
        /// Executes the attach operation.
        /// </summary>
        public void Attach()
        {
            _documentModels = [];
            _anchorableModels = [];

            CollectDockables(_rootDock);

            _manager.DocumentsSource = _documentModels;
            _manager.AnchorablesSource = _anchorableModels;

            SyncActiveContentToWpf();

            SubscribeToMvvm();
            SubscribeToWpf();
        }

        /// <summary>
        /// Executes the detach operation.
        /// </summary>
        public void Detach()
        {
            UnsubscribeFromWpf();
            UnsubscribeFromMvvm();

            _manager.DocumentsSource = null;
            _manager.AnchorablesSource = null;

            _contentToSide.Clear();
            _documentModels = null;
            _anchorableModels = null;
        }

        private void CollectDockables(IDockable node)
        {
            SubscribeDockPropertyChanges(node);

            if (node is IDocumentDock { VisibleDockables: not null } docDock)
            {
                foreach (var child in docDock.VisibleDockables)
                {
                    if (!ContainsReference(_documentModels, child))
                    {
                        _documentModels.Add(child);
                    }
                }

                SubscribeCollection(docDock.VisibleDockables, OnDocumentCollectionChanged);
            }
            else if (node is IToolDock { VisibleDockables: not null } toolDock)
            {
                var side = AlignmentToAnchorSide(toolDock.Alignment);
                foreach (var child in toolDock.VisibleDockables)
                {
                    if (!ContainsReference(_anchorableModels, child))
                    {
                        _anchorableModels.Add(child);
                    }

                    _contentToSide[child] = side;
                }

                SubscribeCollection(toolDock.VisibleDockables, OnAnchorableCollectionChanged);
            }
            else if (node is IDock { VisibleDockables: not null } containerDock)
            {
                SubscribeCollection(containerDock.VisibleDockables, OnDockStructureChanged);
            }

            if (node is IDock { VisibleDockables: not null } dock)
            {
                foreach (var child in dock.VisibleDockables)
                {
                    if (child is IDock)
                    {
                        CollectDockables(child);
                    }
                }
            }
        }

        private void SubscribeCollection(IList<IDockable> list, NotifyCollectionChangedEventHandler handler)
        {
            if (list is INotifyCollectionChanged ncc)
            {
                if (_collectionSubscriptions.Any(subscription =>
                    ReferenceEquals(subscription.Source, ncc) && subscription.Handler == handler))
                {
                    return;
                }

                ncc.CollectionChanged += handler;
                _collectionSubscriptions.Add((ncc, handler));
            }
        }

        private void UnsubscribeAllCollections()
        {
            foreach (var subscription in _collectionSubscriptions)
            {
                subscription.Source.CollectionChanged -= subscription.Handler;
            }

            _collectionSubscriptions.Clear();
        }

        private void SubscribeDockPropertyChanges(IDockable dockable)
        {
            if (dockable is not IDock || dockable is not INotifyPropertyChanged notifier)
            {
                return;
            }

            if (_dockPropertySubscriptions.Any(subscription => ReferenceEquals(subscription, notifier)))
            {
                return;
            }

            notifier.PropertyChanged += OnDockPropertyChanged;
            _dockPropertySubscriptions.Add(notifier);
        }

        private void UnsubscribeDockPropertyChanges()
        {
            foreach (var notifier in _dockPropertySubscriptions)
            {
                notifier.PropertyChanged -= OnDockPropertyChanged;
            }

            _dockPropertySubscriptions.Clear();
        }

        private void SubscribeToMvvm()
        {
            if (_rootDock is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += OnRootDockPropertyChanged;
            }
        }

        private void UnsubscribeFromMvvm()
        {
            if (_rootDock is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged -= OnRootDockPropertyChanged;
            }

            UnsubscribeAllCollections();
            UnsubscribeDockPropertyChanges();
        }

        private void SubscribeToWpf()
        {
            _manager.ActiveContentChanged += OnWpfActiveContentChanged;
            _manager.DocumentClosed += OnWpfDocumentClosed;
        }

        private void UnsubscribeFromWpf()
        {
            _manager.ActiveContentChanged -= OnWpfActiveContentChanged;
            _manager.DocumentClosed -= OnWpfDocumentClosed;
        }

        private void OnDocumentCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isSyncing || _documentModels == null)
            {
                return;
            }

            _isSyncing = true;
            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var item in e.NewItems)
                        {
                            if (!ContainsReference(_documentModels, item))
                            {
                                _documentModels.Add(item);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (var item in e.OldItems)
                        {
                            RemoveReference(_documentModels, item);
                        }

                        break;
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Reset:
                        RebuildDocuments();
                        break;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void OnAnchorableCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isSyncing || _anchorableModels == null)
            {
                return;
            }

            _isSyncing = true;
            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        var addSide = FindSideForSender(sender);
                        foreach (var item in e.NewItems)
                        {
                            if (!ContainsReference(_anchorableModels, item))
                            {
                                _anchorableModels.Add(item);
                            }

                            if (addSide.HasValue)
                            {
                                _contentToSide[item] = addSide.Value;
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (var item in e.OldItems)
                        {
                            RemoveReference(_anchorableModels, item);
                            _contentToSide.Remove(item);
                        }

                        break;
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Reset:
                        RebuildAnchorables();
                        break;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void OnDockStructureChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isSyncing)
            {
                return;
            }

            _isSyncing = true;
            try
            {
                RebuildAllFromTree();
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void OnDockPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isSyncing ||
                (e.PropertyName != nameof(IDock.VisibleDockables) &&
                 e.PropertyName != nameof(IToolDock.Alignment)))
            {
                return;
            }

            _isSyncing = true;
            try
            {
                RebuildAllFromTree();
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void OnRootDockPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isSyncing)
            {
                return;
            }

            if (e.PropertyName == nameof(IRootDock.ActiveDockable)
                || e.PropertyName == nameof(IDock.ActiveDockable))
            {
                SyncActiveContentToWpf();
            }
        }

        private void OnWpfActiveContentChanged(object sender, EventArgs e)
        {
            if (_isSyncing)
            {
                return;
            }

            _isSyncing = true;
            try
            {
                var active = _manager.ActiveContent;
                if (active is IDockable dockable)
                {
                    _rootDock.ActiveDockable = dockable;
                }
                else
                {
                    FindAndSetActiveDockable(active);
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void OnWpfDocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            if (_isSyncing)
            {
                return;
            }

            _isSyncing = true;
            try
            {
                var content = e.Document.Content;
                RemoveReference(_documentModels, content);

                if (content is IDockable dockable)
                {
                    RemoveDockableFromTree(_rootDock, dockable, isDocument: true);
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private bool RemoveDockableFromTree(IDockable node, IDockable target, bool isDocument)
        {
            if (isDocument && node is IDocumentDock docDock)
            {
                if (RemoveReference(docDock.VisibleDockables, target))
                {
                    return true;
                }
            }

            if (!isDocument && node is IToolDock toolDock)
            {
                if (RemoveReference(toolDock.VisibleDockables, target))
                {
                    return true;
                }
            }

            if (node is IDock { VisibleDockables: not null } dock)
            {
                foreach (var child in dock.VisibleDockables)
                {
                    if (child is IDock)
                    {
                        if (RemoveDockableFromTree(child, target, isDocument))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void SyncActiveContentToWpf()
        {
            if (_isSyncing)
            {
                return;
            }

            _isSyncing = true;
            try
            {
                var activeDockable = _rootDock.ActiveDockable;
                if (activeDockable != null)
                {
                    _manager.ActiveContent = activeDockable;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void FindAndSetActiveDockable(object content)
        {
            if (content == null)
            {
                _rootDock.ActiveDockable = null;
                return;
            }

            var match = _documentModels?.OfType<IDockable>().FirstOrDefault(d => d.Context == content)
                ?? _anchorableModels?.OfType<IDockable>().FirstOrDefault(d => d.Context == content);

            _rootDock.ActiveDockable = match;
        }

        private void RebuildDocuments()
        {
            _documentModels.Clear();
            RebuildDocumentsFromTree(_rootDock);
        }

        private void RebuildDocumentsFromTree(IDockable node)
        {
            if (node is IDocumentDock { VisibleDockables: not null } docDock)
            {
                foreach (var child in docDock.VisibleDockables)
                {
                    if (!ContainsReference(_documentModels, child))
                    {
                        _documentModels.Add(child);
                    }
                }
            }

            if (node is IDock { VisibleDockables: not null } dock)
            {
                foreach (var child in dock.VisibleDockables)
                {
                    if (child is IDock)
                    {
                        RebuildDocumentsFromTree(child);
                    }
                }
            }
        }

        private void RebuildAnchorables()
        {
            _anchorableModels.Clear();
            _contentToSide.Clear();
            RebuildAnchorablesFromTree(_rootDock);
        }

        private void RebuildAnchorablesFromTree(IDockable node)
        {
            if (node is IToolDock { VisibleDockables: not null } toolDock)
            {
                var side = AlignmentToAnchorSide(toolDock.Alignment);
                foreach (var child in toolDock.VisibleDockables)
                {
                    if (!ContainsReference(_anchorableModels, child))
                    {
                        _anchorableModels.Add(child);
                    }

                    _contentToSide[child] = side;
                }
            }

            if (node is IDock { VisibleDockables: not null } dock)
            {
                foreach (var child in dock.VisibleDockables)
                {
                    if (child is IDock)
                    {
                        RebuildAnchorablesFromTree(child);
                    }
                }
            }
        }

        private AnchorSide? FindSideForSender(object sender)
        {
            return FindToolDockForCollection(_rootDock, sender) is IToolDock td
                ? AlignmentToAnchorSide(td.Alignment)
                : null;
        }

        private static IToolDock FindToolDockForCollection(IDockable node, object sender)
        {
            if (node is IToolDock toolDock && toolDock.VisibleDockables == sender)
            {
                return toolDock;
            }

            if (node is IDock { VisibleDockables: not null } dock)
            {
                foreach (var child in dock.VisibleDockables)
                {
                    if (child is IDock)
                    {
                        var found = FindToolDockForCollection(child, sender);
                        if (found != null)
                        {
                            return found;
                        }
                    }
                }
            }

            return null;
        }

        private void RebuildAllFromTree()
        {
            UnsubscribeAllCollections();
            UnsubscribeDockPropertyChanges();
            _documentModels.Clear();
            _anchorableModels.Clear();
            _contentToSide.Clear();
            CollectDockables(_rootDock);
        }

        private static bool ContainsReference<T>(IEnumerable<T> items, object item)
            where T : class
        {
            return items != null && items.Any(candidate => ReferenceEquals(candidate, item));
        }

        private static bool RemoveReference<T>(IList<T> items, object item)
            where T : class
        {
            if (items == null)
            {
                return false;
            }

            for (var index = 0; index < items.Count; index++)
            {
                if (ReferenceEquals(items[index], item))
                {
                    items.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        private static AnchorSide AlignmentToAnchorSide(DockAlignment alignment)
        {
            switch (alignment)
            {
                case DockAlignment.Left: return AnchorSide.Left;
                case DockAlignment.Right: return AnchorSide.Right;
                case DockAlignment.Top: return AnchorSide.Top;
                case DockAlignment.Bottom: return AnchorSide.Bottom;
                default: return AnchorSide.Right;
            }
        }
    }
}
