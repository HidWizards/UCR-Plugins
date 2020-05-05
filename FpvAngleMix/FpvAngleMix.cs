using System.Reflection;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;

namespace FpvAngleMix
{
    [Plugin("FPV Angle Mix", Group = "Axis", Description = "Simulate BetaFlight's feature of the same name")]
    [PluginInput(DeviceBindingCategory.Range, "Roll Axis")]
    [PluginInput(DeviceBindingCategory.Range, "Yaw Axis")]
    [PluginOutput(DeviceBindingCategory.Range, "Roll Axis", Group = "Roll axis")]
    [PluginOutput(DeviceBindingCategory.Range, "Yaw Axis", Group = "Yaw axis")]
    public class FpvAngleMix : Plugin
    {
        [PluginGui("Camera Angle")]
        public decimal CameraAngle { get; set; }

        [PluginGui("Invert Roll", Group = "Roll axis")]
        public bool InvertRoll { get; set; }

        [PluginGui("Invert Yaw", Group = "Yaw axis")]
        public bool InvertYaw { get; set; }

        private decimal _mixFactor;

        public FpvAngleMix()
        {
        }

        public override void InitializeCacheValues()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Calculate amount of input to convert to other output
            // At 90 degrees, will be 1.0 (Convert all roll to yaw and vice versa)
            // At 45 degrees, will be 0.5 (Convert half of roll to yaw and vice versa)
            _mixFactor = CameraAngle / 90;
        }

        public override void Update(params short[] values)
        {
            // While inputs are short (16-bit), use 32-bit values for calculations, to ensure that no wrap-around occurs ((short))-32768 * -1 == -32768!)
            var inputRoll = InvertRoll ? (int)Functions.Invert(values[0]) : (int)values[0];
            var inputYaw = InvertYaw ? (int)Functions.Invert(values[1]) : (int)values[1];

            // Calculate the amount of roll to be converted to yaw
            var rollToYaw = (int)(inputRoll * _mixFactor) * -1;
            var outputYaw = rollToYaw;

            // Calculate the amount of yaw to be converted to roll
            var yawToRoll = (int)(inputYaw * _mixFactor);
            var outputRoll = yawToRoll;

            // Remove amount of roll converted to yaw from input roll
            inputRoll += rollToYaw;
            // Remove amount of yaw converted to roll from input yaw
            inputYaw -= yawToRoll;

            // Add remaining inputs to outputs
            outputRoll += inputRoll;
            outputYaw += inputYaw;

            // Clamp 32-bit values to 16-bit values, and pass to output
            WriteOutput(0, Functions.ClampAxisRange(outputRoll));
            WriteOutput(1, Functions.ClampAxisRange(outputYaw));
        }

        public override PropertyValidationResult Validate(PropertyInfo propertyInfo, dynamic value)
        {
            switch (propertyInfo.Name)
            {
                case nameof(CameraAngle):
                    return InputValidation.ValidateRange(value, 0, 90);
            }

            return PropertyValidationResult.ValidResult;
        }
    }
}
