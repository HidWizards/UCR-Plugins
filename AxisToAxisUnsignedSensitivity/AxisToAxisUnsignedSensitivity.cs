﻿using System.Reflection;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;

namespace AxisToAxisUnsignedSensitivity
{
    [Plugin("Axis to Axis (Unsigned Sensitivity)", Group = "Axis", Description = "Map from one axis to another (Unsigned Sensitivity)")]
    [PluginInput(DeviceBindingCategory.Range, "Axis")]
    [PluginOutput(DeviceBindingCategory.Range, "Axis")]
    [PluginSettingsGroup("Sensitivity", Group = "Sensitivity")]
    [PluginSettingsGroup("Dead zone", Group = "Dead zone")]
    public class AxisToAxisUnsignedSensitivity : Plugin
    {
        private float _sensitivityFactor;

        [PluginGui("Invert")]
        public bool Invert { get; set; }

        //[PluginGui("Linear", Group = "Sensitivity", Order = 1)]
        //public bool Linear { get; set; }

        //[PluginGui("Percentage", Group = "Dead zone", Order = 0)]
        //public int DeadZone { get; set; }

        //[PluginGui("Anti-dead zone", Group = "Dead zone")]
        //public int AntiDeadZone { get; set; }

        [PluginGui("Percentage", Group = "Sensitivity")]
        public float Sensitivity { get; set; }
        //public int Sensitivity { get; set; }

        //private readonly DeadZoneHelper _deadZoneHelper = new DeadZoneHelper();
        //private readonly AntiDeadZoneHelper _antiDeadZoneHelper = new AntiDeadZoneHelper();
        //private readonly SensitivityHelper _sensitivityHelper = new SensitivityHelper();

        public AxisToAxisUnsignedSensitivity()
        {
            //DeadZone = 0;
            //AntiDeadZone = 0;
            Sensitivity = 100;
        }

        public override void InitializeCacheValues()
        {
            Initialize();
        }

        public override void Update(params short[] values)
        {
            var value = values[0];
            if (Invert) value = Functions.Invert(value);
            //if (DeadZone != 0) value = _deadZoneHelper.ApplyRangeDeadZone(value);
            //if (AntiDeadZone != 0) value = _antiDeadZoneHelper.ApplyRangeAntiDeadZone(value);
            //if (Sensitivity != 100) value = _sensitivityHelper.ApplyRangeSensitivity(value);
            var wideValue = (int)value + Constants.AxisMaxAbsValue;
            wideValue = (int)(wideValue * _sensitivityFactor);
            wideValue -= Constants.AxisMaxAbsValue;
            WriteOutput(0, Functions.ClampAxisRange(wideValue));
        }

        private void Initialize()
        {
            //_deadZoneHelper.Percentage = DeadZone;
            //_antiDeadZoneHelper.Percentage = AntiDeadZone;
            //_sensitivityHelper.Percentage = Sensitivity;
            //_sensitivityHelper.IsLinear = Linear;
            _sensitivityFactor = Sensitivity / 100;
        }

        public override PropertyValidationResult Validate(PropertyInfo propertyInfo, dynamic value)
        {
            switch (propertyInfo.Name)
            {
                //case nameof(DeadZone):
                //case nameof(AntiDeadZone):
                //    return InputValidation.ValidatePercentage(value);
            }

            return PropertyValidationResult.ValidResult;
        }
    }
}
