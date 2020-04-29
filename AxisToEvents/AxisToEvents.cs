using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;
using HidWizards.UCR.Core.Utilities.AxisHelpers;

namespace AxisToEvents
{
    [Plugin("Axis to Events", Group = "Event", Description = "Map from one axis to two Events")]
    [PluginInput(DeviceBindingCategory.Range, "Axis")]
    [PluginOutput(DeviceBindingCategory.Event, "Event high")]
    [PluginOutput(DeviceBindingCategory.Event, "Event low")]
    public class AxisToEvents : Plugin
    {
        [PluginGui("Pulse", Order = 0)]
        public bool Pulse { get; set; }

        [PluginGui("Sensitivity", Order = 3)]
        public int Sensitivity { get; set; }

        [PluginGui("Invert", Order = 1)]
        public bool Invert { get; set; }

        [PluginGui("Dead zone", Order = 2)]
        public int DeadZone { get; set; }

        private short oldvalue = 0;
        private int counter = 0;

        private bool RelativeContinue;

        private Thread _relativeThread;
        private readonly object _threadLock = new object();

        private readonly DeadZoneHelper _deadZoneHelper = new DeadZoneHelper();

        public AxisToEvents()
        {
            DeadZone = 10;
            Sensitivity = 30;
        }

        public override void InitializeCacheValues()
        {
            Initialize();
        }

        public override void Update(params short[] values)
        {
            var value = values[0];

            if (Invert) value = Functions.Invert(value);

            int temp = Math.Sign(_deadZoneHelper.ApplyRangeDeadZone(value));
            int oldtemp = Math.Sign(_deadZoneHelper.ApplyRangeDeadZone(oldvalue));

            if (oldtemp != temp && oldtemp == 0)
            {
                if (Pulse)
                {
                    counter = 0;
                }
                else
                {
                    SetRelativeThreadState(true);
                }
            }
            else if (temp == 0 || Pulse)
            {
                SetRelativeThreadState(false);
            }

            if (Pulse && counter > -10 * Sensitivity/100)
            {
                switch (temp)
                {
                    case 0:
                        break;
                    case 1:
                        WriteOutput(0, 1);
                        break;
                    case -1:
                        WriteOutput(1, 1);
                        break;
                }

                counter--;
            }

            oldvalue = value;           
        }

        private void Initialize()
        {
            RelativeContinue = true;
            _deadZoneHelper.Percentage = DeadZone;
            _relativeThread = new Thread(RelativeThread);
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

        public void RelativeThread()
        {
            int temp;
            while (RelativeContinue)
            {
                RelativeUpdate();

                temp = Functions.ClampAxisRange(oldvalue);

                if (temp != 0)
                {
                    Thread.Sleep(Math.Abs(100000/(100*Sensitivity) * 100 / ((100 * temp) / Constants.AxisMaxAbsValue)));
                }
            }
        }

        private void RelativeUpdate()
        {
            switch (Math.Sign(_deadZoneHelper.ApplyRangeDeadZone(oldvalue)))
            {
                case 0:
                    break;
                case 1:
                    WriteOutput(0, 1);
                    break;
                case -1:
                    WriteOutput(1, 1);
                    break;
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
