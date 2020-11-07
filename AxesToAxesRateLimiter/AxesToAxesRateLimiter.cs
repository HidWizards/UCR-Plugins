using System;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;
using HidWizards.UCR.Core.Utilities.AxisHelpers;

namespace AxesToAxesRateLimiter
{
    [Plugin("Axes to Axes (with rate limiting)", Group = "Axis", Description = "Map from joystick to joystick, limiting the movement rate")]
    [PluginInput(DeviceBindingCategory.Range, "X Axis")]
    [PluginInput(DeviceBindingCategory.Range, "Y Axis")]
    [PluginOutput(DeviceBindingCategory.Range, "X Axis", Group = "X axis")]
    [PluginOutput(DeviceBindingCategory.Range, "Y Axis", Group = "Y axis")]
    [PluginSettingsGroup("Sensitivity", Group = "Sensitivity")]
    [PluginSettingsGroup("Dead zone", Group = "Dead zone")]
    [PluginSettingsGroup("Axis Movement Rate", Group = "Axis Movement Rate")]
    public class AxesToAxesRateLimiter : Plugin
    {
        private readonly CircularDeadZoneHelper _circularDeadZoneHelper = new CircularDeadZoneHelper();
        private readonly DeadZoneHelper _deadZoneHelper = new DeadZoneHelper();
        private readonly SensitivityHelper _sensitivityHelper = new SensitivityHelper();
        private double _linearSensitivityScaleFactor;
        private double _MaxRateIncrement;
 
        [PluginGui("Invert X", Group = "X axis")]
        public bool InvertX { get; set; }

        [PluginGui("Invert Y", Group = "Y axis")]
        public bool InvertY { get; set; }

        [PluginGui("Max Rate (%/sec)", Order = 0, Group = "Axis Movement Rate")]
        public double MaxRate { get; set; }

        [PluginGui("Percentage", Order = 0, Group = "Sensitivity")]
        public int Sensitivity { get; set; }

        [PluginGui("Linear", Order = 0, Group = "Sensitivity")]
        public bool Linear { get; set; }

        [PluginGui("Percentage", Order = 0, Group = "Dead zone")]
        public int DeadZone { get; set; }

        [PluginGui("Circular", Order = 1, Group = "Dead zone")]
        public bool CircularDz { get; set; }

        private readonly object _threadLock = new object();

        private Thread _relativeThread;

        private short[] outputValues = new short[2];
        private int[] outputValuesLimited = new int[2];


        public AxesToAxesRateLimiter()
        {
            DeadZone = 0;
            Sensitivity = 100;
            MaxRate = 100;
            _relativeThread = new Thread(RelativeThread);
        }

        public override void InitializeCacheValues()
        {
            Initialize();
        }

        private void Initialize()
        {
            _deadZoneHelper.Percentage = DeadZone;
            _circularDeadZoneHelper.Percentage = DeadZone;
            _sensitivityHelper.Percentage = Sensitivity;
            _linearSensitivityScaleFactor = ((double)Sensitivity / 100);
            _MaxRateIncrement = (0.573 * MaxRate);
        }

        public override void Update(params short[] values)
        {
            outputValues[0] = values[0];
            outputValues[1] = values[1];
            
            if (DeadZone != 0)
            {
                if (CircularDz)
                {
                    outputValues = _circularDeadZoneHelper.ApplyRangeDeadZone(outputValues);
                }
                else
                {
                    outputValues[0] = _deadZoneHelper.ApplyRangeDeadZone(outputValues[0]);
                    outputValues[1] = _deadZoneHelper.ApplyRangeDeadZone(outputValues[1]);
                }

            }
            if (Sensitivity != 100)
            {
                if (Linear)
                {
                    outputValues[0] = (short)(outputValues[0] * _linearSensitivityScaleFactor);
                    outputValues[1] = (short)(outputValues[1] * _linearSensitivityScaleFactor);
                }
                else
                {
                    outputValues[0] = _sensitivityHelper.ApplyRangeSensitivity(outputValues[0]);
                    outputValues[1] = _sensitivityHelper.ApplyRangeSensitivity(outputValues[1]);
                }
            }

            outputValues[0] = Functions.ClampAxisRange(outputValues[0]);
            outputValues[1] = Functions.ClampAxisRange(outputValues[1]);

            if (InvertX) outputValues[0] = Functions.Invert(outputValues[0]);
            if (InvertY) outputValues[1] = Functions.Invert(outputValues[1]);

            SetRelativeThreadState(true);
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
  
            while (true)
             {
                RelativeUpdate();
                Thread.Sleep(1);
            }
        }

        private void RelativeUpdate()
        {
            if (outputValuesLimited[0] < outputValues[0])
            {
                outputValuesLimited[0] += (int)_MaxRateIncrement;
                if (outputValuesLimited[0] > outputValues[0])
                {
                    outputValuesLimited[0] = outputValues[0];
                }
            }
            if (outputValuesLimited[0] > outputValues[0])
            {
                outputValuesLimited[0] -= (int)_MaxRateIncrement;
                if (outputValuesLimited[0] < outputValues[0])
                {
                    outputValuesLimited[0] = outputValues[0];
                }
            }

            if (outputValuesLimited[1] < outputValues[1])
            {
                outputValuesLimited[1] += (int)_MaxRateIncrement;
                if (outputValuesLimited[1] > outputValues[1])
                {
                    outputValuesLimited[1] = outputValues[1];
                }
            }
            if (outputValuesLimited[1] > outputValues[1])
            {
                outputValuesLimited[1] -= (int)_MaxRateIncrement;
                if (outputValuesLimited[1] < outputValues[1])
                {
                    outputValuesLimited[1] = outputValues[1];
                }
            }
            WriteOutput(0, (short)outputValuesLimited[0]);
            WriteOutput(1, (short)outputValuesLimited[1]); 
        }

                  

        public override PropertyValidationResult Validate(PropertyInfo propertyInfo, dynamic value)
        {
            switch (propertyInfo.Name)
            {
                case nameof(DeadZone):
                    return InputValidation.ValidatePercentage(value);
            }

            return PropertyValidationResult.ValidResult;
        }
    }
}
