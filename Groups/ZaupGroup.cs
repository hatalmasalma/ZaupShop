using System.Collections.Generic;

namespace ZaupShop.Groups
{
    public class ZaupGroup
    {
        public readonly string Name;
        public readonly bool Whitelist;
        public HashSet<ZaupGroupElement> Elements;

        public ZaupGroup(string name, bool whitelist, HashSet<ZaupGroupElement> elements = null)
        {
            Name = name;
            Whitelist = whitelist;
            Elements = elements ?? new HashSet<ZaupGroupElement>();
        }
    }
}