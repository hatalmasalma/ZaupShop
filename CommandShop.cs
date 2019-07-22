using System.Collections.Generic;
using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace ZaupShop
{
    public class CommandShop : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "shop";

        public string Help => "Allows admins to change, add, or remove items/vehicles from the shop.";

        public string Syntax => "<add | rem | chng | buy> [v.]<itemid> <cost>";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string> {"shop.*", "shop.add", "shop.rem", "shop.chng", "shop.buy"};

        public void Execute(IRocketPlayer caller, string[] msg)
        {
            var console = caller is ConsolePlayer;
            string[] permnames = {"shop.*", "shop.add", "shop.rem", "shop.chng", "shop.buy"};
            bool[] perms = {false, false, false, false, false};
            var anyuse = false;
            string message;
            foreach (var s in caller.GetPermissions())
                switch (s.Name)
                {
                    case "shop.*":
                        perms[0] = true;
                        anyuse = true;
                        break;
                    case "shop.add":
                        perms[1] = true;
                        anyuse = true;
                        break;
                    case "shop.rem":
                        perms[2] = true;
                        anyuse = true;
                        break;
                    case "shop.chng":
                        perms[3] = true;
                        anyuse = true;
                        break;
                    case "shop.buy":
                        perms[4] = true;
                        anyuse = true;
                        break;
                    case "*":
                        perms[0] = true;
                        perms[1] = true;
                        perms[2] = true;
                        perms[3] = true;
                        perms[4] = true;
                        anyuse = true;
                        break;
                }

            if (console || ((UnturnedPlayer) caller).IsAdmin)
            {
                perms[0] = true;
                perms[1] = true;
                perms[2] = true;
                perms[3] = true;
                perms[4] = true;
                anyuse = true;
            }

            if (!anyuse)
            {
                // Assume this is a player
                UnturnedChat.Say(caller, "You don't have permission to use the /shop command.");
                return;
            }

            if (msg.Length == 0)
            {
                message = ZaupShop.Instance.Translate("shop_command_usage");
                // We are going to print how to use
                SendMessage(caller, message, console);
                return;
            }

            if (msg.Length < 2)
            {
                message = ZaupShop.Instance.Translate("no_itemid_given");
                SendMessage(caller, message, console);
                return;
            }

            if (msg.Length == 2 && msg[0] != "rem")
            {
                message = ZaupShop.Instance.Translate("no_cost_given");
                SendMessage(caller, message, console);
            }
            else if (msg.Length >= 2)
            {
                var type = Parser.getComponentsFromSerial(msg[1], '.');
                if (type.Length > 1 && type[0] != "v")
                {
                    message = ZaupShop.Instance.Translate("v_not_provided");
                    SendMessage(caller, message, console);
                    return;
                }

                ushort id;
                if (type.Length > 1)
                {
                    if (!ushort.TryParse(type[1], out id))
                    {
                        message = ZaupShop.Instance.Translate("invalid_id_given");
                        SendMessage(caller, message, console);
                        return;
                    }
                }
                else
                {
                    if (!ushort.TryParse(type[0], out id))
                    {
                        message = ZaupShop.Instance.Translate("invalid_id_given");
                        SendMessage(caller, message, console);
                        return;
                    }
                }

                // All basic checks complete.  Let's get down to business.
                var success = false;
                var change = false;
                var pass = false;
                switch (msg[0])
                {
                    case "chng":
                        if (!perms[3] && !perms[0])
                        {
                            message = ZaupShop.Instance.Translate("no_permission_shop_chng");
                            SendMessage(caller, message, console);
                            return;
                        }

                        change = true;
                        pass = true;
                        goto case "add";
                    case "add":
                        if (!pass)
                            if (!perms[1] && !perms[0])
                            {
                                message = ZaupShop.Instance.Translate("no_permission_shop_add");
                                SendMessage(caller, message, console);
                                return;
                            }

                        var ac = pass
                            ? ZaupShop.Instance.Translate("changed")
                            : ZaupShop.Instance.Translate("added");
                        switch (type[0])
                        {
                            case "v":
                                if (!IsAsset(id, "v"))
                                {
                                    message = ZaupShop.Instance.Translate("invalid_id_given");
                                    SendMessage(caller, message, console);
                                    return;
                                }

                                var va = (VehicleAsset) Assets.find(EAssetType.VEHICLE, id);
                                message = ZaupShop.Instance.Translate("changed_or_added_to_shop", ac, va.vehicleName,
                                    msg[2]);
                                success = ZaupShop.Instance.ShopDB.AddVehicle(id, va.vehicleName,
                                    decimal.Parse(msg[2]), change);
                                if (!success)
                                    message = ZaupShop.Instance.Translate("error_adding_or_changing", va.vehicleName);
                                SendMessage(caller, message, console);
                                break;
                            default:
                                if (!IsAsset(id, "i"))
                                {
                                    message = ZaupShop.Instance.Translate("invalid_id_given");
                                    SendMessage(caller, message, console);
                                    return;
                                }

                                var ia = (ItemAsset) Assets.find(EAssetType.ITEM, id);
                                message = ZaupShop.Instance.Translate("changed_or_added_to_shop", ac, ia.itemName,
                                    msg[2]);
                                success = ZaupShop.Instance.ShopDB.AddItem(id, ia.itemName, decimal.Parse(msg[2]),
                                    change);
                                if (!success)
                                    message = ZaupShop.Instance.Translate("error_adding_or_changing", ia.itemName);
                                SendMessage(caller, message, console);
                                break;
                        }

                        break;
                    case "rem":
                        if (!perms[2] && !perms[0])
                        {
                            message = ZaupShop.Instance.Translate("no_permission_shop_rem");
                            SendMessage(caller, message, console);
                            return;
                        }

                        switch (type[0])
                        {
                            case "v":
                                if (!IsAsset(id, "v"))
                                {
                                    message = ZaupShop.Instance.Translate("invalid_id_given");
                                    SendMessage(caller, message, console);
                                    return;
                                }

                                var va = (VehicleAsset) Assets.find(EAssetType.VEHICLE, id);
                                message = ZaupShop.Instance.Translate("removed_from_shop", va.vehicleName);
                                success = ZaupShop.Instance.ShopDB.DeleteVehicle(id);
                                if (!success)
                                    message = ZaupShop.Instance.Translate("not_in_shop_to_remove", va.vehicleName);
                                SendMessage(caller, message, console);
                                break;
                            default:
                                if (!IsAsset(id, "i"))
                                {
                                    message = ZaupShop.Instance.Translate("invalid_id_given");
                                    SendMessage(caller, message, console);
                                    return;
                                }

                                var ia = (ItemAsset) Assets.find(EAssetType.ITEM, id);
                                message = ZaupShop.Instance.Translate("removed_from_shop", ia.itemName);
                                success = ZaupShop.Instance.ShopDB.DeleteItem(id);
                                if (!success)
                                    message = ZaupShop.Instance.Translate("not_in_shop_to_remove", ia.itemName);
                                SendMessage(caller, message, console);
                                break;
                        }

                        break;
                    case "buy":
                        if (!perms[4] && !perms[0])
                        {
                            message = ZaupShop.Instance.Translate("no_permission_shop_buy");
                            SendMessage(caller, message, console);
                            return;
                        }

                        if (!IsAsset(id, "i"))
                        {
                            message = ZaupShop.Instance.Translate("invalid_id_given");
                            SendMessage(caller, message, console);
                            return;
                        }

                        var iab = (ItemAsset) Assets.find(EAssetType.ITEM, id);
                        decimal.TryParse(msg[2], out var buyb);
                        message = ZaupShop.Instance.Translate("set_buyback_price", iab.itemName, buyb.ToString());
                        success = ZaupShop.Instance.ShopDB.SetBuyPrice(id, buyb);
                        if (!success)
                            message = ZaupShop.Instance.Translate("not_in_shop_to_buyback", iab.itemName);
                        SendMessage(caller, message, console);
                        break;
                    default:
                        // We shouldn't get this, but if we do send an error.
                        message = ZaupShop.Instance.Translate("not_in_shop_to_remove");

                        SendMessage(caller, message, console);
                        return;
                }
            }
        }

        private bool IsAsset(ushort id, string type)
        {
            // Check for valid Item/Vehicle Id.
            switch (type)
            {
                case "i":
                    return Assets.find(EAssetType.ITEM, id) != null;
                case "v":
                    return Assets.find(EAssetType.VEHICLE, id) != null;
                default:
                    return false;
            }
        }

        private void SendMessage(IRocketPlayer caller, string message, bool console)
        {
            if (console)
                Logger.Log(message);
            else
                UnturnedChat.Say(caller, message);
        }
    }
}