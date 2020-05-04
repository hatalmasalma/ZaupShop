using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using fr34kyn01535.Uconomy;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace ZaupShop.Commands
{
    public class CommandBuy : IRocketCommand
    {
        #region Boilerplate
        
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "buy";
        public string Help => "Allows you to buy items from the shop.";
        public string Syntax => "[v.]<name or id> [amount] [25 | 50 | 75 | 100]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>();

        #endregion

        public void Execute(IRocketPlayer player, string[] command)
        {
            UnturnedPlayer uPlayer = (UnturnedPlayer) player;
            SteamPlayer steamPlayer = PlayerTool.getSteamPlayer(uPlayer.CSteamID);

            if (command.Length == 0)
            {
                // We are going to print how to use
                ZaupShop.Instance.TellPlayer(steamPlayer, "buy_command_usage");
                return;
            }

            byte amttobuy = 1;
            if (command.Length > 1)
            {
                if (!byte.TryParse(command[1], out amttobuy))
                {
                    ZaupShop.Instance.TellPlayer(steamPlayer, "invalid_amt");
                    return;
                }
            }

            string[] components = Parser.getComponentsFromSerial(command[0], '.');
            if (components.Length == 2 && components[0].Trim() != "v" ||
                components.Length == 1 && components[0].Trim() == "v" || components.Length > 2 ||
                command[0].Trim() == string.Empty)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "buy_command_usage");
                return;
            }

            switch (components[0])
            {
                case "v":
                    if (!ZaupShop.Instance.Configuration.Instance.CanBuyVehicles)
                    {
                        ZaupShop.Instance.TellPlayer(steamPlayer, "buy_vehicles_off");
                        return;
                    }

                    BuyVehicle(components[1], uPlayer, steamPlayer);
                    break;
                default:
                    if (!ZaupShop.Instance.Configuration.Instance.CanBuyItems)
                    {
                        ZaupShop.Instance.TellPlayer(steamPlayer, "buy_items_off");
                        return;
                    }

                    BuyItem(components[1], amttobuy, uPlayer, steamPlayer);
                    break;
            }
        }

        private void BuyVehicle(string vehicle, UnturnedPlayer uPlayer, SteamPlayer steamPlayer)
        {
            VehicleAsset vehicleAsset = AssetUtils.GetVehicle(vehicle);

            if (vehicleAsset == null)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "could_not_find", vehicle);
                return;
            }
            
            ushort vehicleID = vehicleAsset.id;
            string vehicleName = vehicleAsset.vehicleName;

            decimal vehiclePrice = ZaupShop.Instance.ShopDB.GetVehicleCost(vehicleID);
            decimal playerBalance = Uconomy.Instance.Database.GetBalance(uPlayer.CSteamID.ToString());

            if (vehiclePrice <= 0m)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "vehicle_not_available", vehicleName);
                return;
            }

            string currencyName = Uconomy.Instance.Configuration.Instance.MoneyName;

            if (playerBalance < vehiclePrice)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "not_enough_currency_msg",
                    currencyName, "1", vehicleName);
                return;
            }

            if (!uPlayer.GiveVehicle(vehicleID))
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "error_giving_item", vehicleName);
                return;
            }

            decimal newBalance =
                Uconomy.Instance.Database.IncreaseBalance(uPlayer.CSteamID.ToString(), vehiclePrice * -1);

            ZaupShop.Instance.TellPlayer(steamPlayer, "vehicle_buy_msg", vehicleName, vehiclePrice,
                currencyName, newBalance, currencyName);

            ZaupShop.Instance.RaiseBuyVehicle(uPlayer, vehiclePrice, vehicleID);
        }

        private void BuyItem(string item, byte amount, UnturnedPlayer uPlayer, SteamPlayer steamPlayer)
        {
            ItemAsset itemAsset = AssetUtils.GetItem(item);

            if (itemAsset == null)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "could_not_find", item);
                return;
            }

            ushort itemID = itemAsset.id;
            string itemName = itemAsset.itemName;
            
            decimal price = decimal.Round(ZaupShop.Instance.ShopDB.GetItemCost(itemID) * amount, 2);
            decimal playerBalance = Uconomy.Instance.Database.GetBalance(uPlayer.CSteamID.ToString());
            
            if (price <= 0m)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "item_not_available", itemName);
                return;
            }

            string currencyName = Uconomy.Instance.Configuration.Instance.MoneyName;

            if (playerBalance < price)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "not_enough_currency_msg", currencyName, amount, itemName);
                return;
            }
            
            uPlayer.GiveItem(itemID, amount);
            decimal newbal = Uconomy.Instance.Database.IncreaseBalance(uPlayer.CSteamID.ToString(), price * -1);

            ZaupShop.Instance.TellPlayer(steamPlayer, "item_buy_msg", itemName, price, currencyName, newbal,
                currencyName, amount);
            
            ZaupShop.Instance.RaiseBuyItem(uPlayer, price, amount, itemID);
        }
    }
}