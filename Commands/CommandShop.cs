using System.Collections.Generic;
using System.Diagnostics;
using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace ZaupShop.Commands
{
    public class CommandShop : IRocketCommand
    {
        #region Boilerplate

        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "shop";
        public string Help => "Allows admins to change, add, or remove items/vehicles from the shop.";
        public string Syntax => "<add | rem | chng | buy> [v.]<itemid> <cost>";
        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>
            {"shop.*", "shop.add", "shop.rem", "shop.chng", "shop.buy", "shop.group"};

        #endregion

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0)
            {
                SendMessage(caller, "shop_command_usage");
                return;
            }

            if (command[0] == "group")
            {
                ProcessGroupCommand(caller, command);
                return;
            }

            if (command.Length < 2)
            {
                SendMessage(caller, "no_itemid_given");
                return;
            }

            if (command.Length == 2 && command[0] != "rem")
            {
                SendMessage(caller, "no_cost_given");
                return;
            }

            string[] type = Parser.getComponentsFromSerial(command[1], '.');
            if (type.Length > 1 && type[0] != "v")
            {
                SendMessage(caller, "v_not_provided");
                return;
            }

            string unprocessedID = type.Length > 1 ? type[1] : type[0];

            if (!ushort.TryParse(unprocessedID, out ushort id))
            {
                SendMessage(caller, "invalid_id_given");
                return;
            }

            // All basic checks complete.  Let's get down to business.
            var change = false;
            switch (command[0])
            {
                case "chng":
                    if (!caller.HasPermission("shop.chng"))
                    {
                        SendMessage(caller, "no_permission_shop_chng");
                        return;
                    }

                    change = true;
                    goto case "add";
                case "add":
                    if (!change)
                    {
                        if (!caller.HasPermission("shop.add"))
                        {
                            SendMessage(caller, "no_permission_shop_add");
                            return;
                        }
                    }

                    decimal price = decimal.Parse(command[2]);
                    if (type[0] == "v")
                    {
                        AddEntry(caller, id, price, change, true);
                        return;
                    }
                    else
                    {
                        AddEntry(caller, id, price, change, false);
                        return;
                    }
                case "rem":
                    if (!caller.HasPermission("shop.rem"))
                    {
                        SendMessage(caller, "no_permission_shop_rem");
                        return;
                    }

                    if (type[0] == "v")
                    {
                        DeleteEntry(caller, id, true);
                        return;
                    }
                    else
                    {
                        DeleteEntry(caller, id, false);
                        return;
                    }
                case "buy":
                    if (!caller.HasPermission("shop.buy"))
                    {
                        SendMessage(caller, "no_permission_shop_buy");
                        return;
                    }

                    Asset potentialItem = AssetUtils.GetAssetByID(id, false);
                    if (potentialItem == null)
                    {
                        SendMessage(caller, "invalid_id_given");
                        return;
                    }

                    ItemAsset itemAsset = (ItemAsset) potentialItem;
                    if (!decimal.TryParse(command[2], out decimal buybackPrice))
                    {
                        SendMessage(caller, "shop_command_usage");
                        return;
                    }

                    if (!ZaupShop.Instance.ShopDB.SetBuyPrice(id, buybackPrice))
                    {
                        SendMessage(caller, "not_in_shop_to_buyback", itemAsset.itemName);
                        return;
                    }

                    SendMessage(caller, "set_buyback_price", itemAsset.itemName, buybackPrice);
                    return;
                default:
                    // We shouldn't get this, but if we do send an error.
                    SendMessage(caller, "shop_command_usage");
                    return;
            }
        }

        private void AddEntry(IRocketPlayer caller, ushort id, decimal price, bool change, bool vehicle)
        {
            string ac = change
                ? ZaupShop.Instance.Translate("changed")
                : ZaupShop.Instance.Translate("added");

            if (vehicle)
            {
                Asset potentialVehicle = AssetUtils.GetAssetByID(id, true);
                if (potentialVehicle == null)
                {
                    SendMessage(caller, "invalid_id_given");
                    return;
                }

                VehicleAsset vehicleAsset = (VehicleAsset) potentialVehicle;

                if (!ZaupShop.Instance.ShopDB.AddVehicle(id, vehicleAsset.vehicleName, price, change))
                {
                    SendMessage(caller, "error_adding_or_changing", vehicleAsset.vehicleName);
                    return;
                }

                SendMessage(caller, "changed_or_added_to_shop", ac, vehicleAsset.vehicleName, price);
            }
            else
            {
                Asset potentialItem = AssetUtils.GetAssetByID(id, false);
                if (potentialItem == null)
                {
                    SendMessage(caller, "invalid_id_given");
                    return;
                }

                ItemAsset itemAsset = (ItemAsset) potentialItem;

                if (!ZaupShop.Instance.ShopDB.AddItem(id, itemAsset.itemName, price, change))
                {
                    SendMessage(caller, "error_adding_or_changing", itemAsset.itemName);
                    return;
                }

                SendMessage(caller, "changed_or_added_to_shop", ac, itemAsset.itemName, price);
            }
        }

        private void DeleteEntry(IRocketPlayer caller, ushort id, bool vehicle)
        {
            if (vehicle)
            {
                Asset potentialVehicle = AssetUtils.GetAssetByID(id, true);
                if (potentialVehicle == null)
                {
                    SendMessage(caller, "invalid_id_given");
                    return;
                }

                VehicleAsset vehicelAsset = (VehicleAsset) potentialVehicle;

                if (!ZaupShop.Instance.ShopDB.DeleteVehicle(id))
                {
                    SendMessage(caller, "not_in_shop_to_remove", vehicelAsset.vehicleName);
                    return;
                }

                SendMessage(caller, "removed_from_shop", vehicelAsset.vehicleName);
            }
            else
            {
                Asset potentialItem = AssetUtils.GetAssetByID(id, false);
                if (potentialItem == null)
                {
                    SendMessage(caller, "invalid_id_given");
                    return;
                }

                ItemAsset itemAsset = (ItemAsset) potentialItem;

                if (!ZaupShop.Instance.ShopDB.DeleteItem(id))
                {
                    SendMessage(caller, "not_in_shop_to_remove", itemAsset.itemName);
                    return;
                }

                SendMessage(caller, "removed_from_shop", itemAsset.itemName);
            }
        }

        private void SendMessage(IRocketPlayer recipient, string translationKey, params object[] translationParameters)
        {
            if (recipient is ConsolePlayer)
                ZaupShop.Instance.TellConsole(translationKey, translationParameters);
            else
            {
                SteamPlayer playerRecipient = PlayerTool.getSteamPlayer(((UnturnedPlayer) recipient).CSteamID);
                ZaupShop.Instance.TellPlayer(playerRecipient, translationKey, translationParameters);
            }
        }

        private void ProcessGroupCommand(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 1 || command.Length > 4)
            {
                SendMessage(caller, "shop_group_usage");
                return;
            }
            
            switch (command[1])
            {
                case "create":
                    if (command.Length != 4)
                    {
                        SendMessage(caller, "shop_group_create_usage");
                        return;
                    }

                    string groupName = command[2];
                    string groupType = command[3];

                    switch (groupType)
                    {
                        case "wlist":
                            if (!ZaupShop.Instance.ShopDB.AddGroup(groupName, true))
                            {
                                SendMessage(caller, "shop_group_create_failed", groupName);
                                return;
                            }

                            SendMessage(caller, "shop_group_created", groupName, groupType);
                            return;
                        case "blist":
                            if (!ZaupShop.Instance.ShopDB.AddGroup(groupName, false))
                            {
                                SendMessage(caller, "shop_group_create_failed", groupName);
                                return;
                            }

                            SendMessage(caller, "shop_group_created", groupName, groupType);
                            return;
                        default:
                            SendMessage(caller, "shop_group_create_usage");
                            return;
                    }
                case "delgroup":
                    if (command.Length != 3)
                    {
                        SendMessage(caller, "shop_group_del_usage");
                        return;
                    }

                    groupName = command[2];



                    break;
                case "add":
                    if (command.Length != 4)
                    {
                        SendMessage(caller, "shop_group_change_usage");
                        return;
                    }

                    groupName = command[2];
                    break;
                case "rem":
                    if (command.Length != 4)
                    {
                        SendMessage(caller, "shop_group_change_usage");
                        return;
                    }

                    groupName = command[2];
                    break;
            }
        }
    }
}