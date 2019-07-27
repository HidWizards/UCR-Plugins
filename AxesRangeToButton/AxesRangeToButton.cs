using System.Reflection;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;

namespace AxesRangeToButton
{
    [Plugin("Axes Range to Button", Group = "Axis", Description = "Presses a button when two axes are within a specific range")]
    [PluginInput(DeviceBindingCategory.Range, "Axis 1")]
    [PluginInput(DeviceBindingCategory.Range, "Axis 2")]
    [PluginOutput(DeviceBindingCategory.Momentary, "Button")]
    public class AxesRangeToButton : Plugin
    {
        [PluginGui("Invert X")]
        public bool InvertX { get; set; }

        [PluginGui("Invert Y")]
        public bool InvertY { get; set; }

        [PluginGui("X Start %")]
        public double XStart { get; set; }

        [PluginGui("X End %")]
        public double XEnd { get; set; }

        [PluginGui("Y Start %")]
        public double YStart { get; set; }

        [PluginGui("Y End %")]
        public double YEnd { get; set; }

        private short _xStart;
        private short _xEnd;
        private short _yStart;
        private short _yEnd;
        private short _currentState;

        public AxesRangeToButton()
        {
            XStart = -50.0;
            XEnd = 50.0;
            YStart = -50.0;
            YEnd = 50.0;
        }

        private void Initialize()
        {
            _xStart = Functions.ClampAxisRange((int)(XStart * 327.68));
            _xEnd = Functions.ClampAxisRange((int)(XEnd * 327.68));

            _yStart = Functions.ClampAxisRange((int)(YStart * 327.68));
            _yEnd = Functions.ClampAxisRange((int)(YEnd * 327.68));
        }

        public override void Update(params short[] values)
        {

            if (InvertX) values[0] = Functions.Invert(values[0]);
            if (InvertY) values[1] = Functions.Invert(values[1]);

            short newState;
            if (values[0] >= _xStart && values[0] <= _xEnd && values[1] >= _yStart && values[1] <= _yEnd)
            {
                newState = 1;
            }
            else
            {
                newState = 0;
            }

            if (newState == _currentState) return;
            WriteOutput(0, newState);
            _currentState = newState;
        }

        #region Event Handling
        public override void OnActivate()
        {
            base.OnActivate();
            Initialize();
        }

        public override void OnPropertyChanged()
        {
            base.OnPropertyChanged();
            Initialize();
        }
        #endregion

        public override PropertyValidationResult Validate(PropertyInfo propertyInfo, dynamic value)
        {
            switch (propertyInfo.Name)
            {
                case nameof(XStart):
                case nameof(XEnd):
                case nameof(YStart):
                case nameof(YEnd):
                    return InputValidation.ValidateRange(value, -100, 100);
            }

            return PropertyValidationResult.ValidResult;
        }
    }
}