/*
 * The RadialProgressBar control derives from:
 *
 * Alexander Smirnov
 * https://github.com/panthernet/XamlRadialProgressBar
 * Apache-2.0 license
 */

using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Threading;

namespace MosaicWpfDemo.Views.Examples
{
    [ObservableObject]
    public partial class RadialProgressBarExample
    {
        private bool _timersInitialized = false;

        [ObservableProperty]
        private double _value1;

        [ObservableProperty]
        private double _value2;

        [ObservableProperty]
        private double _value3;

        [ObservableProperty]
        private double _value4;

        public RadialProgressBarExample()
        {
            InitializeComponent();
            DataContext = this;

            Loaded += (_, _) => InitializeTimers();
        }

        private void InitializeTimers()
        {
            if (_timersInitialized)
            {
                return;
            }

            StartTimer(100, () => Value1 = Value1 >= 100 ? 0 : Value1 + 1);
            StartTimer(150, () => Value2 = Value2 >= 100 ? 0 : Value2 + 2);
            StartTimer(200, () => Value3 = Value3 >= 100 ? 0 : Value3 + 2);
            StartTimer(100, () => Value4 = Value4 >= 100 ? 0 : Value4 + 3);

            _timersInitialized = true;
        }

        private void StartTimer(int intervalMilliseconds, System.Action onTick)
        {
            var timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = System.TimeSpan.FromMilliseconds(intervalMilliseconds)
            };

            timer.Tick += (_, _) => onTick();
            timer.Start();
        }
    }
}
