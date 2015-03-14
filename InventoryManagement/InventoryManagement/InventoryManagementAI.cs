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

namespace SpaceEngineers
{
    class InventoryManagementAI
    {
        IMyGridTerminalSystem GridTerminalSystem = null;

        /* 
         * Save a few programmable blocks, all the scripts combined!
         * Due to heavy code reuse, an efficient combination needs
         * to be done by hand. Last combined state was after SHA:
         * 32cf502ec92f1697263c84502d203d9f3b48366e
         */
        #region CodeEditor

        //User Customization Variables Start
        String INGOT_STORAGE = "Ingot";
        String COMPONENT_STORAGE = "Comp";
        String ORE_STORAGE = "Ore";
        String STONE_STORAGE = "Stone"; 
        String lcdname = "Cargo Status";
        String trashContainerName = "trash";
        //User Customization Variables End

        List<IMyTerminalBlock> containers = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> oreProcessors = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> assemblers = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> refineries = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> arcFurnaces = new List<IMyTerminalBlock>();
        List<string> arcMetals = new List<string>();
        IMyTextPanel lcd;

        long maxvol = 0;
        long curvol = 0;
        long trashmaxvol = 0;
        long trashcurvol = 0;

        void Main()
        {
            initBlocks();
            redistributeArc();
            redistribute(arcFurnaces);
            redistribute(refineries);
            cleanOutRefineries();
            cleanOutAssemblers();

            if (lcd == null)
            {
                //Skip counting if we wouldn't print it out
                return;
            }

            //From CargoPercent.cs
            maxvol = 0;
            curvol = 0;
            trashmaxvol = 0;
            trashcurvol = 0;
            for (int i = 0; i < containers.Count; i++)
            {
                IMyCargoContainer container = (IMyCargoContainer)containers[i];

                // count trash
                if (container.DisplayNameText.ToLower().Contains(trashContainerName))
                {
                    trashmaxvol += (long)container.GetInventory(0).MaxVolume;
                    trashcurvol += (long)container.GetInventory(0).CurrentVolume;
                }
                else
                {
                    maxvol += (long)container.GetInventory(0).MaxVolume;
                    curvol += (long)container.GetInventory(0).CurrentVolume;
                }
            }
            if (maxvol > 0)
            {
                lcd.WritePublicText("Storage: " + ((curvol * 100) / maxvol) + "%\n", false);
            }
            else
            {
                lcd.WritePublicText("Storage: 0%\n", false);
            }
            if (trashmaxvol > 0)
            {
                lcd.WritePublicText("Trash: " + ((trashcurvol * 100) / trashmaxvol) + "%", true);
            }
            else
            {
                lcd.WritePublicText("Trash: 0%", true);
            }
        }

        void initBlocks()
        {
            refineries.Clear();
            arcFurnaces.Clear();
            containers.Clear();
            oreProcessors.Clear();
            assemblers.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(containers);
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(oreProcessors);
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblers);

            for (int i = 0; i < oreProcessors.Count; i++)
            {
                IMyRefinery oreProcessor = (IMyRefinery)oreProcessors[i];

                if (oreProcessor.DisplayNameText.ToLower().Contains("furnace"))
                {
                    arcFurnaces.Add(oreProcessor);
                }
                else
                {
                    refineries.Add(oreProcessor);
                }
            }
            //Don't know how to extract this from the game, but is unlikely to change
            arcMetals.Add("Iron");
            arcMetals.Add("Cobalt");
            arcMetals.Add("Nickel");

            if (lcd == null)
                lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(lcdname);
        }

        bool redistribute(List<IMyTerminalBlock> blocks)
        {
            IMyRefinery fullest = null;
            long fullestAmount = 0;
            IMyRefinery empty = null;
            for (int i = 0; i < blocks.Count; i++)
            {
                IMyRefinery test = (IMyRefinery)blocks[i];
                long mass = (long)test.GetInventory(0).CurrentMass;

                if (mass == 0)
                {
                    if (empty == null)
                        empty = test;
                    continue;
                }

                if (mass > fullestAmount)
                {
                    fullest = test;
                    fullestAmount = mass;
                }
            }

            if (empty == null || fullest == null)
            {
                return true;
            }

            IMyInventory inv = fullest.GetInventory(0);
            inv.TransferItemTo(empty.GetInventory(0), 0, 0, true, inv.GetItems()[0].Amount * 0.5F);
            return false;
        }

