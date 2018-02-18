using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharpDX;
using SharpDX.Direct3D9;
using PoeHUD.Hud.Menu;
using PoeHUD.Hud.Settings;
using PoeHUD.Models.Enums;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using PoeHUD.Models;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.Elements;
using PoeHUD.Poe.EntityComponents;
using PoeHUD.Poe.RemoteMemoryObjects;

namespace MinionHealthPlugin
{
    public class MinionHealthPlugin : BaseSettingsPlugin<MinionHealthPlugin_Settings>
    {
        private List<MyMinion> MainMinions = new List<MyMinion>();//WickerMan and KaomWarrior(Tukohama's Vanguard)
        private List<MyMinion> MeatMinions = new List<MyMinion>();//for Raise zombie
        private List<EntityWrapper> AllMinions = new List<EntityWrapper>();
 

        public override void Initialise()
        {
            GameController.Area.OnAreaChange += Area_OnAreaChange;
            
            var item = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory][0, 0, 12];
            if (item == null)
            {
                LogMessage("Not found!", 10);
                return;
            }
            LogMessage(item.Path, 10);
            LogMessage(item.Address.ToString("x"), 10);
            
        }

        private void Area_OnAreaChange(PoeHUD.Controllers.AreaController obj)
        {
            MainMinions = new List<MyMinion>();
            MeatMinions = new List<MyMinion>();
        }

        public override void EntityAdded(EntityWrapper entity)
        {
            if (Settings.Enable && entity != null && !AllMinions.Contains(entity) && entity.HasComponent<Monster>() && !entity.IsHostile)
            {
                AllMinions.Add(entity);

           
                string path = entity.Path;
                if (string.IsNullOrEmpty(path)) return;

                if (path.ToLower().Contains("totem") || path.ToLower().Contains("golem"))
                    return;

                if (path.Contains("SummonedSpectralWolf"))
                    return;

                var newMinion = new MyMinion();
                newMinion.Entity = entity;

                if (path.Contains("@"))
                {
                    int.TryParse(path.Substring(path.LastIndexOf("@") + 1), out newMinion.Level);
                }

                if (path.Contains("Metadata/Monsters/WickerMan/WickerMan"))
                {
                    newMinion.Type = 2;
                    newMinion.Name = "WickerMan";
                    MainMinions.Add(newMinion);
                }
                else if (path.Contains("Metadata/Monsters/AnimatedItem/AnimatedArmour"))
                {
                    newMinion.Type = 3;
                    newMinion.Name = "Animated Guardian";
                    MainMinions.Add(newMinion);
                }
                else if (path.Contains("Metadata/Monsters/KaomWarrior/KaomWarrior"))
                {
                    newMinion.Type = 1;
                    newMinion.Name = "KaomWarrior";
                    MainMinions.Add(newMinion);
                }    
                else
                {
                    newMinion.Name = "Meat";
                    MeatMinions.Add(newMinion);
                }

                MainMinions = MainMinions.OrderByDescending(x => x.Type).ToList();
            }
        }

        public override void EntityRemoved(EntityWrapper entity)
        {
            AllMinions.Remove(entity);
            MainMinions.RemoveAll(x => x.Entity == entity);
            MeatMinions.RemoveAll(x => x.Entity == entity);
        }

        private class MyMinion
        {
            public EntityWrapper Entity;
            public int Level;
            public string Name;
            public int Type;//1 - wicker, 2 - kaom
        }

