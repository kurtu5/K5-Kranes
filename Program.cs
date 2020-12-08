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
            markup =
$@"
Invent some things here.
Ex.  [flag ?  lcd.sprite('spinner') : lcd.sprite('not spinner')]
mass = [mass]
or make it like a form?
lcd.addtext(blah, x,y?)

nah.. lets hide complexity
";
            lcd
                .Animate(50)
                .Start()
                .SetFont("White", 2.0f)
                .NewLine()
                .AddText("ASDF")
                .SetFont("White", 0.5f)
                .AddText("1234")
                .SetFont("White", 1.5f)
                .AddText("IJKL")
                //.CursorNewline("1234")
                //.AddText("ASDF1234")
                .Form(markup)
                .End();

            //lcd.Animate(50); // move this inside lcd to detect if animation is desired in ml?
            //lcd.Start();
            //lcd.AddText("asdf");
            //// do we really want to regen the form for every update?  for now i will
            //lcd.Form(markup); 
            
            //lcd.End();  // if form is generated each time, this is extra code...  just update in form
        }
    }
}
