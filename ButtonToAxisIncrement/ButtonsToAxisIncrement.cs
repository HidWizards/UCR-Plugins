using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;

namespace ButtonsToAxisIncrement
{
    [Plugin("Buttons to Axis Increment")]
    [PluginInput(DeviceBindingCategory.Momentary, "Button (Dec)")]
    [PluginInput(DeviceBindingCategory.Momentary, "Button (Inc)")]
    [PluginOutput(DeviceBindingCategory.Range, "Axis")]
    public class ButtonsToAxisIncrement : Plugin
    {
        [PluginGui("Invert", ColumnOrder = 0)]
        public bool Invert { get; set; }

        /// <summary>
        /// It increment and decrement the output axis by a define amount
        /// </summary>
        [PluginGui("Amount", ColumnOrder = 0, RowOrder = 2)]
        public double Amount { get; set; }

        long _currentOutputValue;
        readonly object _threadLock = new object();

        private Thread _relativeThread;

        public ButtonsToAxisIncrement()
        {
            Amount = 5;
            _relativeThread = new Thread(RelativeThread);
        }

        public override void Update(params long[] values)
        {
            var value = _currentOutputValue;

            // if Button (Dec) is pressed, reduce axis value by the defined amount
            if (values[0] == 1)
            {                
                RelativeUpdate(0, value);
            }

            // but if Button (Inc) is pressed, then increment the axis value
            else if (values[1] == 1)
            {                
                RelativeUpdate(1, value);
            }
            
            if (Invert) value = Functions.Invert(value);
            WriteOutput(0, value);

            _currentOutputValue = value;
        }

        public override void OnDeactivate()
        {
            SetRelativeThreadState(false);
        }

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

        public static void RelativeThread()
        {
            Thread.Sleep(10);
        }

        private void RelativeUpdate(int button, long value)
        {
            // if Button (Dec) is pressed, reduce axis value by the defined amount
            if (button == 0)
            {
                value += (long)Amount;                
            }

            // but if Button (Inc) is pressed, then increment the axis value
            else if (button == 1)
            {
                value -= (long)Amount;
            }

            value = Math.Min(Math.Max(value, Constants.AxisMinValue), Constants.AxisMaxValue);
            WriteOutput(0, value);
            _currentOutputValue = value;
        }
    }
}
