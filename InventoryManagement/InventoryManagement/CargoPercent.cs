using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders;
using VRage;
using VRage.MyFixedPoint;

namespace InventoryManagement
{
    class CargoPercent
    {
        IMyGridTerminalSystem GridTerminalSystem = null;

        #region CodeEditor
        String lcdname = "Cargo Status";
        long warnpercent = 80; 
 
        List<IMyTerminalBlock> containers = new List<IMyTerminalBlock>(); 
        IMyTextPanel lcd; 
 
        long maxvol = 0; 
        long curvol = 0; 
 
        void Main() 
        { 
            maxvol = 0; 
            curvol = 0; 
            initBlocks(); 
 
            for (int i = 0; i < containers.Count; i++) 
            { 
                IMyCargoContainer container = (IMyCargoContainer)containers[i]; 
 
                    maxvol += (long)container.GetInventory(0).MaxVolume; 
                    curvol += (long)container.GetInventory(0).CurrentVolume; 
 
            }
            if (maxvol > 0) 
            { 
                long percent = ((curvol * 100) / maxvol); 
                String output = "Storage:" + percent + "%"; 
                //Note intent is to fit it on one screen with font size 3.0 
                output += "\nLeft: " + (maxvol - curvol) + "L"; 
                if (percent > 99.999) { //100 minus epsilon 
                    lcd.SetValue<Color>("FontColor", VRageMath.Color.Red); 
                    output += "\nCARGO FULL"; 
                } else if (percent > warnpercent) { 
                    lcd.SetValue<Color>("FontColor", VRageMath.Color.Yellow); 
                    output += "\nNEAR FULL"; 
                } else { 
                    lcd.SetValue<Color>("FontColor", VRageMath.Color.Green); 
                } 
                lcd.WritePublicText(output, false); 
            } 
            else 
            { 
                lcd.SetValue<Color>("FontColor", VRageMath.Color.Red); 
                lcd.WritePublicText("Storage: 0%\nFound no\ncontainers", false); 
            } 
 
        } 
 
        void initBlocks() 
        { 
            containers.Clear(); 
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(containers); 
            if (lcd == null) 
                lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(lcdname); 
        }
        #endregion
    }
}
