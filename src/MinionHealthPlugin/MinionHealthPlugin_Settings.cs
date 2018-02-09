using System.Collections.Generic;
using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using SharpDX;

namespace MinionHealthPlugin
{
    public class MinionHealthPlugin_Settings : SettingsBase
    {
        public MinionHealthPlugin_Settings()
        {
            Enable = false;
            PosX = new RangeNode<int>(500, 0, 2000);
            PosY = new RangeNode<int>(800, 0, 2000);
            Width = new RangeNode<int>(700, 0, 1000);
            Height = new RangeNode<int>(20, 0, 100);
            Opacity = new RangeNode<float>(160, 0, 255);
            OfferingDuration = new RangeNode<float>(12, 2, 20);
            BGColor = Color.Black;
            HpColor = Color.Green;
            BorderColor = Color.White;
            Gradient = true;
        }

        [Menu("Pos X")]
        public RangeNode<int> PosX { get; set; }
        [Menu("Pos Y")]
        public RangeNode<int> PosY { get; set; }
        [Menu("Width")]
        public RangeNode<int> Width { get; set; }
        [Menu("Height")]
        public RangeNode<int> Height { get; set; }
        [Menu("BG Color")]
        public ColorNode BGColor { get; set; }
        [Menu("HP Color")]
        public ColorNode HpColor { get; set; }
        [Menu("Border Color")]
        public ColorNode BorderColor { get; set; }

        [Menu("Lerp Color Linearly")]
        public ToggleNode Gradient { get; set; }

        [Menu("Opacity")]
        public RangeNode<float> Opacity { get; set; }

        [Menu("Offering Duration")]
        public RangeNode<float> OfferingDuration { get; set; }
    }
}