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
string CargoTag = "Cargo";

        // Variables
        private Logger _logger;
        private MyItemType _stoneType = MyItemType.Parse("MyObjectBuilder_Ore/Stone");
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            _logger = new Logger(Me, 100);
            _logger.Clear();
            _logger.LogTrace($"Logger initialized");
        }
        public void Main(string args)
        {
            var connectors = new List<IMyShipConnector>();
            var ejectors = new List<IMyShipConnector>();
            var cargos = new List<IMyCargoContainer>();

            GridTerminalSystem.GetBlocksOfType(connectors, block => block.CustomName.Contains(ConnectorTag));
            GridTerminalSystem.GetBlocksOfType(ejectors, block => block.CustomName.Contains(EjectorTag));
            GridTerminalSystem.GetBlocksOfType(cargos, block => block.CustomName.Contains(CargoTag));

            if (ejectors.Count == 0)
            {
                _logger.LogTrace("No ejectors found");
                return;
            }

            // Rule 1: if any connector is docked, skip entirely
            foreach (var connector in connectors)
            {
                if (connector.Status == MyShipConnectorStatus.Connected)
                {
                    _logger.LogTrace($"Connector {connector.CustomName} is docked, aborting");
                    return;
                }
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
                        foreach (var cargo in cargos)
                        {
                            var cargoInventory = cargo.GetInventory();
                            if (cargoInventory.IsFull)
                                continue;

                            var result = ejectorInventory.TransferItemTo(cargoInventory, item, item.Amount);
                            if (result)
                            {
                                _logger.LogTrace($"Moved {item.Amount} {item.Type} to {cargo.CustomName}");
                                break;
                            }
                        }
                    }
                    continue;
                }

                // Rule 3: ejector has only stone (or is empty) — enable throw out
                _logger.LogTrace($"Ejector {ejector.CustomName} is clean, enabling throw out");
                ejector.ThrowOut = true;

                // Rule 4: fill ejector with stone from cargo
                foreach (var cargo in cargos)
                {
                    if (ejectorInventory.IsFull)
                        break;

                    var cargoInventory = cargo.GetInventory();
                    if (!cargoInventory.ContainItems(1, _stoneType))
                        continue;

                    var stone = cargoInventory.FindItem(_stoneType).Value;
                    _logger.LogTrace($"Moving {stone.Amount} stone from {cargo.CustomName} to {ejector.CustomName}");
                    cargoInventory.TransferItemTo(ejectorInventory, stone, stone.Amount);
                }
            }
        }

        #region PreludeFooter
    }
}
#endregion