        public override void Render()
        {
            if (!GameController.InGame) return;

            var ui = GameController.Game.IngameState.IngameUi;

            if (ui.AtlasPanel.IsVisible) return;
            if (ui.InventoryPanel.IsVisible) return;
            if (ui.OpenLeftPanel.IsVisible) return;
            if (ui.OpenRightPanel.IsVisible) return;
            if (ui.TreePanel.IsVisible) return;

            var mainRect = new RectangleF(Settings.PosX.Value, Settings.PosY.Value, Settings.Width.Value, Settings.Height.Value);


            foreach (var minion in MainMinions.ToList())
            {
                var lifeComp = minion.Entity.GetComponent<Life>();
                
                if(lifeComp.CurES == 0 && lifeComp.CurHP == 0)
                {
                    MainMinions.Remove(minion);
                    continue;
                }

                float perc = (float)lifeComp.CurHP / lifeComp.MaxHP;

                DrawMinionLifebar(mainRect, perc);


                byte byteOpacity = (byte)(Settings.Opacity.Value * 255);
                var color3 = Color.White;
                color3.A = byteOpacity;

                var textSize = Math.Min(Settings.Height.Value, 30);


                Graphics.DrawText(perc.ToString("P0"), textSize, mainRect.Center, color3, FontDrawFlags.Center | FontDrawFlags.VerticalCenter);
                Graphics.DrawText(minion.Level + "lvl, " + lifeComp.MaxHP.KiloFormat() + " hp", textSize, mainRect.TopRight + new Vector2(-10, 0), color3, FontDrawFlags.Right);

                if (!string.IsNullOrEmpty(minion.Name))
                    Graphics.DrawText(minion.Name, textSize, mainRect.TopLeft + new Vector2(10, 0), color3, FontDrawFlags.Top);

                mainRect.Y -= Settings.Height.Value + 5;
            }

            mainRect.Y -= 10;

            var meatMinionCount = MeatMinions.Count;
            var partialLineWidth = mainRect.Width / meatMinionCount;
            var partialDrawRect = mainRect;
            partialDrawRect.Width = partialLineWidth;

            foreach (var minion in MeatMinions.ToList())
            {
                var lifeComp = minion.Entity.GetComponent<Life>();
                if (lifeComp.CurES == 0 && lifeComp.CurHP == 0)
                {
                    MeatMinions.Remove(minion);
                    continue;
                }
                float perc = (float)lifeComp.CurHP / lifeComp.MaxHP;
                DrawMinionLifebar(partialDrawRect, perc);
                partialDrawRect.X += partialDrawRect.Width;
            }
            mainRect.Y -= 30;

            var miniosCountRect = mainRect;
            miniosCountRect.Width = 40;// Settings.Height.Value * 1.25f;
            miniosCountRect.Height = 30;// Settings.Height.Value;

            Graphics.DrawBox(miniosCountRect, Color.Black);
            Graphics.DrawFrame(miniosCountRect, 2, Color.White);
            Graphics.DrawText(meatMinionCount.ToString(), 35, miniosCountRect.Center, FontDrawFlags.Center | FontDrawFlags.VerticalCenter);


            var playerLife = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>();

            var offering = playerLife.Buffs.Find(x => x.Name == "active_offering");

            if(offering != null)
            {
                mainRect.X += miniosCountRect.Width + 5;
                mainRect.Width -= miniosCountRect.Width + 5;

                if (OfferingBuffAddr != offering.Address)
                {
                    OfferingBuffAddr = offering.Address;
                    OfferingActivationTime = DateTime.Now;
                }

                Graphics.DrawBox(mainRect, Color.Black);
                Graphics.DrawFrame(mainRect, 2, Color.White);

                var seconds = (float)((DateTime.Now - OfferingActivationTime).TotalSeconds);

                DrawMinionLifebar(mainRect, 1 - (seconds / Settings.OfferingDuration.Value), true);
                //Graphics.DrawText(Math.Round(Settings.OfferingDuration.Value - seconds, 1).ToString(), 25, mainRect.Center, FontDrawFlags.Center | FontDrawFlags.VerticalCenter);
            }
            else if(OfferingBuffAddr != 0)
            {
                OfferingBuffAddr = 0;
            }
        }

        private long OfferingBuffAddr = 0;
        private DateTime OfferingActivationTime;

        private void DrawMinionLifebar(RectangleF drawRect, float healthPerc, bool offering = false)
        {
            byte byteOpacity = (byte)(Settings.Opacity.Value * 255);
            var color0 = Settings.BGColor.Value;
            color0.A = byteOpacity;

            Graphics.DrawBox(drawRect, color0);


            var scaleRect = drawRect;
            scaleRect.Width *= healthPerc;
            var color = Settings.HpColor.Value;

            if(offering)
            {
                color = Color.Yellow;
            }
            else if (Settings.Gradient.Value)
            {
                color = Color.Lerp(Color.Red, Settings.HpColor.Value, healthPerc);
            }

            color.A = byteOpacity;
            Graphics.DrawBox(scaleRect, color);

            var color2 = Settings.BorderColor.Value;
            color2.A = byteOpacity;
            Graphics.DrawFrame(drawRect, 1, color2);
        }
    }

    public static class Extensions
    {
        public static string KiloFormat(this int num)
        {
            if (num >= 100000000)
                return (num / 1000000).ToString("#,0M");

            if (num >= 10000000)
                return (num / 1000000).ToString("0.#") + "M";

            if (num >= 100000)
                return (num / 1000).ToString("#,0K");

            if (num >= 10000)
                return (num / 1000).ToString("0.#") + "K";

            return num.ToString("#,0");
        }
    }
}