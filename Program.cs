using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        LCD lcd;
        public Program()
        {
            lcd = new LCD(this, Me.GetSurface(0));
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        public void Save()
        {
        }

        string markup;  // test a markdown format for lcds
        public void Main(string argument, UpdateType updateSource)
        {
//            string ipsm = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
            string ipsm = "Lorem ipsum dolor sit amet, consectetur adipiscing elit...";
            lcd._debug = false;
            lcd
                .Animate(50)
                .Start()

                .Font("White").SpriteSize(10,100).FontSize(2).NewLine(LCD.NewLineCaculation.TEXT)
                .AddText("No Marquee.")

                .NewLine(LCD.NewLineCaculation.TEXT)
                .AddMarquee(ipsm, 0.1f)
                
                .NewLine(LCD.NewLineCaculation.TEXT)
                .AddMarquee("12345", 1.0f)
                //.TrimPadding(18, 11).Font("Debug").FontSize(1).NewLine(LCD.BOTH)
                //.Font("White").FontSize(1).AddText("|||")
                //.AddText("X")

                //.FontSize(2).NewLine().FontSize(1).AddText("XYZ")

                //.Font("White").FontSize(2.0f).AddText("1234")
                //.FontSize(0.6f).AddText("IJKL")
                //.SpriteSize(512, 1).NewLine(LCD.NEITHER).AddSprite("SquareSimple")

                //.TrimPadding(0, 0).FontSize(2.0f).NewLine()
                //.AddText("Padding Back On")

                //.FontSize(2.0f).SpriteSize(40, 43).NewLine()
                //.AddText("O2/H2 indicator:")
                //.AddSpriteAnimation(new string[] { "IconHydrogen", "IconOxygen" }, 1.0f)

                //.TrimPadding(-10, 14).FontSize(0.4f).NewLine(LCD.TEXT)
                //.AddText("Extra padding on top")

                //.Form(markup)
                .End();

        }
    }
}
