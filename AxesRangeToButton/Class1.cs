using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;

namespace AxesRangeToButton
{
    [Plugin("Axes Range to Button")]
    [PluginInput(DeviceBindingCategory.Range, "X Axis")]
    [PluginInput(DeviceBindingCategory.Range, "Y Axis")]
    [PluginOutput(DeviceBindingCategory.Momentary, "Button")]
    public class AxesRangeToButton : Plugin
    {
        [PluginGui("Invert X", ColumnOrder = 0)]
        public bool InvertX { get; set; }

        [PluginGui("Invert Y", ColumnOrder = 1)]
        public bool InvertY { get; set; }

        [PluginGui("X Start %", RowOrder = 1, ColumnOrder = 0)]
        public double XStart { get; set; }

        [PluginGui("X End %", RowOrder = 1, ColumnOrder = 1)]
        public double XEnd { get; set; }

        [PluginGui("Y Start %", RowOrder = 1, ColumnOrder = 2)]
        public double YStart { get; set; }

        [PluginGui("Y End %", RowOrder = 1, ColumnOrder = 3)]
        public double YEnd { get; set; }

        private long _xStart;
        private long _xEnd;
        private long _yStart;
        private long _yEnd;
        private long _currentState;

        public AxesRangeToButton()
        {
            XStart = -50.0;
            XEnd = 50.0;
            YStart = -50.0;
            YEnd = 50.0;
        }

        private void Initialize()
        {
            _xStart = Functions.ClampAxisRange((long)(XStart * 327.68));
            _xEnd = Functions.ClampAxisRange((long)(XEnd * 327.68));

            _yStart = Functions.ClampAxisRange((long)(YStart * 327.68));
            _yEnd = Functions.ClampAxisRange((long)(YEnd * 327.68));
        }

        public override void Update(params long[] values)
        {

            if (InvertX) values[0] = Functions.Invert(values[0]);
            if (InvertY) values[1] = Functions.Invert(values[1]);

            long newState;
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
    }
}
