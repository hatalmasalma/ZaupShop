using System.Collections.Generic;
using System.Linq;
using fr34kyn01535.Uconomy;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;

namespace ZaupShop.Commands
{
    public class CommandSell : IRocketCommand
    {
        #region Boilerplate
        
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "sell";
        public string Help => "Allows you to sell items to the shop from your inventory.";
        public string Syntax => "<name or id> [amount]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>();
        
        #endregion
        
        public void Execute(IRocketPlayer player, string[] command)
        {
            UnturnedPlayer uPlayer = (UnturnedPlayer) player;
            SteamPlayer steamPlayer = PlayerTool.getSteamPlayer(uPlayer.CSteamID);
            
            if (command.Length == 0 || command.Length > 0 && command[0].Trim() == string.Empty)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "sell_command_usage");
                return;
            }

            byte amount = 1;
            if (command.Length > 1)
            {
                if (!byte.TryParse(command[1], out amount))
                {
                    ZaupShop.Instance.TellPlayer(steamPlayer, "invalid_amt");
                    return;
                }

                if (amount == 0)
                {
                    ZaupShop.Instance.TellPlayer(steamPlayer, "invalid_amt");
                    return;
                }
            }

            if (!ZaupShop.Instance.Configuration.Instance.CanSellItems)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "sell_items_off");
                return;
            }

            ItemAsset itemAsset = AssetUtils.GetItem(command[0]);

            if (itemAsset == null)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "could_not_find", command[0]);
                return;
            }

            ushort itemID = itemAsset.id;
            string itemName = itemAsset.itemName;
            byte itemAmount = itemAsset.amount;

            List<InventorySearch> playerItems = uPlayer.Inventory.search(itemID, true, true);

            if (playerItems.Count == 0)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "not_have_item_sell", itemName);
                return;
            }

            if (itemAmount == 1 && playerItems.Count < amount)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "not_enough_items_sell", amount, itemName);
                return;
            }

            if (itemAmount > 1)
            {
                byte ammoMags = 0;
                foreach (InventorySearch item in playerItems)
                    ammoMags += item.jar.item.amount;

                if (ammoMags < amount)
                {
                    ZaupShop.Instance.TellPlayer(steamPlayer, "not_enough_ammo_sell", itemName);
                    return;
                }
            }

            decimal price = ZaupShop.Instance.ShopDB.GetItemBuyPrice(itemID);

            if (price <= 0m)
            {
                ZaupShop.Instance.TellPlayer(steamPlayer, "no_sell_price_set", itemName);
                return;
            }

            decimal income = 0;
            
            switch (itemAmount)
            {
                case 1:
                    income = GetSellItemIncome(uPlayer, playerItems, price, amount);
                    break;
                default:
                    income = GetSellAmmoIncome(uPlayer, playerItems, price, amount, itemAmount);
                    break;
            }

            decimal balance = Uconomy.Instance.Database.IncreaseBalance(uPlayer.CSteamID.ToString(), income);
            string currencyName = Uconomy.Instance.Configuration.Instance.MoneyName;

            ZaupShop.Instance.TellPlayer(steamPlayer, "sold_items", amount, itemName, income, currencyName, balance,
                currencyName);
            
            ZaupShop.Instance.RaiseSellItem(uPlayer, income, amount, itemID);
        }

        private decimal GetSellItemIncome(UnturnedPlayer uPlayer, List<InventorySearch> playerItems, decimal price, byte amount)
        {
            decimal finalIncome = 0;

            for (byte b = 0; b < amount; b++)
            {
                InventorySearch searchItem = playerItems[b];
                
                byte itemPage = searchItem.page;
                byte itemX = searchItem.jar.x;
                byte itemY = searchItem.jar.y;
                byte quality = 100;
                
                if (uPlayer.Player.equipment.checkSelection(itemPage, itemX, itemY))
                    uPlayer.Player.equipment.dequip();

                if (ZaupShop.Instance.Configuration.Instance.QualityCounts)
                    quality = searchItem.jar.item.durability;

                decimal finalPrice = decimal.Round(price * (quality / 100.0m), 2);
                finalIncome += finalPrice;
                
                uPlayer.Inventory.removeItem(itemPage, uPlayer.Inventory.getIndex(itemPage, itemX, itemY));
            }

            return finalIncome;
        }

        private decimal GetSellAmmoIncome(UnturnedPlayer uPlayer, List<InventorySearch> playerItems, decimal price, byte amount, byte originalItemAmount)
        {
            byte totalAmount = amount;
            
            for (byte b = 0; b < amount; b++)
            {
                InventorySearch searchItem = playerItems[b];
                
                byte itemPage = searchItem.page;
                byte itemX = searchItem.jar.x;
                byte itemY = searchItem.jar.y;

                if (uPlayer.Player.equipment.checkSelection(itemPage, itemX, itemY)) 
                    uPlayer.Player.equipment.dequip();

                byte itemAmount = searchItem.jar.item.amount;

                if (itemAmount >= amount)
                {
                    byte remaining = (byte)(itemAmount - amount);

                    if (remaining != 0)
                    {
                        searchItem.jar.item.amount = remaining;
                        uPlayer.Inventory.sendUpdateAmount(itemPage, itemX, itemY, remaining);
                    }
                    else 
                        uPlayer.Inventory.removeItem(itemPage, uPlayer.Inventory.getIndex(itemPage, itemX, itemY));
                    
                    break;
                }

                amount -= itemAmount;
                uPlayer.Inventory.removeItem(itemPage, uPlayer.Inventory.getIndex(itemPage, itemX, itemY));
            }

            return decimal.Round(price * (totalAmount / (decimal)originalItemAmount), 2);
        }
    }
}