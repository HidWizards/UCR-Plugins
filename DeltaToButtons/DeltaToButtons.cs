using System;
using System.Timers;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;

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
        public int AbsoluteTimeout { get; set; }

        private readonly Timer _absoluteModeTimer;

        public DeltaToButtons()
        {
            Min = 0;
            AbsoluteTimeout = 100;
            _absoluteModeTimer = new Timer();
            _absoluteModeTimer.Elapsed += AbsoluteModeTimerElapsed;
        }

        private void AbsoluteModeTimerElapsed(object sender, ElapsedEventArgs e)
        {
            WriteOutput(0, 0);
            WriteOutput(1, 0);
            SetAbsoluteTimerState(false);
        }

        public override void Update(params short[] values)
        {
            if (Math.Abs(values[0]) < Min) return;
            SetAbsoluteTimerState(true);
            if (values[0] > 0)
            {
                WriteOutput(0, 0);
                WriteOutput(1, 1);
            }
            else if (values[0] < 0)
            {
                WriteOutput(1, 0);
                WriteOutput(0, 1);
            }
        }

        public void SetAbsoluteTimerState(bool state)
        {
            if (state)
            {
                if (_absoluteModeTimer.Enabled)
                {
                    _absoluteModeTimer.Stop();
                }
                _absoluteModeTimer.Interval = AbsoluteTimeout;
                _absoluteModeTimer.Start();
            }
            else if (_absoluteModeTimer.Enabled)
            {
                _absoluteModeTimer.Stop();
            }
        }

        public override void OnDeactivate()
        {
            SetAbsoluteTimerState(false);
        }
    }
}
