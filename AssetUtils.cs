using System.Linq;
using SDG.Unturned;

namespace ZaupShop
{
    public class AssetUtils
    {
        public static VehicleAsset GetVehicle(string searchTerm)
        {
            VehicleAsset vehicleAsset = null;
            if (!ushort.TryParse(searchTerm, out ushort vehicleID))
            {
                vehicleAsset = Assets.find(EAssetType.VEHICLE).Cast<VehicleAsset>()
                    .Where(veh => !string.IsNullOrEmpty(veh.vehicleName))
                    .FirstOrDefault(veh =>
                        veh.vehicleName.ToUpperInvariant().Contains(searchTerm.ToUpperInvariant()));


                if (vehicleAsset == null)
                    return null;
            }

            if (vehicleAsset != null)
                return vehicleAsset;
            
            Asset potentialMatch = Assets.find(EAssetType.VEHICLE, vehicleID);

            if (potentialMatch == null)
                return null;

            vehicleAsset = (VehicleAsset) potentialMatch;

            return vehicleAsset;
        }

        public static ItemAsset GetItem(string searchTerm)
        {
            ItemAsset itemAsset = null;
            
            if (!ushort.TryParse(searchTerm, out ushort itemID))
            {
                itemAsset = Assets.find(EAssetType.ITEM).Cast<ItemAsset>()
                    .Where(i => !string.IsNullOrEmpty(i.itemName)).OrderBy(i => i.itemName.Length)
                    .FirstOrDefault(i => i.itemName.ToUpperInvariant().Contains(searchTerm.ToUpperInvariant()));

                if (itemAsset == null)
                    return null;
            }

            if (itemAsset != null)
                return itemAsset;
            
            Asset potentialMatch = Assets.find(EAssetType.ITEM, itemID);

            if (potentialMatch == null)
                return null;

            itemAsset = (ItemAsset) potentialMatch;

            return itemAsset;
        }
        
        public static Asset GetAssetByID(ushort id, bool vehicle) => Assets.find(vehicle ? EAssetType.VEHICLE : EAssetType.ITEM, id);
    }
}