using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;
using HidWizards.UCR.Core.Utilities.AxisHelpers;

namespace AxisToAxisIncrement
{
    [Plugin("Axis to Axis Increment", Group = "Axis", Description = "Remap an axis to an axis, but in an \"incremental\" way")]
    [PluginInput(DeviceBindingCategory.Range, "Axis")]
    [PluginOutput(DeviceBindingCategory.Range, "Axis")]
    [PluginSettingsGroup("Relative Mode", Group = "Relative")]
    [PluginSettingsGroup("Absolute Mode", Group = "Absolute")]
    public class AxisToAxisIncrement : Plugin
    {
        [PluginGui("Invert")]
        public bool Invert { get; set; }

        [PluginGui("Linear deadzone")]
        public bool Linear { get; set; }

        [PluginGui("Dead zone %")]
        public int DeadZone { get; set; }

        [PluginGui("Input Sensitivity %")]
        public int Sensitivity { get; set; }

        /// <summary>
        /// To constantly add current axis values to the output - WORK IN PROGRESS!!!
        /// </summary>
        [PluginGui("Continue Mode", Group = "Relative")]
        public bool RelativeContinue { get; set; }

        [PluginGui("Sensitivity %", Group = "Relative")]
        public decimal RelativeSensitivity { get; set; }

        [PluginGui("Counter Effect", Group = "Relative")]
        public double CounterEffect { get; set; }

        private readonly DeadZoneHelper _deadZoneHelper = new DeadZoneHelper();
        private readonly SensitivityHelper _sensitivityHelper = new SensitivityHelper();

        private short _currentOutputValue;
        private short _currentInputValue;
        private readonly object _threadLock = new object();

        private Thread _relativeThread;

        public AxisToAxisIncrement()
        {
            DeadZone = 0;
            Sensitivity = 100;
            RelativeContinue = true;
            RelativeSensitivity = 2;
            CounterEffect = 1.5;
            _relativeThread = new Thread(RelativeThread);
        }

        public override void Update(params short[] values)
        {
            var value = values[0];

            if (Invert) value = Functions.Invert(value);
            if (DeadZone != 0) value = _deadZoneHelper.ApplyRangeDeadZone(value);
            if (Sensitivity != 100) value = _sensitivityHelper.ApplyRangeSensitivity(value);
                    
            _currentInputValue = value;

            if (RelativeContinue)
            {
                SetRelativeThreadState(value != 0);
            }
            else
            {
                RelativeUpdate();
            }
        }

        private void Initialize()
        {
            _deadZoneHelper.Percentage = DeadZone;
            _sensitivityHelper.Percentage = Sensitivity;
            _sensitivityHelper.IsLinear = Linear;
        }

        #region Event Handling
        public override void OnActivate()
        {
            Initialize();
        }

        public override void OnPropertyChanged()
        {
            Initialize();
        }

        public override void OnDeactivate()
        {
            SetRelativeThreadState(false);
        }
        #endregion

        private void SetRelativeThreadState(bool state)
        {
            lock (_threadLock)
            {
                var relativeThreadActive = RelativeThreadActive();
                if (!relativeThreadActive && state)
                {
                    _relativeThread = new Thread(RelativeThread);
                    _relativeThread.Start();
                    Debug.WriteLine("UCR| Started Relative Thread");
                }
                else if (relativeThreadActive && !state)
                {
                    _relativeThread.Abort();
                    _relativeThread.Join();
                    _relativeThread = null;
                    Debug.WriteLine("UCR| Stopped Relative Thread");
                }
            }
        }

        private bool RelativeThreadActive()
        {
            return _relativeThread != null && _relativeThread.IsAlive;
        }

        public void RelativeThread()
        {
            while (RelativeContinue)
            {
                RelativeUpdate();
                Thread.Sleep(10);
            }
        }

        short lastInputValue;

        private void RelativeUpdate()
        {
            if (Math.Sign((double)_currentInputValue) != Math.Sign((double)_currentOutputValue) && Math.Abs(_currentInputValue) < Math.Abs(lastInputValue))
            {
                _currentInputValue *= (short)(CounterEffect);
            }

            int wideValue = (int)(_currentInputValue * (RelativeSensitivity / 100) + _currentOutputValue);

            var value = Functions.ClampAxisRange(wideValue);
            WriteOutput(0, value);
            _currentOutputValue = value;
            lastInputValue = _currentInputValue;
        }
    }
}
