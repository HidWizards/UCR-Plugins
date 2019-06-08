using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;

namespace HidWizards.UCR.Plugins.Remapper
{
    [Plugin("Delta to Axis", Group = "Delta", Description = "Remaps a mouse axis to a joystick axis")]
    [PluginInput(DeviceBindingCategory.Delta, "Delta")]
    [PluginInput(DeviceBindingCategory.Momentary, "Reset")]
    [PluginOutput(DeviceBindingCategory.Range, "Axis")]
    public class DeltaToAxis : Plugin
    {
        [PluginGui("Deadzone")]
        public int Deadzone { get; set; }

        [PluginGui("Relative Sensitivity")]
        public double RelativeSensitivity { get; set; }

        [PluginGui("Absolute Mode")]
        public bool AbsoluteMode { get; set; }

        [PluginGui("Absolute Sensitivity")]
        public double AbsoluteSensitivity { get; set; }

        [PluginGui("Absolute Timeout")]
        public int AbsoluteTimeout { get; set; }

        private short _currentValue;
        private readonly Timer _absoluteModeTimer;

        public DeltaToAxis()
        {
            AbsoluteMode = false;
            Deadzone = 0;
            RelativeSensitivity = 100;
            AbsoluteSensitivity = 10000;
            AbsoluteTimeout = 100;
            _absoluteModeTimer = new Timer();
            _absoluteModeTimer.Elapsed += AbsoluteModeTimerElapsed;
        }

        private void AbsoluteModeTimerElapsed(object sender, ElapsedEventArgs e)
        {
            WriteOutput(0, 0);
            SetAbsoluteTimerState(false);
        }

        public override void Update(params short[] values)
        {
            int wideValue;
            if (!AbsoluteMode && values[1] == 1)
            {
                // Reset button pressed
                wideValue = 0;
            }
            else
            {
                // Normal operation
                if (Math.Abs(values[0]) < Deadzone) return;
                if (AbsoluteMode)
                {
                    wideValue = (int)(values[0] * AbsoluteSensitivity);
                    SetAbsoluteTimerState(true);
                }
                else
                {
                    wideValue = _currentValue + (int)(values[0] * RelativeSensitivity);
                }
            }
            var value = Functions.ClampAxisRange(wideValue);
            _currentValue = value;
            WriteOutput(0, value);
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
