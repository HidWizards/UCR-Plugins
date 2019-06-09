using System;
using System.Windows;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;
using HidWizards.UCR.Core.Utilities.AxisHelpers;

namespace AxesToAxesRotation
{
    [Plugin("Axes to Axes (With Rotation)", Group = "Axis", Description = "Remaps two axes to two axes, with rotation.\nIntended to be used with console style round gate sticks\nIf you find yourself hitting up right on left stick when you meant to hit up, this plugin can solve that")]
    [PluginInput(DeviceBindingCategory.Range, "X Axis")]
    [PluginInput(DeviceBindingCategory.Range, "Y Axis")]
    [PluginOutput(DeviceBindingCategory.Range, "X Axis")]
    [PluginOutput(DeviceBindingCategory.Range, "Y Axis")]
    public class AxesToAxesRotation : Plugin
    {
        private readonly CircularDeadZoneHelper _circularDeadZoneHelper = new CircularDeadZoneHelper();
        private readonly DeadZoneHelper _deadZoneHelper = new DeadZoneHelper();
        private readonly SensitivityHelper _sensitivityHelper = new SensitivityHelper();
        private double _linearSenstitivityScaleFactor;

        [PluginGui("Invert X")]
        public bool InvertX { get; set; }

        [PluginGui("Invert Y")]
        public bool InvertY { get; set; }

        [PluginGui("Sensitivity %")]
        public int Sensitivity { get; set; }

        [PluginGui("Linear Sensitivity")]
        public bool Linear { get; set; }

        [PluginGui("Dead zone %")]
        public int DeadZone { get; set; }

        [PluginGui("Circular deadzone")]
        public bool CircularDz { get; set; }

        [PluginGui("Rotation degrees")]
        public double Rotation { get; set; }

        public AxesToAxesRotation()
        {
            DeadZone = 0;
            Sensitivity = 100;
        }

        private void Initialize()
        {
            _deadZoneHelper.Percentage = DeadZone;
            _circularDeadZoneHelper.Percentage = DeadZone;
            _sensitivityHelper.Percentage = Sensitivity;
            _linearSenstitivityScaleFactor = ((double)Sensitivity / 100);
        }

        public override void Update(params short[] values)
        {
            var outputValues = new [] { values[0], values[1] };
            if (DeadZone != 0)
            {
                if (CircularDz)
                {
                    outputValues = _circularDeadZoneHelper.ApplyRangeDeadZone(outputValues);
                }
                else
                {
                    outputValues[0] = _deadZoneHelper.ApplyRangeDeadZone(outputValues[0]);
                    outputValues[1] = _deadZoneHelper.ApplyRangeDeadZone(outputValues[1]);
                }

            }
            if (Sensitivity != 100)
            {
                if (Linear)
                {
                    outputValues[0] = (short)(outputValues[0] * _linearSenstitivityScaleFactor);
                    outputValues[1] = (short)(outputValues[1] * _linearSenstitivityScaleFactor);
                }
                else
                {
                    outputValues[0] = _sensitivityHelper.ApplyRangeSensitivity(outputValues[0]);
                    outputValues[1] = _sensitivityHelper.ApplyRangeSensitivity(outputValues[1]);
                }
            }

            outputValues[0] = Functions.ClampAxisRange(outputValues[0]);
            outputValues[1] = Functions.ClampAxisRange(outputValues[1]);

            if (InvertX) outputValues[0] = Functions.Invert(outputValues[0]);
            if (InvertY) outputValues[1] = Functions.Invert(outputValues[1]);

            if (Math.Abs(Rotation) > 0)
            {
                var vector = new Vector(outputValues[0], outputValues[1]);
                vector = vector.Rotate(Rotation);
                outputValues[0] = (short)vector.X;
                outputValues[1] = (short)vector.Y;
            }

            WriteOutput(0, outputValues[0]);
            WriteOutput(1, outputValues[1]);
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

    public static class VectorExtensions
    {
        private const double DegToRad = Math.PI / 180;

        public static Vector Rotate(this Vector v, double degrees)
        {
            return v.RotateRadians(degrees * DegToRad);
        }

        public static Vector RotateRadians(this Vector v, double radians)
        {
            var ca = Math.Cos(radians);
            var sa = Math.Sin(radians);
            return new Vector(ca * v.X - sa * v.Y, sa * v.X + ca * v.Y);
        }
    }
}
