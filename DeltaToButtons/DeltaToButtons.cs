using System;
using System.Threading;
using System.Timers;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;
using Timer = System.Timers.Timer;

namespace DeltaToButtons
{
    [Plugin("Delta to Buttons", Group = "Delta", Description = "Remaps a mouse axis to two buttons")]
    [PluginInput(DeviceBindingCategory.Delta, "Delta")]
    [PluginOutput(DeviceBindingCategory.Momentary, "Button Low")]
    [PluginOutput(DeviceBindingCategory.Momentary, "Button High")]
    public class DeltaToButtons : Plugin
    {
        [PluginGui("Min")]
        public int Min { get; set; }

        [PluginGui("Center Timeout")]
        public int CenterTimeout { get; set; }

        [PluginGui("DeBounce Time")]
        public int DeBounceTimeout { get; set; }

        private readonly Timer _centerTimer;
        private Thread _debounceThread;
        private int _stateChangeTimeout = int.MaxValue;
        private int _nextState = 0;

        public DeltaToButtons()
        {
            Min = 0;
            CenterTimeout = 100;
            DeBounceTimeout = 100;
            _centerTimer = new Timer();
            _centerTimer.Elapsed += CenterTimerElapsed;
        }

        private void CenterTimerElapsed(object sender, ElapsedEventArgs e)
        {
            SetOutputState(0);
            SetCenterTimerState(false);
        }

        public override void Update(params short[] values)
        {
            if (Math.Abs(values[0]) < Min) return;
            SetCenterTimerState(true);
            if (values[0] > 0)
            {
                SetOutputState(1);
            }
            else if (values[0] < 0)
            {
                SetOutputState(-1);
            }
        }

        public void SetCenterTimerState(bool state)
        {
            if (state)
            {
                if (_centerTimer.Enabled)
                {
                    _centerTimer.Stop();
                }
                _centerTimer.Interval = CenterTimeout;
                _centerTimer.Start();
            }
            else if (_centerTimer.Enabled)
            {
                _centerTimer.Stop();
            }
        }

        public override void OnActivate()
        {
            _debounceThread = new Thread(DeBounceThread);
            _debounceThread.Start();
        }

        public override void OnDeactivate()
        {
            SetCenterTimerState(false);
            _debounceThread.Abort();
            _debounceThread.Join();
        }

        private void SetOutputState(int state)
        {
            if (state != _nextState)
            {
                _stateChangeTimeout = Environment.TickCount + DeBounceTimeout;
                _nextState = state;
            }
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

            _stateChangeTimeout = int.MaxValue;
        }

        private void DeBounceThread()
        {
            while (true)
            {
                if (Environment.TickCount > _stateChangeTimeout)
                {
                    WriteOutput(_nextState);
                }
                Thread.Sleep(10);
            }
        }
    }
}
