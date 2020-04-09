using System.Reflection;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;
using HidWizards.UCR.Core.Utilities.AxisHelpers;

namespace AxesToAxesTrim
{
    [Plugin("Axes to Axes (Trim)", Group = "Axis", Description = "Remap two axes to two axes, and trim them when a button is held")]
    [PluginInput(DeviceBindingCategory.Range, "X Axis")]
    [PluginInput(DeviceBindingCategory.Range, "Y Axis")]
    [PluginInput(DeviceBindingCategory.Momentary, "Trim")]
    [PluginInput(DeviceBindingCategory.Momentary, "Reset Trim")]
    [PluginOutput(DeviceBindingCategory.Range, "X Axis")]
    [PluginOutput(DeviceBindingCategory.Range, "Y Axis")]
    public class AxesToAxesTrim : Plugin
    {
        private readonly CircularDeadZoneHelper _circularDeadZoneHelper = new CircularDeadZoneHelper();
        private readonly DeadZoneHelper _deadZoneHelper = new DeadZoneHelper();
        private readonly SensitivityHelper _sensitivityHelper = new SensitivityHelper();
        private double _linearSenstitivityScaleFactor;
        private short _trimX;
        private short _trimY;
        private bool _trimValueTaken;

        [PluginGui("Invert X")]
        public bool InvertX { get; set; }

        [PluginGui("Invert Y")]
        public bool InvertY { get; set; }

        [PluginGui("Sensitivity %")]
        public int Sensitivity { get; set; }

        [PluginGui("Linear sensitivity")]
        public bool Linear { get; set; }

        [PluginGui("Dead zone %")]
        public int DeadZone { get; set; }

        [PluginGui("Circular deadzone")]
        public bool CircularDz { get; set; }


        public AxesToAxesTrim()
        {
            DeadZone = 0;
            Sensitivity = 100;
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
            _linearSenstitivityScaleFactor = ((double)Sensitivity / 100);
        }

        public override void Update(params short[] values)
        {
            if (values[3] == 1)
            {
                // Reset held
                _trimX = 0;
                _trimY = 0;
            }
            else if (values[2] == 1)
            {
                // Trim held
                if (!_trimValueTaken)
                {
                    // First iteration through, store new trim value and set _trimValueTaken flag
                    _trimX += values[0];
                    _trimY += values[1];
                    _trimValueTaken = true;
                }
            }
            else
            {
                var inputValues = new[] { values[0], values[1] };
                if (values[2] == 0) _trimValueTaken = false;    // ToDo: This is clumsy, fix once we can get notification of *which* input changed state
                if (DeadZone != 0)
                {
                    if (CircularDz)
                    {
                        inputValues = _circularDeadZoneHelper.ApplyRangeDeadZone(inputValues);
                    }
                    else
                    {
                        inputValues[0] = _deadZoneHelper.ApplyRangeDeadZone(inputValues[0]);
                        inputValues[1] = _deadZoneHelper.ApplyRangeDeadZone(inputValues[1]);
                    }

                }
                if (Sensitivity != 100)
                {
                    if (Linear)
                    {
                        inputValues[0] = (short)(inputValues[0] * _linearSenstitivityScaleFactor);
                        inputValues[1] = (short)(inputValues[1] * _linearSenstitivityScaleFactor);
                    }
                    else
                    {
                        inputValues[0] = _sensitivityHelper.ApplyRangeSensitivity(inputValues[0]);
                        inputValues[1] = _sensitivityHelper.ApplyRangeSensitivity(inputValues[1]);
                    }
                }

                // Apply trim
                var wideValues = new[] { (int)inputValues[0], (int)inputValues[1] };
                wideValues[0] += _trimX;
                wideValues[1] += _trimY;

                var outputValues = new short[2];
                outputValues[0] = Functions.ClampAxisRange(wideValues[0]);
                outputValues[1] = Functions.ClampAxisRange(wideValues[1]);

                if (InvertX) outputValues[0] = Functions.Invert(outputValues[0]);
                if (InvertY) outputValues[1] = Functions.Invert(outputValues[1]);

                WriteOutput(0, outputValues[0]);
                WriteOutput(1, outputValues[1]);
            }
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
