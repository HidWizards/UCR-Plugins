using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;
using Timer = System.Timers.Timer;

namespace DeltaToButtons
{
    [Plugin("Delta to Buttons")]
    [PluginInput(DeviceBindingCategory.Delta, "Delta")]
    [PluginOutput(DeviceBindingCategory.Momentary, "Button Low")]
    [PluginOutput(DeviceBindingCategory.Momentary, "Button High")]
    public class DeltaToButtons : Plugin
    {
        [PluginGui("Min", ColumnOrder = 0, RowOrder = 0)]
        public int Min { get; set; }

        [PluginGui("Center Timeout", ColumnOrder = 1, RowOrder = 0)]
        public int CenterTimeout
        {
            get => _centerTimeout;
            set
            {
                _centerTimeout = value;
                _centerHandler.SetTime(_centerTimeout);
            }
        }

        private int _centerTimeout;

        [PluginGui("DeBounce Time", ColumnOrder = 2, RowOrder = 0)]
        public int DeBounceTimeout
        {
            get => _debounceTimeout;
            set
            {
                _debounceTimeout = value;
                _debounceHandler.SetTime(_debounceTimeout);
            }
        }

        private int _debounceTimeout;
        private int _nextState = 0;
        private readonly TimeoutAction _centerHandler;
        private readonly TimeoutAction _debounceHandler;

        public DeltaToButtons()
        {
            _debounceHandler = new TimeoutAction(OnDebounceTimeout);
            _centerHandler = new TimeoutAction(OnCenterTimeout);
            Min = 0;
            CenterTimeout = 100;
            DeBounceTimeout = 100;
        }

        private void OnCenterTimeout()
        {
            SetOutputState(0);
        }

        private void OnDebounceTimeout()
        {
            WriteOutput(_nextState);
        }

        public override void Update(params short[] values)
        {
            if (Math.Abs(values[0]) < Min) return;
            _centerHandler.SetState(true);
            if (values[0] > 0)
            {
                SetOutputState(1);
            }
            else if (values[0] < 0)
            {
                SetOutputState(-1);
            }
        }

        public override void OnActivate()
        {
            _debounceHandler.SetState(true);
        }

        public override void OnDeactivate()
        {
            _centerHandler.SetState(false);
            _debounceHandler.SetState(false);
        }

        private void SetOutputState(int state)
        {
            if (state == _nextState) return;
            _nextState = state;
            _debounceHandler.SetState(true);
        }

        private void WriteOutput(int state)
        {
            if (state > 0)
            {
                WriteOutput(0, 0);
                WriteOutput(1, 1);
            }
            else if (state < 0)
            {
                WriteOutput(1, 0);
                WriteOutput(0, 1);
            }
            else
            {
                WriteOutput(0, 0);
                WriteOutput(1, 0);
            }

        }
    }

    public class TimeoutAction : IDisposable
    {
        private readonly Timer _timer;
        private int _period;
        private readonly Action _action;

        public TimeoutAction(Action action, int timeout = 10)
        {
            _action = action;
            _period = timeout;
            _timer = new Timer { Interval = _period };
            _timer.Elapsed += OnElapsed;
        }

        public TimeoutAction SetState(bool state)
        {
            if (state)
            {
                if (_timer.Enabled)
                {
                    _timer.Stop();
                }
                _timer.Start();
            }
            else if (_timer.Enabled)
            {
                _timer.Stop();
            }

            return this;
        }

        public void SetTime(int timeout)
        {
            if (timeout == _period) return;
            var timerWasRunning = _timer.Enabled;
            if (timerWasRunning)
                SetState(false);
            _period = timeout;
            _timer.Interval = _period;
            if (timerWasRunning)
                SetState(true);
        }

        public void Dispose()
        {
            SetState(false);
        }

        private void OnElapsed(object sender, ElapsedEventArgs e)
        {
            SetState(false);
            _action();
        }
    }

    public class PeriodicAction : IDisposable
    {
        private readonly Action _action;
        private readonly TimeSpan _period;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _task;

        public PeriodicAction(Action action, TimeSpan period)
        {
            _action = action;
            _period = period;
        }

        public PeriodicAction SetState(bool state)
        {
            if (_task != null)
            {
                _cancellationTokenSource.Cancel();
                _task = null;
            }
            if (state)
            {
                _task = AsyncStart();
            }

            return this;
        }

        public void Dispose()
        {
            SetState(false);
        }

        private async Task AsyncStart()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(_period, _cancellationTokenSource.Token);

                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                    _action();
            }
        }
    }
}
