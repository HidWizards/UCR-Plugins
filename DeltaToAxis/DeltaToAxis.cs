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
    [PluginSettingsGroup("Absolute Mode Settings", Group = "Absolute")]
    public class DeltaToAxis : Plugin
    {
        public enum Modes { Absolute, Relative } ;

        [PluginGui("Deadzone %")]
        public int Deadzone { get; set; }

        [PluginGui("Mode")]
        public Modes Mode { get; set; }

        [PluginGui("Sensitivity (multiplier)")]
        public double Sensitivity { get; set; }

        [PluginGui("Timeout in ms", Group = "Absolute")]
        public int AbsoluteTimeout { get; set; }

        private short _currentValue;
        private readonly Timer _absoluteModeTimer;

        public DeltaToAxis()
        {
            Deadzone = 0;
            Sensitivity = 100;
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
            if (Mode == Modes.Relative && values[1] == 1)
            {
                // Reset button pressed
                wideValue = 0;
            }
            else
            {
                // Normal operation
                if (Math.Abs(values[0]) < Deadzone) return;
                if (Mode == Modes.Absolute)
                {
                    wideValue = (int)(values[0] * 100 * Sensitivity);
                    SetAbsoluteTimerState(true);
                }
                else
                {
                    wideValue = _currentValue + (int)(values[0] * Sensitivity);
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
