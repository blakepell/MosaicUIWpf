/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Keeps favorite BBS profiles clustered at the top of the directory.
    /// </summary>
    /// <remarks>
    /// The clustering is stable: profiles keep the relative order the user gave them through
    /// drag-and-drop or the sort dialog, they are simply partitioned into favorites first.  Work
    /// is coalesced onto a single background dispatcher callback so bulk operations such as the
    /// deferred startup load or a Big List import only re-cluster once.
    /// </remarks>
    internal sealed class BbsFavoriteOrganizer
    {
        private readonly AppSettings _settings;
        private readonly Dispatcher _dispatcher;
        private ObservableCollection<BbsProfile>? _profiles;
        private bool _isQueued;
        private bool _isClustering;

        private BbsFavoriteOrganizer(AppSettings settings, Dispatcher dispatcher)
        {
            _settings = settings;
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Begins watching the supplied settings and clusters the current directory.
        /// </summary>
        /// <param name="settings">The settings instance that owns the BBS directory.</param>
        /// <param name="dispatcher">The dispatcher that owns the directory collection.</param>
        public static BbsFavoriteOrganizer Attach(AppSettings settings, Dispatcher dispatcher)
        {
            var organizer = new BbsFavoriteOrganizer(settings, dispatcher);
            settings.PropertyChanged += organizer.Settings_OnPropertyChanged;
            organizer.BindCollection(settings.BbsProfiles);
            return organizer;
        }

        private void Settings_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppSettings.BbsProfiles))
            {
                BindCollection(_settings.BbsProfiles);
            }
        }

        /// <summary>
        /// Switches the watched collection, moving the item subscriptions with it.
        /// </summary>
        private void BindCollection(ObservableCollection<BbsProfile>? profiles)
        {
            if (ReferenceEquals(_profiles, profiles))
            {
                return;
            }

            if (_profiles != null)
            {
                _profiles.CollectionChanged -= Profiles_OnCollectionChanged;
                Unsubscribe(_profiles);
            }

            _profiles = profiles;

            if (_profiles == null)
            {
                return;
            }

            _profiles.CollectionChanged += Profiles_OnCollectionChanged;
            Subscribe(_profiles);
            QueueCluster();
        }

        private void Profiles_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isClustering)
            {
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // A reset does not report the old items, so re-subscribe from the current contents.
                Subscribe(_profiles);
            }
            else
            {
                Unsubscribe(e.OldItems);
                Subscribe(e.NewItems);
            }

            QueueCluster();
        }

        private void Profile_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BbsProfile.Favorite))
            {
                QueueCluster();
            }
        }

        private void Subscribe(System.Collections.IEnumerable? profiles)
        {
            if (profiles == null)
            {
                return;
            }

            foreach (BbsProfile profile in profiles)
            {
                // Guard against double subscriptions when a reset re-walks items already bound.
                profile.PropertyChanged -= Profile_OnPropertyChanged;
                profile.PropertyChanged += Profile_OnPropertyChanged;
            }
        }

        private void Unsubscribe(System.Collections.IEnumerable? profiles)
        {
            if (profiles == null)
            {
                return;
            }

            foreach (BbsProfile profile in profiles)
            {
                profile.PropertyChanged -= Profile_OnPropertyChanged;
            }
        }

        /// <summary>
        /// Schedules a single clustering pass at background priority.
        /// </summary>
        private void QueueCluster()
        {
            if (_isQueued)
            {
                return;
            }

            _isQueued = true;
            _dispatcher.InvokeAsync(Cluster, DispatcherPriority.Background);
        }

        /// <summary>
        /// Moves every favorite above the non-favorites while preserving relative order.
        /// </summary>
        private void Cluster()
        {
            _isQueued = false;

            if (_profiles is not { Count: > 1 } profiles)
            {
                return;
            }

            _isClustering = true;

            try
            {
                int insertIndex = 0;

                for (int i = 0; i < profiles.Count; i++)
                {
                    if (!profiles[i].Favorite)
                    {
                        continue;
                    }

                    if (i != insertIndex)
                    {
                        profiles.Move(i, insertIndex);
                    }

                    insertIndex++;
                }
            }
            finally
            {
                _isClustering = false;
            }
        }
    }
}
