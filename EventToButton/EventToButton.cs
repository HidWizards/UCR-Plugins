using System.Threading;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;

namespace EventToButton
{
    [Plugin("Event to Button", Group = "Event", Description = "Remaps an event type (eg Mouse Wheel) to a button")]
    [PluginInput(DeviceBindingCategory.Event, "Button")]
    [PluginOutput(DeviceBindingCategory.Momentary, "Button")]
    public class EventToButton : Plugin
    {

        [PluginGui("Hold Time")]
        public int HoldTime { get; set; }

        public EventToButton()
        {
            HoldTime = 50;
        }

        public override void Update(params short[] values)
        {
            WriteOutput(0, 1);
            ThreadPool.QueueUserWorkItem(cb => Release());
        }

        private void Release()
        {
            Thread.Sleep(HoldTime);
            WriteOutput(0, 0);
        }
    }
}
