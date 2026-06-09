/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mosaic.UI.Wpf.Controls;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MosaicDocument = Mosaic.UI.Wpf.Controls.Document;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class DocumentContainerExample
    {
        private DocumentContainerExampleViewModel ViewModel => (DocumentContainerExampleViewModel)DataContext;

        public DocumentContainerExample()
        {
            InitializeComponent();
            DataContext = new DocumentContainerExampleViewModel();
        }

        private void OnDocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            if (e.Document.Title == "Close is canceled")
            {
                e.Cancel = true;
                ViewModel.Status = "The close request was canceled by the DocumentClosing event.";
            }
        }

        private void OnDocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            ViewModel.Status = $"Closed \"{e.Document.Title}\".";
        }
    }

    /// <summary>
    /// Provides the observable state used by the document container example.
    /// </summary>
    public partial class DocumentContainerExampleViewModel : ObservableObject
    {
        private int _documentNumber = 4;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentContainerExampleViewModel"/> class.
        /// </summary>
        public DocumentContainerExampleViewModel()
        {
            Documents =
            [
                CreateDocument("Welcome", "This document contains a TextBox that stretches to fill the available area."),
                CreateDocument("Pinned document", "This tab cannot be closed.", canClose: false),
                CreateDocument("Close is canceled", "Its close request is canceled by the DocumentClosing event."),
                CreateDocument("Notes", "Additional documents demonstrate the tab overflow behavior."),
                CreateDocument("Output", "Select Wrap or Scroll in the shared header controls."),
                CreateDocument("Search results", "Wrap is the default overflow mode."),
                CreateDocument("Properties", "Scroll mode displays left and right navigation buttons."),
                CreateDocument("Preview", "The active tab uses #007ACC with a white foreground by default.")
            ];

            ActiveDocument = Documents[0];
        }

        /// <summary>
        /// Gets the collection of documents displayed by the example.
        /// </summary>
        public ObservableCollection<MosaicDocument> Documents { get; }

        /// <summary>
        /// Gets or sets the active document.
        /// </summary>
        [ObservableProperty]
        public partial MosaicDocument? ActiveDocument { get; set; }

        /// <summary>
        /// Gets or sets the status text displayed below the control.
        /// </summary>
        [ObservableProperty]
        public partial string Status { get; set; } = "Ready.";

        /// <summary>
        /// Gets or sets how overflowing document tabs are arranged.
        /// </summary>
        [ObservableProperty]
        public partial DocumentTabOverflowMode TabOverflowMode { get; set; } = DocumentTabOverflowMode.Wrap;

        [RelayCommand]
        private void AddDocument()
        {
            var title = $"Document {_documentNumber++}";
            var document = CreateDocument(title, "New documents are added through an MVVM relay command.");
            Documents.Add(document);
            ActiveDocument = document;
            Status = $"Added \"{title}\".";
        }

        [RelayCommand]
        private void SetOverflowMode(DocumentTabOverflowMode mode)
        {
            TabOverflowMode = mode;
            Status = $"Tab overflow mode changed to {mode}.";
        }

        private static MosaicDocument CreateDocument(string title, string text, bool canClose = true)
        {
            return new MosaicDocument
            {
                Title = title,
                ToolTip = $"Document: {title}",
                CanClose = canClose,
                Content = new Label
                {
                    Margin = new Thickness(0),
                    Padding = new Thickness(8),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Content = text
                }
            };
        }
    }
}
