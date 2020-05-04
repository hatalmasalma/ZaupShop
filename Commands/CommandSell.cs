using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;
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
        
        public void Execute(IRocketPlayer player, string[] msg)
        {
            ZaupShop.Instance.Sell((UnturnedPlayer)player, msg);
        }
    }
}