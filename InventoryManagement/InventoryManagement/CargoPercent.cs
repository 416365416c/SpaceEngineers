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
        long fullpercent = 99;
        bool autoSize = true; //If true, will automatically select and set a font size
        bool autoColor = true; //If true, will automatically select and set a font color
 
        List<IMyTerminalBlock> containers = new List<IMyTerminalBlock>(); 
        List<IMyTerminalBlock> lcds = new List<IMyTerminalBlock>(); 
 
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
            String output = "Storage:\n0%\nN/A";
            String outputWide = "Storage: 0%\nFound no\ncontainers";
            VRageMath.Color colorVar = VRageMath.Color.White;
            if (maxvol > 0) 
            { 
                //Output intent is to fit it on one screen with font size 5.0 
                //Wide Output intent is to fit it on one wide screen with font size 6.0 
                //Half for small screens
                long percent = ((curvol * 100) / maxvol); 
                output = "Storage:\n" + percent + "%"; 
                outputWide = "Storage: " + percent + "%"; 
                if (percent >= fullpercent) {
                    colorVar = VRageMath.Color.Red;
                    output += "\nAll full"; 
                    outputWide += "\nCARGO FULL"; 
                } else if (percent >= warnpercent) { 
                    colorVar = VRageMath.Color.Yellow; 
                    output += "\nNear full"; 
                    outputWide += "\nALMOST FULL";
                } else { 
                    colorVar = VRageMath.Color.Green; 
                } 
            }

            for (var i=0; i < lcds.Count; i++) {
                IMyTextPanel lcd = lcds[i] as IMyTextPanel;
                if (lcd != null) {
                    String subtypeId = getSubtypeId(lcd);
                    if (autoColor)
                        lcd.SetValue<Color>("FontColor", colorVar); 
                    if (subtypeId.Contains("Wide")) {
                        lcd.WritePublicText(outputWide, false); 
                        if (autoSize) 
                            lcd.SetValueFloat("FontSize", 6.0f); 
                    } else {
                        lcd.WritePublicText(output, false); 
                        //Note that small blocks don't need a small size
                        if (autoSize)
                            lcd.SetValueFloat("FontSize", 5.0f); 
                    }
                }
            }
        } 
 
        String getSubtypeId(IMyTerminalBlock block) {
            String[] parts = block.BlockDefinition.ToString().Split('/');
            return parts[parts.Length - 1];
        }

        void initBlocks()
        {
            containers.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(containers);
            lcds.Clear();
            GridTerminalSystem.SearchBlocksOfName(lcdname, lcds);
        }
        #endregion
    }
}
