using System.Reflection;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;
using HidWizards.UCR.Core.Utilities.AxisHelpers;

namespace HidWizards.UCR.Plugins.Remapper
{
    [Plugin("Axis Splitter with Enable", Group = "Axis", Description = "Split one axis into two new axes, when disabled ressets axes to default values")]
    [PluginInput(DeviceBindingCategory.Range, "Axis")]
    [PluginInput(DeviceBindingCategory.Momentary, "Enable")]
    [PluginOutput(DeviceBindingCategory.Range, "Axis high")]
    [PluginOutput(DeviceBindingCategory.Range, "Axis low")]
    public class AxisSplitterEnable : Plugin
    {
        [PluginGui("Invert Enable", Order = 1)]
        public bool InvertEnable { get; set; }


        [PluginGui("Invert high", Order = 2)]
        public bool InvertHigh { get; set; }

        [PluginGui("Default high", Order = 3)]
        public double DefaultHigh { get; set; }


        [PluginGui("Invert low", Order = 4)]
        public bool InvertLow { get; set; }
        
        [PluginGui("Default high", Order = 5)]
        public double DefaultLow { get; set; }


        [PluginGui("Dead zone")]
        public int DeadZone { get; set; }

        private readonly DeadZoneHelper _deadZoneHelper = new DeadZoneHelper();

        public AxisSplitterEnable()
        {
            DeadZone = 0;
            DefaultLow = 0;
            DefaultHigh = 0;
        }

        public override void InitializeCacheValues()
        {
            Initialize();
        }

        public override void Update(params short[] values)
        {
            var value = values[0];
            var enable = values[1];
            var high = Functions.GetRangeFromPercentage((short)DefaultHigh);
            var low = Functions.GetRangeFromPercentage((short)DefaultLow);
            if (enable > 0 ^ InvertEnable)
            {
                if (DeadZone != 0) value = _deadZoneHelper.ApplyRangeDeadZone(value);
                high = Functions.SplitAxis(value, true);
                low = Functions.SplitAxis(value, false);
                if (InvertHigh) high = Functions.Invert(high);
                if (InvertLow) low = Functions.Invert(low);
            }

            WriteOutput(0, high);
            WriteOutput(1, low);
        }

        private void Initialize()
        {
            _deadZoneHelper.Percentage = DeadZone;
        }

        public override PropertyValidationResult Validate(PropertyInfo propertyInfo, dynamic value)
        {
            switch (propertyInfo.Name)
            {
                case nameof(DeadZone):
                    return InputValidation.ValidatePercentage(value);
                case nameof(DefaultLow):
                case nameof(DefaultHigh):
                    return InputValidation.ValidateRange(value, -100.0, 100.0);
            }

            return PropertyValidationResult.ValidResult;
        }
    }
}