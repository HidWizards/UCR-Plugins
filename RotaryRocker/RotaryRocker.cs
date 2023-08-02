using System.Reflection;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;
using System.Threading;


namespace HidWizards.UCR.Plugins.Remapper
{
    [Plugin("Rotary/Rocker", Group = "Button", Description = "Two buttons into rotary or multiposition switch")]
    [PluginInput(DeviceBindingCategory.Momentary , "Up")]
    [PluginInput(DeviceBindingCategory.Momentary, "Down")]
    [PluginOutput(DeviceBindingCategory.Momentary, "1")]
    [PluginOutput(DeviceBindingCategory.Momentary, "2")]
    [PluginOutput(DeviceBindingCategory.Momentary, "3")]
    [PluginOutput(DeviceBindingCategory.Momentary, "4")]
    [PluginOutput(DeviceBindingCategory.Momentary, "5")]
    [PluginOutput(DeviceBindingCategory.Momentary, "6")]
    
    public class RotaryRocker : Plugin
    {
        [PluginGui("Rotary", Order = 1)]
        public bool Rotary { get; set; }


        [PluginGui("Position Count", Order = 2)]
        public int PositionCount { get; set; }

        [PluginGui("Hold", Order = 3)]
        public bool Hold { get; set; }

        public RotaryRocker()
        {
            Rotary = false;
            PositionCount = 5;
            Hold = true;
        }
        private bool pUp = false;

        private bool pDown = false;
        private int position = 0;
        public override void InitializeCacheValues()
        {
            Initialize();
        }

        public override void Update(params short[] values)
        {
            var up =  values[0]>0;
            var down = values[1]>0;
            if (up&&!pUp)
            {
                WriteOutput(position, 0);
                position++;
                if (position >= PositionCount)
                {
                    position = Rotary ? 0 : PositionCount - 1;
                }
                WriteOutput(position, 1);
                if (!Hold)
                {
                    Thread.Sleep(50);
                    WriteOutput(position, 0);
                }
            }
            pUp = up;

            if (down&&!pDown)
            {
                WriteOutput(position, 0);
                position--;
                if (position <0)
                {
                    position = Rotary ? PositionCount - 1 : 0;
                }
                WriteOutput(position, 1);
                if(!Hold)
                {
                    Thread.Sleep(50);
                    WriteOutput(position, 0);
                }
            }
            pDown = down;
        }

        private void Initialize()
        {
            WriteOutput(position, Hold ? (short)1 : (short)0);
        }

        public override PropertyValidationResult Validate(PropertyInfo propertyInfo, dynamic value)
        {
            switch (propertyInfo.Name)
            {
                case nameof(PositionCount):
                    return InputValidation.ValidateRange(value, 1, 6);
            }

            return PropertyValidationResult.ValidResult;
        }
        public override void OnActivate()
        {
            base.OnActivate();
            WriteOutput(position, Hold?(short)1:(short)0);
        }
    }
}