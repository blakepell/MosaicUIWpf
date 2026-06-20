/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// An audio player control with a familiar transport layout: a centered Previous / Play-Stop / Next
    /// button row sitting above a full-width seek slider that is flanked by the current playback time and
    /// the total track length.
    /// </summary>
    /// <remarks>
    /// Playback is backed by <see cref="MediaPlayer"/> which supports the common audio formats handled by
    /// Windows Media Foundation (.mp3, .wav, .wma, .aac, etc.). The control manages an internal
    /// <see cref="Playlist"/> so that the Previous / Next buttons (and the <see cref="Previous"/> /
    /// <see cref="Next"/> helper methods) navigate between tracks. Programmatic control is available via the
    /// <see cref="Play"/>, <see cref="Stop"/>, <see cref="Pause"/>, <see cref="Next"/>, <see cref="Previous"/>
    /// and <see cref="Seek"/> methods.
    /// </remarks>
    [TemplatePart(Name = PartPreviousButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartPlayButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartStopButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartNextButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartPositionSlider, Type = typeof(Slider))]
    [DefaultEvent(nameof(PlaybackStarted))]
    public class AudioPlayer : Control
    {
        #region Template Part Names

        private const string PartPreviousButton = "PART_PreviousButton";
        private const string PartPlayButton = "PART_PlayButton";
        private const string PartStopButton = "PART_StopButton";
        private const string PartNextButton = "PART_NextButton";
        private const string PartPositionSlider = "PART_PositionSlider";

        #endregion

        #region Private Fields

        private readonly MediaPlayer _player = new();
        private readonly DispatcherTimer _timer;

        private ButtonBase? _previousButton;
        private ButtonBase? _playButton;
        private ButtonBase? _stopButton;
        private ButtonBase? _nextButton;
        private Slider? _positionSlider;

        /// <summary>True while the user is dragging the seek slider's thumb.</summary>
        private bool _isUserDragging;

        /// <summary>True while the slider value is being updated from code (timer/seek) so the
        /// <c>ValueChanged</c> handler does not treat it as a user seek.</summary>
        private bool _suppressSliderValueChanged;

        /// <summary>True while <see cref="Position"/> is being updated internally so the property
        /// changed callback does not issue a redundant seek.</summary>
        private bool _suppressPositionSeek;

        /// <summary>True once media has been opened and a duration is known.</summary>
        private bool _hasMedia;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source), typeof(Uri), typeof(AudioPlayer), new FrameworkPropertyMetadata(null, OnSourceChanged));

        /// <summary>
        /// Gets or sets the <see cref="Uri"/> of the audio file currently loaded into the player.
        /// </summary>
        [Category("Media")]
        [Description("The URI of the audio file currently loaded into the player.")]
        public Uri? Source
        {
            get => (Uri?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Playlist"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaylistProperty = DependencyProperty.Register(
            nameof(Playlist), typeof(ObservableCollection<Uri>), typeof(AudioPlayer), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the internal playlist of track sources navigated by the Previous / Next buttons.
        /// </summary>
        [Category("Media")]
        [Description("The internal playlist of track sources navigated by the Previous and Next buttons.")]
        public ObservableCollection<Uri> Playlist
        {
            get => (ObservableCollection<Uri>)GetValue(PlaylistProperty);
            set => SetValue(PlaylistProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CurrentIndex"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentIndexProperty = DependencyProperty.Register(
            nameof(CurrentIndex), typeof(int), typeof(AudioPlayer), new FrameworkPropertyMetadata(-1, OnCurrentIndexChanged));

        /// <summary>
        /// Gets or sets the zero-based index of the active track within the <see cref="Playlist"/>, or
        /// <c>-1</c> when no playlist track is selected.
        /// </summary>
        [Category("Media")]
        [Description("The zero-based index of the active track within the playlist.")]
        public int CurrentIndex
        {
            get => (int)GetValue(CurrentIndexProperty);
            set => SetValue(CurrentIndexProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Position"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            nameof(Position), typeof(TimeSpan), typeof(AudioPlayer),
            new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPositionChanged));

        /// <summary>
        /// Gets or sets the current playback position within the active track. Setting this value seeks the player.
        /// </summary>
        [Category("Media")]
        [Description("The current playback position within the active track.")]
        public TimeSpan Position
        {
            get => (TimeSpan)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        private static readonly DependencyPropertyKey DurationPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Duration), typeof(TimeSpan), typeof(AudioPlayer), new PropertyMetadata(TimeSpan.Zero));

        /// <summary>
        /// Identifies the <see cref="Duration"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DurationProperty = DurationPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the total length of the active track. Returns <see cref="TimeSpan.Zero"/> until the media has been opened.
        /// </summary>
        [Browsable(false)]
        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            private set => SetValue(DurationPropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="Volume"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(
            nameof(Volume), typeof(double), typeof(AudioPlayer), new FrameworkPropertyMetadata(0.5, OnVolumeChanged));

        /// <summary>
        /// Gets or sets the playback volume in the range <c>0.0</c> (silent) to <c>1.0</c> (full volume).
        /// </summary>
        [Category("Media")]
        [Description("The playback volume in the range 0.0 (silent) to 1.0 (full volume).")]
        public double Volume
        {
            get => (double)GetValue(VolumeProperty);
            set => SetValue(VolumeProperty, value);
        }

        private static readonly DependencyPropertyKey IsPlayingPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsPlaying), typeof(bool), typeof(AudioPlayer), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsPlaying"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPlayingProperty = IsPlayingPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether audio is currently playing. Drives the swap between the Play and Stop buttons.
        /// </summary>
        [Browsable(false)]
        public bool IsPlaying
        {
            get => (bool)GetValue(IsPlayingProperty);
            private set => SetValue(IsPlayingPropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutoPlay"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoPlayProperty = DependencyProperty.Register(
            nameof(AutoPlay), typeof(bool), typeof(AudioPlayer), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether a newly loaded <see cref="Source"/> should begin playing automatically.
        /// </summary>
        [Category("Media")]
        [Description("Whether a newly loaded source should begin playing automatically.")]
        public bool AutoPlay
        {
            get => (bool)GetValue(AutoPlayProperty);
            set => SetValue(AutoPlayProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutoAdvance"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoAdvanceProperty = DependencyProperty.Register(
            nameof(AutoAdvance), typeof(bool), typeof(AudioPlayer), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the player automatically advances to the next playlist track when
        /// the current track finishes.
        /// </summary>
        [Category("Media")]
        [Description("Whether the player automatically advances to the next playlist track when the current track finishes.")]
        public bool AutoAdvance
        {
            get => (bool)GetValue(AutoAdvanceProperty);
            set => SetValue(AutoAdvanceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(AudioPlayer), new FrameworkPropertyMetadata(new CornerRadius(6)));

        /// <summary>
        /// Gets or sets the radius applied to the corners of the player's outer border. Use the default for rounded
        /// corners or set a value of <c>0</c> for square corners.
        /// </summary>
        [Category("Appearance")]
        [Description("The radius applied to the corners of the player's outer border (0 for square corners).")]
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="PlaybackStarted"/> routed event.
        /// </summary>
        public static readonly RoutedEvent PlaybackStartedEvent = EventManager.RegisterRoutedEvent(
            nameof(PlaybackStarted), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AudioPlayer));

        /// <summary>
        /// Occurs when playback starts.
        /// </summary>
        public event RoutedEventHandler PlaybackStarted
        {
            add => AddHandler(PlaybackStartedEvent, value);
            remove => RemoveHandler(PlaybackStartedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="PlaybackStopped"/> routed event.
        /// </summary>
        public static readonly RoutedEvent PlaybackStoppedEvent = EventManager.RegisterRoutedEvent(
            nameof(PlaybackStopped), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AudioPlayer));

        /// <summary>
        /// Occurs when playback stops.
        /// </summary>
        public event RoutedEventHandler PlaybackStopped
        {
            add => AddHandler(PlaybackStoppedEvent, value);
            remove => RemoveHandler(PlaybackStoppedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="MediaOpened"/> routed event.
        /// </summary>
        public static readonly RoutedEvent MediaOpenedEvent = EventManager.RegisterRoutedEvent(
            nameof(MediaOpened), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AudioPlayer));

        /// <summary>
        /// Occurs when a track has been opened and its duration is known.
        /// </summary>
        public event RoutedEventHandler MediaOpened
        {
            add => AddHandler(MediaOpenedEvent, value);
            remove => RemoveHandler(MediaOpenedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="MediaEnded"/> routed event.
        /// </summary>
        public static readonly RoutedEvent MediaEndedEvent = EventManager.RegisterRoutedEvent(
            nameof(MediaEnded), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AudioPlayer));

        /// <summary>
        /// Occurs when the active track reaches the end.
        /// </summary>
        public event RoutedEventHandler MediaEnded
        {
            add => AddHandler(MediaEndedEvent, value);
            remove => RemoveHandler(MediaEndedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="TrackChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent TrackChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(TrackChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AudioPlayer));

        /// <summary>
        /// Occurs when the active <see cref="Source"/> changes (for example via Previous / Next navigation).
        /// </summary>
        public event RoutedEventHandler TrackChanged
        {
            add => AddHandler(TrackChangedEvent, value);
            remove => RemoveHandler(TrackChangedEvent, value);
        }

        #endregion

        /// <summary>
        /// Initializes static metadata for the <see cref="AudioPlayer"/> class.
        /// </summary>
        static AudioPlayer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AudioPlayer), new FrameworkPropertyMetadata(typeof(AudioPlayer)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlayer"/> class.
        /// </summary>
        public AudioPlayer()
        {
            Playlist = new ObservableCollection<Uri>();
            _player.Volume = Volume;
            _player.MediaOpened += OnPlayerMediaOpened;
            _player.MediaEnded += OnPlayerMediaEnded;
            _player.MediaFailed += OnPlayerMediaFailed;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _timer.Tick += OnTimerTick;

            Unloaded += (_, _) => _timer.Stop();
        }

        #region Template

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            DetachTemplateParts();
            base.OnApplyTemplate();

            _previousButton = GetTemplateChild(PartPreviousButton) as ButtonBase;
            _playButton = GetTemplateChild(PartPlayButton) as ButtonBase;
            _stopButton = GetTemplateChild(PartStopButton) as ButtonBase;
            _nextButton = GetTemplateChild(PartNextButton) as ButtonBase;
            _positionSlider = GetTemplateChild(PartPositionSlider) as Slider;

            if (_previousButton != null)
            {
                _previousButton.Click += OnPreviousButtonClick;
            }

            if (_playButton != null)
            {
                _playButton.Click += OnPlayButtonClick;
            }

            if (_stopButton != null)
            {
                _stopButton.Click += OnStopButtonClick;
            }

            if (_nextButton != null)
            {
                _nextButton.Click += OnNextButtonClick;
            }

            if (_positionSlider != null)
            {
                _positionSlider.Maximum = Duration.TotalSeconds;
                _positionSlider.ValueChanged += OnSliderValueChanged;
                _positionSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnSliderDragStarted));
                _positionSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnSliderDragCompleted));
            }
        }

        private void DetachTemplateParts()
        {
            if (_previousButton != null)
            {
                _previousButton.Click -= OnPreviousButtonClick;
                _previousButton = null;
            }

            if (_playButton != null)
            {
                _playButton.Click -= OnPlayButtonClick;
                _playButton = null;
            }

            if (_stopButton != null)
            {
                _stopButton.Click -= OnStopButtonClick;
                _stopButton = null;
            }

            if (_nextButton != null)
            {
                _nextButton.Click -= OnNextButtonClick;
                _nextButton = null;
            }

            if (_positionSlider != null)
            {
                _positionSlider.ValueChanged -= OnSliderValueChanged;
                _positionSlider.RemoveHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnSliderDragStarted));
                _positionSlider.RemoveHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnSliderDragCompleted));
                _positionSlider = null;
            }
        }

        /// <inheritdoc/>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new AudioPlayerAutomationPeer(this);
        }

        #endregion

        #region Public Helper Methods

        /// <summary>
        /// Begins (or resumes) playback of the active <see cref="Source"/>.
        /// </summary>
        public void Play()
        {
            if (Source == null)
            {
                return;
            }

            _player.Play();
            IsPlaying = true;
            _timer.Start();
            RaiseEvent(new RoutedEventArgs(PlaybackStartedEvent, this));
        }

        /// <summary>
        /// Pauses playback, leaving the current <see cref="Position"/> intact so that <see cref="Play"/> resumes from
        /// the same point.
        /// </summary>
        public void Pause()
        {
            _player.Pause();
            IsPlaying = false;
            _timer.Stop();
            RaiseEvent(new RoutedEventArgs(PlaybackStoppedEvent, this));
        }

        /// <summary>
        /// Stops playback while retaining the current <see cref="Position"/> so that a subsequent call to
        /// <see cref="Play"/> resumes from the same point.
        /// </summary>
        public void Stop()
        {
            _player.Pause();
            IsPlaying = false;
            _timer.Stop();
            UpdatePosition(_player.Position);
            RaiseEvent(new RoutedEventArgs(PlaybackStoppedEvent, this));
        }

        /// <summary>
        /// Advances to the next track in the <see cref="Playlist"/>, if one exists.
        /// </summary>
        public void Next()
        {
            if (Playlist == null || Playlist.Count == 0)
            {
                return;
            }

            int next = CurrentIndex + 1;

            if (next >= Playlist.Count)
            {
                return;
            }

            bool wasPlaying = IsPlaying;
            CurrentIndex = next;

            if (wasPlaying)
            {
                Play();
            }
        }

        /// <summary>
        /// Moves to the previous track in the <see cref="Playlist"/>. If playback is past the first few seconds of the
        /// current track it restarts the current track instead, matching common audio-player behavior.
        /// </summary>
        public void Previous()
        {
            if (Playlist == null || Playlist.Count == 0)
            {
                return;
            }

            // If we are more than 3 seconds into the track, restart the current track rather than skipping back.
            if (Position > TimeSpan.FromSeconds(3))
            {
                Seek(TimeSpan.Zero);
                return;
            }

            int previous = CurrentIndex - 1;

            if (previous < 0)
            {
                Seek(TimeSpan.Zero);
                return;
            }

            bool wasPlaying = IsPlaying;
            CurrentIndex = previous;

            if (wasPlaying)
            {
                Play();
            }
        }

        /// <summary>
        /// Seeks the active track to the specified <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position to seek to. Values are clamped to the track's duration.</param>
        public void Seek(TimeSpan position)
        {
            if (position < TimeSpan.Zero)
            {
                position = TimeSpan.Zero;
            }

            if (_hasMedia && position > Duration)
            {
                position = Duration;
            }

            _player.Position = position;
            UpdatePosition(position);
        }

        #endregion

        #region Source / Playlist Handling

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AudioPlayer)d).LoadSource();
        }

        private void LoadSource()
        {
            _timer.Stop();
            IsPlaying = false;
            _hasMedia = false;
            Duration = TimeSpan.Zero;
            UpdatePosition(TimeSpan.Zero);

            if (_positionSlider != null)
            {
                _positionSlider.Maximum = 0;
            }

            if (Source == null)
            {
                _player.Close();
                return;
            }

            _player.Open(Source);
            RaiseEvent(new RoutedEventArgs(TrackChangedEvent, this));

            if (AutoPlay)
            {
                Play();
            }
        }

        private static void OnCurrentIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var player = (AudioPlayer)d;
            int index = (int)e.NewValue;

            if (player.Playlist != null && index >= 0 && index < player.Playlist.Count)
            {
                player.Source = player.Playlist[index];
            }
        }

        #endregion

        #region Property Changed Callbacks

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var player = (AudioPlayer)d;

            if (player._suppressPositionSeek)
            {
                return;
            }

            player.Seek((TimeSpan)e.NewValue);
        }

        private static void OnVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var player = (AudioPlayer)d;
            double volume = (double)e.NewValue;
            player._player.Volume = Math.Clamp(volume, 0.0, 1.0);
        }

        #endregion

        #region MediaPlayer Events

        private void OnPlayerMediaOpened(object? sender, EventArgs e)
        {
            _hasMedia = true;
            Duration = _player.NaturalDuration.HasTimeSpan ? _player.NaturalDuration.TimeSpan : TimeSpan.Zero;

            if (_positionSlider != null)
            {
                _positionSlider.Maximum = Duration.TotalSeconds;
            }

            RaiseEvent(new RoutedEventArgs(MediaOpenedEvent, this));
        }

        private void OnPlayerMediaEnded(object? sender, EventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(MediaEndedEvent, this));

            bool canAdvance = AutoAdvance && Playlist != null && CurrentIndex >= 0 && CurrentIndex < Playlist.Count - 1;

            if (canAdvance)
            {
                int next = CurrentIndex + 1;
                CurrentIndex = next;
                Play();
            }
            else
            {
                Stop();
                Seek(TimeSpan.Zero);
            }
        }

        private void OnPlayerMediaFailed(object? sender, ExceptionEventArgs e)
        {
            _hasMedia = false;
            IsPlaying = false;
            _timer.Stop();
        }

        #endregion

        #region Timer / Slider

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (!_hasMedia || _isUserDragging)
            {
                return;
            }

            UpdatePosition(_player.Position);
        }

        /// <summary>
        /// Updates the reported <see cref="Position"/> and the seek slider without triggering a seek back into the player.
        /// </summary>
        private void UpdatePosition(TimeSpan position)
        {
            _suppressPositionSeek = true;
            Position = position;
            _suppressPositionSeek = false;

            if (_positionSlider != null)
            {
                _suppressSliderValueChanged = true;
                _positionSlider.Value = position.TotalSeconds;
                _suppressSliderValueChanged = false;
            }
        }

        private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderValueChanged)
            {
                return;
            }

            // A user-initiated change (track click or thumb drag) seeks the player.
            Seek(TimeSpan.FromSeconds(e.NewValue));
        }

        private void OnSliderDragStarted(object sender, DragStartedEventArgs e)
        {
            _isUserDragging = true;
        }

        private void OnSliderDragCompleted(object sender, DragCompletedEventArgs e)
        {
            _isUserDragging = false;

            if (_positionSlider != null)
            {
                Seek(TimeSpan.FromSeconds(_positionSlider.Value));
            }
        }

        #endregion

        #region Button Events

        private void OnPreviousButtonClick(object sender, RoutedEventArgs e) => Previous();

        private void OnPlayButtonClick(object sender, RoutedEventArgs e) => Play();

        private void OnStopButtonClick(object sender, RoutedEventArgs e) => Stop();

        private void OnNextButtonClick(object sender, RoutedEventArgs e) => Next();

        #endregion
    }
}
