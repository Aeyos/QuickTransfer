#region Prelude
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using System;
using System.Linq;
using VRageMath;
using System.Text;
using SpaceEngineers.CommonLibs;
using VRage.Game.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;
using Sandbox.Game.WorldEnvironment.Definitions;

namespace SpaceEngineers.UWBlockPrograms.SampleMainNs
{
    public sealed class Program : MyGridProgram
    {
        #endregion
// Configuration
string ConnectorTag = "Connector";
string EjectorTag = "Ejector";

        // Variables
        private Logger _logger;
        private MyItemType _stoneType = MyItemType.Parse("MyObjectBuilder_Ore/Stone");
        private bool _enabled = true;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            _logger = new Logger(Me, 100);
            _logger.Clear();
            _logger.LogTrace($"Logger initialized");

            if (!string.IsNullOrEmpty(Storage))
                bool.TryParse(Storage, out _enabled);
        }

        public void Main(string args)
        {
            if (args.Equals("ON", StringComparison.OrdinalIgnoreCase) || args.Equals("OFF", StringComparison.OrdinalIgnoreCase))
            {
                SetEnabled(args.Equals("ON", StringComparison.OrdinalIgnoreCase));
                return;
            }

            if (!_enabled)
            {
                _logger.LogTrace("Script is disabled");
                return;
            }

            var connectors = new List<IMyShipConnector>();
            var ejectors = new List<IMyShipConnector>();
            var allBlocks = new List<IMyTerminalBlock>();

            GridTerminalSystem.GetBlocksOfType(connectors, block => block.CustomName.Contains(ConnectorTag));
            GridTerminalSystem.GetBlocksOfType(ejectors, block => block.CustomName.Contains(EjectorTag));
            GridTerminalSystem.GetBlocksOfType(allBlocks, block => block.HasInventory && !block.CustomName.Contains(EjectorTag));

            // Flatten all inventories from non-ejector blocks into one list
            var inventories = new List<IMyInventory>();
            foreach (var block in allBlocks)
                for (int i = 0; i < block.InventoryCount; i++)
                    inventories.Add(block.GetInventory(i));

            // Rule 1: if any connector is docked, skip entirely
            foreach (var connector in connectors)
            {
                if (connector.Status == MyShipConnectorStatus.Connected)
                {
                    _logger.LogTrace($"Connector {connector.CustomName} is docked, aborting");
                    return;
                }
            }

            if (ejectors.Count == 0)
            {
                _logger.LogTrace("No ejectors found");
                return;
            }

            foreach (var ejector in ejectors)
            {
                var ejectorInventory = ejector.GetInventory();
                var nonStoneItems = new List<MyInventoryItem>();
                ejectorInventory.GetItems(nonStoneItems, item => item.Type != _stoneType);

                // Rule 2: ejector has non-stone items — disable throw out and clean it out
                if (nonStoneItems.Count > 0)
                {
                    _logger.LogTrace($"Ejector {ejector.CustomName} has non-stone items, disabling throw out");
                    ejector.ThrowOut = false;

                    foreach (var item in nonStoneItems)
                    {
                        foreach (var inv in inventories)
                        {
                            if (inv.IsFull)
                                continue;

                            var result = ejectorInventory.TransferItemTo(inv, item, item.Amount);
                            if (result)
                            {
                                _logger.LogTrace($"Moved {item.Amount} {item.Type} out of {ejector.CustomName}");
                                break;
                            }
                        }
                    }
                    continue;
                }

                // Rule 3: ejector has only stone (or is empty) — enable throw out
                _logger.LogTrace($"Ejector {ejector.CustomName} is clean, enabling throw out");
                ejector.ThrowOut = true;

                // Rule 4: fill ejector with stone from any inventory
                foreach (var inv in inventories)
                {
                    if (ejectorInventory.IsFull)
                        break;

                    if (!inv.ContainItems(1, _stoneType))
                        continue;

                    var stone = inv.FindItem(_stoneType).Value;
                    _logger.LogTrace($"Moving {stone.Amount} stone to {ejector.CustomName}");
                    inv.TransferItemTo(ejectorInventory, stone, stone.Amount);
                }
            }
        }

        private void SetEnabled(bool newState)
        {
            bool prevState = _enabled;
            _enabled = newState;
            Storage = _enabled.ToString();

            string currentTag = newState ? "[ON]" : "[OFF]";
            string prevTag = newState ? "[OFF]" : "[ON]";
            string name = Me.CustomName;

            if (!name.Contains("[ON]") && !name.Contains("[OFF]"))
                Me.CustomName = name + " " + currentTag;
            else if (prevState != newState)
                Me.CustomName = name.Replace(prevTag, currentTag);

            _logger.LogTrace($"Script {currentTag}");
        }

        #region PreludeFooter
    }
}
#endregion