        void redistributeArc()
        {
            //Will move arc-able ores from non-empty refineries to empty arc furnaces.
            List<IMyRefinery> arcShortlist = new List<IMyRefinery>();
            for (int j=0; j < arcFurnaces.Count; j++) {
                IMyRefinery arc = (IMyRefinery)arcFurnaces[j];
                if (arc.GetInventory(0).CurrentMass == 0)
                {
                    arcShortlist.Add(arc);
                }
            }

            if (arcShortlist.Count == 0)
            {
                return;
            }

            for (int i = 0; i < refineries.Count; i++)
            {
                IMyRefinery refinery = (IMyRefinery)refineries[i];
                int arcIdx = 0;
                IMyInventory inv = refinery.GetInventory(0);  
                for (int j = 0; j < inv.GetItems().Count; j++)
                {
                    if (arcMetals.Contains(inv.GetItems()[j].Content.SubtypeId.ToString()))
                    {
                        IMyRefinery other = arcShortlist[arcIdx];
                        VRage.MyFixedPoint amount = inv.GetItems()[j].Amount * 0.5F;
                        if (amount < (VRage.MyFixedPoint)100.0F)
                        {
                            //For small amounts stuck high in the queue, it can get stuck dividing
                            //smaller and smaller chunks that the furnace can process in an instant
                            amount = inv.GetItems()[j].Amount;
                        }

                        inv.TransferItemTo(other.GetInventory(0), j, 0, true, amount);
                        arcIdx += 1;
                        if (arcIdx >= arcShortlist.Count)
                        {
                            return;
                        }

                    }
                }
            }
        }

        void cleanOutRefineries()
        {
            for (int i = 0; i < oreProcessors.Count; i++)
            {
                IMyRefinery refinery = (IMyRefinery)oreProcessors[i];

                // move mats
                IMyInventory inv = refinery.GetInventory(1);
                IMyCargoContainer stoneContainer = findCargo(inv, STONE_STORAGE); 
                transferStoneTo(inv, stoneContainer.GetInventory(0));
                IMyCargoContainer container = findCargo(inv, INGOT_STORAGE);
                transferAllTo(inv, container.GetInventory(0));

                inv = refinery.GetInventory(0);
                transferStoneTo(inv, stoneContainer.GetInventory(0));
                container = findCargo(inv, ORE_STORAGE);
                transferAllTo(inv, container.GetInventory(0));
            }
        }

        void cleanOutAssemblers()
        {
            for (int i = 0; i < assemblers.Count; i++)
            {
                IMyAssembler assem = (IMyAssembler)assemblers[i];

                // move parts
                IMyInventory inv = assem.GetInventory(1);
                IMyCargoContainer container = findCargo(inv, COMPONENT_STORAGE);
                transferAllTo(inv, container.GetInventory(0));

                inv = assem.GetInventory(0);
                IMyCargoContainer stoneContainer = findCargo(inv, STONE_STORAGE); 
                transferStoneTo(inv, stoneContainer.GetInventory(0));
                container = findCargo(inv, INGOT_STORAGE);
                transferAllTo(inv, container.GetInventory(0));
            }
        }

        void transferAllTo(IMyInventory source, IMyInventory dest)
        {
            if (dest == null)
            {
                return;
            }

            while (source.GetItems().Count > 0)
            {
                source.TransferItemTo(dest, 0, null, true, null);
            }

        }

        void transferStoneTo(IMyInventory source, IMyInventory dest) 
        { 
            if (dest == null)
            {
                return;
            }

            //This collects both ingot and ore type "Stone" 
            List<int> stoneIdxs = new List<int>(); 
            List<IMyInventoryItem> sourceItems = source.GetItems(); 
            for (int i = 0; i < sourceItems.Count; i++) 
            { 
                if (sourceItems[i].Content.SubtypeId.ToString() == "Stone") 
                { 
                    stoneIdxs.Add(i); 
                } 
            } 
 
            for (int i = stoneIdxs.Count - 1; i >= 0; i--) 
            { 
                source.TransferItemTo(dest, stoneIdxs[i], null, true, null); 
            } 
        } 

        IMyCargoContainer findCargo(IMyInventory sibling, String type)
        {
            IMyCargoContainer selected = null;
            for (int i = 0; i < containers.Count; i++)
            {
                IMyCargoContainer container = (IMyCargoContainer)containers[i];

                if (!container.GetInventory(0).IsFull && sibling.IsConnectedTo(container.GetInventory(0)))
                {
                    if (container.DisplayNameText.Contains(type))
                    {
                        return container;
                    }
                    else
                    {
                        selected = container;
                    }
                }
            }
            return selected;
        }

        #endregion
    }
}
