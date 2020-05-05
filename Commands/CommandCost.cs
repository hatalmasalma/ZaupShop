using System.Collections.Generic;
using System.Management.Instrumentation;
using fr34kyn01535.Uconomy;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace ZaupShop.Commands
{
    public class CommandCost : IRocketCommand
    {
        #region Boilerplate
        
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "cost";
        public string Help => "Tells you the cost of a selected item.";
        public string Syntax => "[v.]<name or id>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>();
        
        #endregion

        public void Execute(IRocketPlayer player, string[] command)
        {
            UnturnedPlayer uPlayer = (UnturnedPlayer) player;
            SteamPlayer steamPlayer = PlayerTool.getSteamPlayer(uPlayer.CSteamID);
            
            // Tell the player how to use the command when misused
            switch (command.Length)
            {
                case 0:
                case 1 when command[0].Trim() == string.Empty || command[0].Trim() == "v":
                    ZaupShop.Instance.TellPlayer(steamPlayer, "cost_command_usage");
                    return;
                case 2 when command[0] != "v" || command[1].Trim() == string.Empty:
                    ZaupShop.Instance.TellPlayer(steamPlayer, "cost_command_usage");
                    return;
            }

            switch (command[0])
            {
                case "v":
                    GetVehiclePrice(command[1], steamPlayer);
                    break;
                default:
                    GetItemPrice(command[1], steamPlayer);
                    break;
            }
        }

        private void GetVehiclePrice(string vehicle, SteamPlayer steamPlayer)
        {
            VehicleAsset vehicleAsset = AssetUtils.GetVehicle(vehicle);

            if (vehicleAsset == null)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "could_not_find", vehicle);
                return;
            }

            ushort vehicleID = vehicleAsset.id;
            string vehicleName = vehicleAsset.vehicleName;
            
            decimal price = ZaupShop.Instance.ShopDB.GetVehicleCost(vehicleID);

            if (price <= 0m)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "error_getting_cost", vehicleName);
                return;
            }
            
            ZaupShop.Instance.TellPlayer(steamPlayer, "vehicle_cost_msg", vehicleName, price,
                Uconomy.Instance.Configuration.Instance.MoneyName);
        }

        private void GetItemPrice(string item, SteamPlayer steamPlayer)
        {
            ItemAsset itemAsset = AssetUtils.GetItem(item);
            
            if (itemAsset == null)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "could_not_find", item);
                return;
            }

            ushort itemID = itemAsset.id;
            string itemName = itemAsset.itemName;
            
            decimal cost = ZaupShop.Instance.ShopDB.GetItemCost(itemID);
            decimal bbp = ZaupShop.Instance.ShopDB.GetItemBuyPrice(itemID);
            string currencyName = Uconomy.Instance.Configuration.Instance.MoneyName;

            if (cost <= 0m)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "error_getting_cost", itemName);
                return;
            }

            ZaupShop.Instance.TellPlayer(steamPlayer, "item_cost_msg", itemName, cost, currencyName, bbp, currencyName);
        }
    }
}