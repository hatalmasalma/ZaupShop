using Rocket.API;

namespace ZaupShop
{
    public class ZaupShopConfiguration : IRocketPluginConfiguration
    {
        public string ItemShopTableName;
        public string VehicleShopTableName;
        public string GroupListTableName;
        public bool CanBuyItems;
        public bool CanBuyVehicles;
        public bool CanSellItems;
        public bool QualityCounts;

        public void LoadDefaults()
        {
            ItemShopTableName = "uconomyitemshop";
            VehicleShopTableName = "uconomyvehicleshop";
            GroupListTableName = "zaupshopgroups";
            CanBuyItems = true;
            CanBuyVehicles = false;
            CanSellItems = true;
            QualityCounts = true;
        }
    }
}