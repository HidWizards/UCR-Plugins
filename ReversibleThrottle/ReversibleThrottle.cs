using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;

namespace ReversibleThrottle
{
    [Plugin("Reversible Throttle", Group = "Axis", Description = "Use a button to swap an axis mapping between mid-point -> maximum and mid-point -> minimum")]
    [PluginInput(DeviceBindingCategory.Range, "Throttle")]
    [PluginOutput(DeviceBindingCategory.Range, "Axis")]
    [PluginInput(DeviceBindingCategory.Momentary, "Reverse")]
    public class ReversibleThrottle : Plugin
    {
        [PluginGui("Invert Input")]
        public bool InvertInput { get; set; }

        [PluginGui("Invert Output")]
        public bool InvertOutput { get; set; }

        private bool _reverseModeEnabled;
        private short _reverseButtonState;

        public ReversibleThrottle()
        {
        }

        public override void InitializeCacheValues()
        {
            _reverseButtonState = 0;
            _reverseModeEnabled = false;
        }

        public override void Update(params short[] values)
        {
            SetReverse(values[1]);
            var inVal = values[0];
            short outVal;
            if (InvertInput) inVal = Functions.Invert(inVal);

            var halfRange = (int)Math.Ceiling((decimal)(inVal + 32768) / 2);
            if (_reverseModeEnabled) outVal = Functions.ClampAxisRange(0 - halfRange);
            else outVal = Functions.ClampAxisRange(halfRange);

            Debug.WriteLine($"reverse: {_reverseModeEnabled}, inVal: {inVal}, halfRange = {halfRange}, outVal = {outVal}");

            if (InvertOutput) outVal = Functions.Invert(outVal);
            WriteOutput(0, outVal);
        }

        private void SetReverse(short value)
        {
            if (value == 1 && _reverseButtonState != 1)
            {
                _reverseModeEnabled = !_reverseModeEnabled;
                Debug.WriteLine($"Reverse = {_reverseModeEnabled}");
            }

            _reverseButtonState = value;
        }
    }

}
