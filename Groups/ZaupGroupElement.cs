namespace ZaupShop.Groups
{
    public class ZaupGroupElement
    {
        public readonly ushort ID;
        public readonly bool Vehicle;

        public ZaupGroupElement(ushort id, bool vehicle)
        {
            ID = id;
            Vehicle = vehicle;
        }
    }
}