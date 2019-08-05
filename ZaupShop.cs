using System.Linq;
using fr34kyn01535.Uconomy;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace ZaupShop
{
    public class ZaupShop : RocketPlugin<ZaupShopConfiguration>
    {
        public DatabaseMgr ShopDB;
        public static ZaupShop Instance;

        public delegate void PlayerShopBuy(UnturnedPlayer player, decimal amt, byte items, ushort item,
            string type = "item");

        public event PlayerShopBuy OnShopBuy;

        public delegate void PlayerShopSell(UnturnedPlayer player, decimal amt, byte items, ushort item);

        public event PlayerShopSell OnShopSell;

        public override TranslationList DefaultTranslations =>
            new TranslationList
            {
                {
                    "buy_command_usage",
                    "Usage: /buy [v.]<name or id> [amount] [quality of 25, 50, 75, or 100] (last 2 optional and only for items, default 1 amount, 100 quality)."
                },
                {
                    "cost_command_usage",
                    "Usage: /cost [v.]<name or id>."
                },
                {
                    "sell_command_usage",
                    "Usage: /sell <name or id> [amount] (optional)."
                },
                {
                    "shop_command_usage",
                    "Usage: /shop <add/rem/chng/buy> [v.]<itemid> <cost>  <cost> is not required for rem, buy is only for items."
                },
                {
                    "error_giving_item",
                    "There was an error giving you {0}.  You have not been charged."
                },
                {
                    "error_getting_cost",
                    "There was an error getting the cost of {0}!"
                },
                {
                    "item_cost_msg",
                    "The item {0} costs {1} {2} to buy and gives {3} {4} when you sell it."
                },
                {
                    "vehicle_cost_msg",
                    "The vehicle {0} costs {1} {2} to buy."
                },
                {
                    "item_buy_msg",
                    "You have bought {5} {0} for {1} {2}.  You now have {3} {4}."
                },
                {
                    "vehicle_buy_msg",
                    "You have bought 1 {0} for {1} {2}.  You now have {3} {4}."
                },
                {
                    "not_enough_currency_msg",
                    "You do not have enough {0} to buy {1} {2}."
                },
                {
                    "buy_items_off",
                    "I'm sorry, but the ability to buy items is turned off."
                },
                {
                    "buy_vehicles_off",
                    "I'm sorry, but the ability to buy vehicles is turned off."
                },
                {
                    "item_not_available",
                    "I'm sorry, but {0} is not available in the shop."
                },
                {
                    "vehicle_not_available",
                    "I'm sorry, but {0} is not available in the shop."
                },
                {
                    "could_not_find",
                    "I'm sorry, I couldn't find an id for {0}."
                },
                {
                    "sell_items_off",
                    "I'm sorry, but the ability to sell items is turned off."
                },
                {
                    "not_have_item_sell",
                    "I'm sorry, but you don't have any {0} to sell."
                },
                {
                    "not_enough_items_sell",
                    "I'm sorry, but you don't have {0} {1} to sell."
                },
                {
                    "not_enough_ammo_sell",
                    "I'm sorry, but you don't have enough ammo in {0} to sell."
                },
                {
                    "sold_items",
                    "You have sold {0} {1} to the shop and receive {2} {3} in return.  Your balance is now {4} {5}."
                },
                {
                    "no_sell_price_set",
                    "The shop is not buying {0} right now"
                },
                {
                    "no_itemid_given",
                    "An itemid is required."
                },
                {
                    "no_cost_given",
                    "A cost is required."
                },
                {
                    "invalid_amt",
                    "You have entered in an invalid amount."
                },
                {
                    "v_not_provided",
                    "You must specify v for vehicle or just an item id.  Ex. /shop rem/101"
                },
                {
                    "invalid_id_given",
                    "You need to provide a valid item or vehicle id."
                },
                {
                    "no_permission_shop_chng",
                    "You don't have permission to use the shop chng msg."
                },
                {
                    "no_permission_shop_add",
                    "You don't have permission to use the shop add msg."
                },
                {
                    "no_permission_shop_rem",
                    "You don't have permission to use the shop rem msg."
                },
                {
                    "no_permission_shop_buy",
                    "You don't have permission to use the shop buy msg."
                },
                {
                    "changed",
                    "changed"
                },
                {
                    "added",
                    "added"
                },
                {
                    "changed_or_added_to_shop",
                    "You have {0} the {1} with cost {2} to the shop."
                },
                {
                    "error_adding_or_changing",
                    "There was an error adding/changing {0}!"
                },
                {
                    "removed_from_shop",
                    "You have removed the {0} from the shop."
                },
                {
                    "not_in_shop_to_remove",
                    "{0} wasn't in the shop, so couldn't be removed."
                },
                {
                    "not_in_shop_to_set_buyback",
                    "{0} isn't in the shop so can't set a buyback price."
                },
                {
                    "set_buyback_price",
                    "You set the buyback price for {0} to {1} in the shop."
                },
                {
                    "invalid_shop_command",
                    "You entered an invalid shop command."
                }
            };

        protected override void Load()
        {
            Instance = this;
            ShopDB = new DatabaseMgr();
        }

        protected override void Unload()
        {
            ShopDB = null;
            Instance = null;
        }

        public bool Buy(UnturnedPlayer playerid, string[] components0)
        {
            string message;
            if (components0.Length == 0)
            {
                message = Instance.Translate("buy_command_usage");
                // We are going to print how to use
                UnturnedChat.Say(playerid, message);
                return false;
            }

            byte amttobuy = 1;
            if (components0.Length > 1)
                if (!byte.TryParse(components0[1], out amttobuy))
                {
                    message = Instance.Translate("invalid_amt");
                    UnturnedChat.Say(playerid, message);
                    return false;
                }

            var components = Parser.getComponentsFromSerial(components0[0], '.');
            if (components.Length == 2 && components[0].Trim() != "v" ||
                components.Length == 1 && components[0].Trim() == "v" || components.Length > 2 ||
                components0[0].Trim() == string.Empty)
            {
                message = Instance.Translate("buy_command_usage");
                // We are going to print how to use
                UnturnedChat.Say(playerid, message);
                return false;
            }

            ushort id;
            switch (components[0])
            {
                case "v":
                    if (!Instance.Configuration.Instance.CanBuyVehicles)
                    {
                        message = Instance.Translate("buy_vehicles_off");
                        UnturnedChat.Say(playerid, message);
                        return false;
                    }

                    string name = null;
                    if (!ushort.TryParse(components[1], out id))
                    {
                        var array = Assets.find(EAssetType.VEHICLE);

                        var vAsset = array.Cast<VehicleAsset>()
                            .FirstOrDefault(k => k?.vehicleName?.ToLower().Contains(components[1].ToLower()) == true);

                        if (vAsset == null)
                        {
                            message = Instance.Translate("could_not_find", components[1]);
                            UnturnedChat.Say(playerid, message);
                            return false;
                        }

                        id = vAsset.id;
                        name = vAsset.vehicleName;
                    }

                    if (Assets.find(EAssetType.VEHICLE, id) == null)
                    {
                        message = Instance.Translate("could_not_find", components[1]);
                        UnturnedChat.Say(playerid, message);
                        return false;
                    }
                    else if (name == null && id != 0)
                    {
                        name = ((VehicleAsset) Assets.find(EAssetType.VEHICLE, id)).vehicleName;
                    }

                    var cost = Instance.ShopDB.GetVehicleCost(id);
                    var balance = Uconomy.Instance.Database.GetBalance(playerid.CSteamID.ToString());
                    if (cost <= 0m)
                    {
                        message = Instance.Translate("vehicle_not_available", name);
                        UnturnedChat.Say(playerid, message);
                        return false;
                    }

                    if (balance < cost)
                    {
                        message = Instance.Translate("not_enough_currency_msg",
                            Uconomy.Instance.Configuration.Instance.MoneyName, "1", name);
                        UnturnedChat.Say(playerid, message);
                        return false;
                    }

                    if (!playerid.GiveVehicle(id))
                    {
                        message = Instance.Translate("error_giving_item", name);
                        UnturnedChat.Say(playerid, message);
                        return false;
                    }

                    var newbal = Uconomy.Instance.Database.IncreaseBalance(playerid.CSteamID.ToString(), cost * -1);
                    message = Instance.Translate("vehicle_buy_msg", name, cost,
                        Uconomy.Instance.Configuration.Instance.MoneyName, newbal,
                        Uconomy.Instance.Configuration.Instance.MoneyName);
                    Instance.OnShopBuy?.Invoke(playerid, cost, 1, id, "vehicle");
                    playerid.Player.gameObject.SendMessage("ZaupShopOnBuy",
                        new object[] {playerid, cost, amttobuy, id, "vehicle"}, SendMessageOptions.DontRequireReceiver);
                    UnturnedChat.Say(playerid, message);
                    return true;
                default:
                    if (!Instance.Configuration.Instance.CanBuyItems)
                    {
                        message = Instance.Translate("buy_items_off");
                        UnturnedChat.Say(playerid, message);
                        return false;
                    }

                    name = null;
                    if (!ushort.TryParse(components[0], out id))
                    {
                        var array = Assets.find(EAssetType.ITEM);
                        var iAsset = array.Cast<ItemAsset>().FirstOrDefault(k =>
                            k?.itemName?.ToLower().Contains(components[0].ToLower()) == true);

                        if (iAsset == null)
                        {
                            message = Instance.Translate("could_not_find", components[0]);
                            UnturnedChat.Say(playerid, message);
                            return false;
                        }

                        id = iAsset.id;
                        name = iAsset.itemName;
                    }

                    if (Assets.find(EAssetType.ITEM, id) == null)
                    {
                        message = Instance.Translate("could_not_find", components[0]);
                        UnturnedChat.Say(playerid, message);
                        return false;
                    }
                    else if (name == null && id != 0)
                    {
                        name = ((ItemAsset) Assets.find(EAssetType.ITEM, id)).itemName;
                    }

                    cost = decimal.Round(Instance.ShopDB.GetItemCost(id) * amttobuy, 2);
                    balance = Uconomy.Instance.Database.GetBalance(playerid.CSteamID.ToString());
                    if (cost <= 0m)
                    {
                        message = Instance.Translate("item_not_available", name);
                        UnturnedChat.Say(playerid, message);
                        return false;
                    }

                    if (balance < cost)
                    {
                        message = Instance.Translate("not_enough_currency_msg",
                            Uconomy.Instance.Configuration.Instance.MoneyName, amttobuy, name);
                        UnturnedChat.Say(playerid, message);
                        return false;
                    }

                    playerid.GiveItem(id, amttobuy);
                    newbal = Uconomy.Instance.Database.IncreaseBalance(playerid.CSteamID.ToString(), cost * -1);
                    message = Instance.Translate("item_buy_msg", name, cost,
                        Uconomy.Instance.Configuration.Instance.MoneyName, newbal,
                        Uconomy.Instance.Configuration.Instance.MoneyName, amttobuy);
                    Instance.OnShopBuy?.Invoke(playerid, cost, amttobuy, id);
                    playerid.Player.gameObject.SendMessage("ZaupShopOnBuy",
                        new object[] {playerid, cost, amttobuy, id, "item"}, SendMessageOptions.DontRequireReceiver);
                    UnturnedChat.Say(playerid, message);
                    return true;
            }
        }

        public void Cost(UnturnedPlayer playerid, string[] components)
        {
            string message;
            if (components.Length == 0 || components.Length == 1 &&
                (components[0].Trim() == string.Empty || components[0].Trim() == "v"))
            {
                message = Instance.Translate("cost_command_usage");
                // We are going to print how to use
                UnturnedChat.Say(playerid, message);
                return;
            }

            if (components.Length == 2 && (components[0] != "v" || components[1].Trim() == string.Empty))
            {
                message = Instance.Translate("cost_command_usage");
                // We are going to print how to use
                UnturnedChat.Say(playerid, message);
                return;
            }

            ushort id;
            switch (components[0])
            {
                case "v":
                    string name = null;
                    if (!ushort.TryParse(components[1], out id))
                    {
                        var array = Assets.find(EAssetType.VEHICLE);

                        var vAsset = array.Cast<VehicleAsset>()
                            .FirstOrDefault(k => k?.vehicleName?.ToLower().Contains(components[1].ToLower()) == true);

                        if (vAsset == null)
                        {
                            message = Instance.Translate("could_not_find", components[1]);
                            UnturnedChat.Say(playerid, message);
                            return;
                        }

                        id = vAsset.id;
                        name = vAsset.vehicleName;
                    }

                    if (Assets.find(EAssetType.VEHICLE, id) == null)
                    {
                        message = Instance.Translate("could_not_find", components[1]);
                        UnturnedChat.Say(playerid, message);
                        return;
                    }
                    else if (name == null && id != 0)
                    {
                        name = ((VehicleAsset) Assets.find(EAssetType.VEHICLE, id)).vehicleName;
                    }

                    var cost = Instance.ShopDB.GetVehicleCost(id);
                    message = Instance.Translate("vehicle_cost_msg", name, cost.ToString(),
                        Uconomy.Instance.Configuration.Instance.MoneyName);
                    if (cost <= 0m) message = Instance.Translate("error_getting_cost", name);
                    UnturnedChat.Say(playerid, message);
                    break;
                default:
                    name = null;
                    if (!ushort.TryParse(components[0], out id))
                    {
                        var array = Assets.find(EAssetType.ITEM);
                        var iAsset = array.Cast<ItemAsset>().FirstOrDefault(k =>
                            k?.itemName?.ToLower().Contains(components[0].ToLower()) == true);

                        if (iAsset == null)
                        {
                            message = Instance.Translate("could_not_find", components[0]);
                            UnturnedChat.Say(playerid, message);
                            return;
                        }

                        id = iAsset.id;
                        name = iAsset.itemName;
                    }

                    if (Assets.find(EAssetType.ITEM, id) == null)
                    {
                        message = Instance.Translate("could_not_find", components[0]);
                        UnturnedChat.Say(playerid, message);
                        return;
                    }
                    else if (name == null && id != 0)
                    {
                        name = ((ItemAsset) Assets.find(EAssetType.ITEM, id)).itemName;
                    }

                    cost = Instance.ShopDB.GetItemCost(id);
                    var bbp = Instance.ShopDB.GetItemBuyPrice(id);
                    message = Instance.Translate("item_cost_msg", name, cost.ToString(),
                        Uconomy.Instance.Configuration.Instance.MoneyName, bbp.ToString(),
                        Uconomy.Instance.Configuration.Instance.MoneyName);
                    if (cost <= 0m) message = Instance.Translate("error_getting_cost", name);
                    UnturnedChat.Say(playerid, message);
                    break;
            }
        }

        public bool Sell(UnturnedPlayer playerid, string[] components)
        {
            string message;
            if (components.Length == 0 || components.Length > 0 && components[0].Trim() == string.Empty)
            {
                message = Instance.Translate("sell_command_usage");
                // We are going to print how to use
                UnturnedChat.Say(playerid, message);
                return false;
            }

            byte amttosell = 1;
            if (components.Length > 1)
                if (!byte.TryParse(components[1], out amttosell))
                {
                    message = Instance.Translate("invalid_amt");
                    UnturnedChat.Say(playerid, message);
                    return false;
                }

            var amt = amttosell;
            if (!Instance.Configuration.Instance.CanSellItems)
            {
                message = Instance.Translate("sell_items_off");
                UnturnedChat.Say(playerid, message);
                return false;
            }

            string name = null;
            if (!ushort.TryParse(components[0], out var id))
            {
                var array = Assets.find(EAssetType.ITEM);
                var iAsset = array.Cast<ItemAsset>().FirstOrDefault(k =>
                    k?.itemName?.ToLower().Contains(components[0].ToLower()) == true);

                if (iAsset == null)
                {
                    message = Instance.Translate("could_not_find", components[0]);
                    UnturnedChat.Say(playerid, message);
                    return false;
                }

                id = iAsset.id;
                name = iAsset.itemName;
            }

            if (id == 0)
            {
                message = Instance.Translate("could_not_find", components[0]);
                UnturnedChat.Say(playerid, message);
                return false;
            }

            var vAsset = (ItemAsset) Assets.find(EAssetType.ITEM, id);

            if (vAsset == null)
            {
                message = Instance.Translate("could_not_find", components[0]);
                UnturnedChat.Say(playerid, message);
                return false;
            }

            if (name == null) name = vAsset.itemName;

            // Get how many they have
            if (playerid.Inventory.has(id) == null)
            {
                message = Instance.Translate("not_have_item_sell", name);
                UnturnedChat.Say(playerid, message);
                return false;
            }

            var list = playerid.Inventory.search(id, true, true);
            if (list.Count == 0 || vAsset.amount == 1 && list.Count < amttosell)
            {
                message = Instance.Translate("not_enough_items_sell", amttosell.ToString(), name);
                UnturnedChat.Say(playerid, message);
                return false;
            }

            if (vAsset.amount > 1)
            {
                var ammomagamt = 0;
                foreach (var ins in list) ammomagamt += ins.jar.item.amount;
                if (ammomagamt < amttosell)
                {
                    message = Instance.Translate("not_enough_ammo_sell", name);
                    UnturnedChat.Say(playerid, message);
                    return false;
                }
            }

            // We got this far, so let's buy back the items and give them money.
            // Get cost per item.  This will be whatever is set for most items, but changes for ammo and magazines.
            var price = Instance.ShopDB.GetItemBuyPrice(id);
            if (price <= 0.00m)
            {
                message = Instance.Translate("no_sell_price_set", name);
                UnturnedChat.Say(playerid, message);
                return false;
            }

            byte quality = 100;
            decimal peritemprice = 0;
            decimal addmoney = 0;
            switch (vAsset.amount)
            {
                case 1:
                    // These are single items, not ammo or magazines
                    while (amttosell > 0)
                    {
                        if (playerid.Player.equipment.checkSelection(list[0].page, list[0].jar.x, list[0].jar.y))
                            playerid.Player.equipment.dequip();
                        if (Instance.Configuration.Instance.QualityCounts)
                            quality = list[0].jar.item.durability;
                        peritemprice = decimal.Round(price * (quality / 100.0m), 2);
                        addmoney += peritemprice;
                        playerid.Inventory.removeItem(list[0].page,
                            playerid.Inventory.getIndex(list[0].page, list[0].jar.x, list[0].jar.y));
                        list.RemoveAt(0);
                        amttosell--;
                    }

                    break;
                default:
                    // This is ammo or magazines
                    var amttosell1 = amttosell;
                    while (amttosell > 0)
                    {
                        if (playerid.Player.equipment.checkSelection(list[0].page, list[0].jar.x, list[0].jar.y))
                            playerid.Player.equipment.dequip();
                        if (list[0].jar.item.amount >= amttosell)
                        {
                            var left = (byte) (list[0].jar.item.amount - amttosell);
                            list[0].jar.item.amount = left;
                            playerid.Inventory.sendUpdateAmount(list[0].page, list[0].jar.x, list[0].jar.y, left);
                            amttosell = 0;
                            if (left == 0)
                            {
                                playerid.Inventory.removeItem(list[0].page,
                                    playerid.Inventory.getIndex(list[0].page, list[0].jar.x, list[0].jar.y));
                                list.RemoveAt(0);
                            }
                        }
                        else
                        {
                            amttosell -= list[0].jar.item.amount;
                            playerid.Inventory.sendUpdateAmount(list[0].page, list[0].jar.x, list[0].jar.y, 0);
                            playerid.Inventory.removeItem(list[0].page,
                                playerid.Inventory.getIndex(list[0].page, list[0].jar.x, list[0].jar.y));
                            list.RemoveAt(0);
                        }
                    }

                    peritemprice = decimal.Round(price * (amttosell1 / (decimal) vAsset.amount), 2);
                    addmoney += peritemprice;
                    break;
            }

            var balance = Uconomy.Instance.Database.IncreaseBalance(playerid.CSteamID.ToString(), addmoney);
            message = Instance.Translate("sold_items", amt, name, addmoney,
                Uconomy.Instance.Configuration.Instance.MoneyName, balance,
                Uconomy.Instance.Configuration.Instance.MoneyName);
            Instance.OnShopSell?.Invoke(playerid, addmoney, amt, id);
            playerid.Player.gameObject.SendMessage("ZaupShopOnSell", new object[] {playerid, addmoney, amt, id},
                SendMessageOptions.DontRequireReceiver);
            UnturnedChat.Say(playerid, message);

            return true;
        }
    }
}