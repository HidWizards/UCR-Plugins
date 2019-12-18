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
using HidWizards.UCR.Core.Utilities.AxisHelpers;

namespace AxisToDelta
{
    [Plugin("Axis to Delta", Group = "Delta", Description = "Remaps a joystick axis to a mouse axis")]
    [PluginInput(DeviceBindingCategory.Range, "Axis")]
    [PluginOutput(DeviceBindingCategory.Delta, "Delta")]
    public class AxisToDelta : Plugin
    {
        [PluginGui("Invert")]
        public bool Invert { get; set; }

        [PluginGui("Dead zone")]
        public int DeadZone { get; set; }

        [PluginGui("Input Sensitivity %")]
        public int Sensitivity { get; set; }

        [PluginGui("Min mouse delta move")]
        public int Min { get; set; }

        [PluginGui("Max mouse delta move")]
        public int Max { get; set; }

        private readonly Timer _absoluteModeTimer;
        private short _currentDelta;
        private float _scaleFactor;
        private readonly DeadZoneHelper _deadZoneHelper = new DeadZoneHelper();
        private readonly SensitivityHelper _sensitivityHelper = new SensitivityHelper();

        public AxisToDelta()
        {
            DeadZone = 0;
            Sensitivity = 1;
            Min = 1;
            Max = 20;
            _absoluteModeTimer = new Timer(10);
            _absoluteModeTimer.Elapsed += AbsoluteModeTimerElapsed;
        }

        #region Input Processing
        public override void Update(params short[] values)
        {
            var value = values[0];
            if (value != 0) value = _deadZoneHelper.ApplyRangeDeadZone(value);
            if (Invert) value = Functions.Invert(value);
            if (Sensitivity != 100) value = _sensitivityHelper.ApplyRangeSensitivity(value);

            if (value == 0)
            {
                SetAbsoluteTimerState(false);
                _currentDelta = 0;
            }
            else
            {
                var sign = Math.Sign(value);
                _currentDelta = Functions.ClampAxisRange(Convert.ToInt32(Min + Functions.WideAbs(value) * _scaleFactor) * sign);
                //Debug.WriteLine($"New Delta: {_currentDelta}");
                SetAbsoluteTimerState(true);
            }
        }

        private void SetAbsoluteTimerState(bool state)
        {
            if (state && !_absoluteModeTimer.Enabled)
            {
                _absoluteModeTimer.Start();
            }
            else if (!state && _absoluteModeTimer.Enabled)
            {
                _absoluteModeTimer.Stop();
            }
        }

        private void AbsoluteModeTimerElapsed(object sender, ElapsedEventArgs e)
        {
            WriteOutput(0, _currentDelta);
        }
        #endregion

        #region Settings configuration

        private void Initialize()
        {
            _scaleFactor = (float)(Max - (Min - 1)) / 32769;
            _deadZoneHelper.Percentage = DeadZone;
            _sensitivityHelper.Percentage = Sensitivity;
        }

        #endregion

        #region Event Handling
        public override void OnActivate()
        {
            Initialize();
            if (_currentDelta != 0)
            {
                SetAbsoluteTimerState(true);
            }
        }

        public override void OnDeactivate()
        {
            SetAbsoluteTimerState(false);
        }

        public override void OnPropertyChanged()
        {
            Initialize();
        }
        #endregion
    }
}
