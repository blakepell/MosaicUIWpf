/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System.Collections.ObjectModel;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a control that displays a list of available drives and allows the user to select one.
    /// </summary>
    public class DriveComboBox : Control
    {
        private ComboBox? _comboBox;

        /// <summary>
        /// Initializes static members of the <see cref="DriveComboBox"/> class.
        /// </summary>
        static DriveComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DriveComboBox), new FrameworkPropertyMetadata(typeof(DriveComboBox)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DriveComboBox"/> class.
        /// </summary>
        /// <remarks>This constructor initializes the <see cref="Drives"/> collection and subscribes to
        /// the <see cref="Loaded"/> event to perform additional setup when the control is loaded.</remarks>
        public DriveComboBox()
        {
            Drives = new ObservableCollection<DriveInfo>();
            Loaded += OnLoaded;
        }

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="SelectedDrive"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedDriveProperty = DependencyProperty.Register(nameof(SelectedDrive), typeof(DriveInfo), typeof(DriveComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDriveChanged));

        /// <summary>
        /// Gets or sets the selected drive.
        /// </summary>
        public DriveInfo SelectedDrive
        {
            get => (DriveInfo)GetValue(SelectedDriveProperty);
            set => SetValue(SelectedDriveProperty, value);
        }

        /// <summary>
        /// Identifies the read-only dependency property key for the <see cref="Drives"/> property.
        /// </summary>
        private static readonly DependencyPropertyKey DrivesPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Drives), typeof(ObservableCollection<DriveInfo>), typeof(DriveComboBox),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="Drives"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DrivesProperty = DrivesPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the collection of available drives.
        /// </summary>
        public ObservableCollection<DriveInfo> Drives
        {
            get => (ObservableCollection<DriveInfo>)GetValue(DrivesProperty);
            private set => SetValue(DrivesPropertyKey, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the selected drive changes.
        /// </summary>
        public event EventHandler<DriveInfo> SelectedDriveChanged;

        /// <summary>
        /// Occurs when the drives are refreshed.
        /// </summary>
        public event EventHandler DrivesRefreshed;

        /// <summary>
        /// Occurs when an error occurs during drive enumeration.
        /// </summary>
        public event EventHandler<Exception> DriveEnumerationError;

        #endregion

        #region Methods

        /// <summary>
        /// Refreshes the list of available drives.
        /// </summary>
        public void Refresh()
        {
            try
            {
                var currentSelection = SelectedDrive;
                Drives.Clear();
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .OrderBy(d => d.Name)
                    .ToList();
                foreach (var drive in drives)
                {
                    Drives.Add(drive);
                }
                // Prefer C drive if available
                var cDrive = Drives.FirstOrDefault(d => d.Name.Equals("C:\\", StringComparison.OrdinalIgnoreCase));
                if (cDrive != null)
                {
                    SelectedDrive = cDrive;
                }
                else if (currentSelection != null)
                {
                    var matchingDrive = Drives.FirstOrDefault(d => d.Name.Equals(currentSelection.Name, StringComparison.OrdinalIgnoreCase));
                    if (matchingDrive != null)
                    {
                        SelectedDrive = matchingDrive;
                    }
                    else if (Drives.Any())
                    {
                        SelectedDrive = Drives.First();
                    }
                }
                else if (Drives.Any())
                {
                    SelectedDrive = Drives.First();
                }
                DrivesRefreshed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                DriveEnumerationError?.Invoke(this, ex);
            }
        }

        /// <summary>
        /// Invoked whenever application code or internal processes call <see cref="FrameworkElement.ApplyTemplate"/>.
        /// </summary>
        /// <remarks>This method is overridden to perform additional setup after the control's template is
        /// applied.  It initializes the <see cref="ComboBox"/> control defined in the template and attaches event
        /// handlers  for its <see cref="ComboBox.SelectionChanged"/> event. Additionally, it refreshes the control's
        /// state  to ensure it is properly initialized.</remarks>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _comboBox = GetTemplateChild("PART_ComboBox") as ComboBox;

            if (_comboBox != null)
            {
                _comboBox.SelectionChanged += OnComboBoxSelectionChanged;
            }

            // Initial load of drives
            Refresh();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the Loaded event of the control and ensures the drives collection is initialized.
        /// </summary>
        /// <remarks>
        /// If the drives collection is empty when the control is loaded, this method triggers a
        /// refresh to populate it.
        /// </remarks>
        /// <param name="sender">The source of the event, typically the control that triggered the Loaded event.</param>
        /// <param name="e">The event data associated with the Loaded event.</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // If no drives are in the list, refresh the list to populate it.
            if (Drives.Count == 0)
            {
                Refresh();
            }

            // Free the event handler to prevent memory leaks.
            Loaded -= OnLoaded;
        }

        /// <summary>
        /// Handles the <see cref="SelectionChangedEventArgs"/> event for the associated ComboBox.
        /// </summary>
        /// <remarks>This method updates the <see cref="SelectedDrive"/> property to reflect the currently
        /// selected <see cref="DriveInfo"/> in the ComboBox, if applicable.</remarks>
        /// <param name="sender">The source of the event, typically the ComboBox whose selection has changed.</param>
        /// <param name="e">The event data containing information about the selection change.</param>
        private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_comboBox is { SelectedItem: DriveInfo selectedDrive })
            {
                SelectedDrive = selectedDrive;
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="SelectedDrive"/> dependency property.
        /// </summary>
        /// <remarks>This method is invoked automatically when the <see cref="SelectedDrive"/> property
        /// changes.  It ensures that the change is propagated to the associated <see cref="DriveComboBox"/>
        /// instance.</remarks>
        /// <param name="d">The object on which the property value has changed. Must be a <see cref="DriveComboBox"/>.</param>
        /// <param name="e">Provides data about the property change, including the old and new values.</param>
        private static void OnSelectedDriveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DriveComboBox driveComboBox)
            {
                driveComboBox.OnSelectedDriveChanged(e.OldValue as DriveInfo, e.NewValue as DriveInfo);
            }
        }

        /// <summary>
        /// Handles changes to the selected drive and updates the associated UI element.
        /// </summary>
        /// <remarks>This method ensures that the UI element representing the selected drive is
        /// synchronized with the new value. It also raises the <see cref="SelectedDriveChanged"/> event to notify
        /// subscribers of the change.</remarks>
        /// <param name="oldValue">The previously selected drive, or <see langword="null"/> if no drive was previously selected.</param>
        /// <param name="newValue">The newly selected drive, or <see langword="null"/> if no drive is currently selected.</param>
        private void OnSelectedDriveChanged(DriveInfo? oldValue, DriveInfo? newValue)
        {
            if (_comboBox != null && _comboBox.SelectedItem != newValue)
            {
                _comboBox.SelectedItem = newValue;
            }

            SelectedDriveChanged?.Invoke(this, newValue);
        }

        #endregion
    }
}
