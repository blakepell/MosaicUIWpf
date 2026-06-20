/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Describes the kind of change that is occurring (or has occurred) to the set of tags in a <see cref="TagBox"/>.
    /// </summary>
    public enum TagChangeAction
    {
        /// <summary>
        /// A tag is being, or has been, added to the collection.
        /// </summary>
        Add,

        /// <summary>
        /// A tag is being, or has been, removed from the collection.
        /// </summary>
        Remove
    }

    /// <summary>
    /// Provides data for the <see cref="TagBox.TagChanging"/> event, which is raised <b>before</b> a tag is added or
    /// removed. Set <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to <see langword="true"/> to prevent
    /// the change from taking place.
    /// </summary>
    public class TagChangingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TagChangingEventArgs"/> class.
        /// </summary>
        /// <param name="tag">The tag that is about to be added or removed.</param>
        /// <param name="action">Whether the tag is being added or removed.</param>
        public TagChangingEventArgs(string tag, TagChangeAction action)
        {
            this.Tag = tag;
            this.Action = action;
        }

        /// <summary>
        /// Gets the tag that is about to be added or removed.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// Gets a value indicating whether the tag is being added or removed.
        /// </summary>
        public TagChangeAction Action { get; }
    }

    /// <summary>
    /// Provides data for the <see cref="TagBox.TagChanged"/> event, which is raised <b>after</b> a tag has been added
    /// or removed from the collection.
    /// </summary>
    public class TagChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TagChangedEventArgs"/> class.
        /// </summary>
        /// <param name="tag">The tag that was added or removed.</param>
        /// <param name="action">Whether the tag was added or removed.</param>
        public TagChangedEventArgs(string tag, TagChangeAction action)
        {
            this.Tag = tag;
            this.Action = action;
        }

        /// <summary>
        /// Gets the tag that was added or removed.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// Gets a value indicating whether the tag was added or removed.
        /// </summary>
        public TagChangeAction Action { get; }
    }
}
