using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Item
{
    public class Inventory
    {
        Dictionary<int, Item> _items = new Dictionary<int, Item>();

        public void Add(Item item)
        {
            _items.Add(item.ItemDbId, item);
        }

        public Item get(int itemDbId)
        {
            Item item = null;
            _items.TryGetValue(itemDbId, out item);
            return item;
        }

        public Item Find(Func<Item, bool> condition)
        {
            foreach (Item item in _items.Values)
            {
                if(condition.Invoke(item))
                {
                    return item;
                }
            }
            return null;
        }

        // 빈 슬롯을 반환
        // 없으면 null
        public int? GetEmptySlot()
        {
            for (int slot = 0; slot < 20; slot++)
            {
                Item item = _items.Values.FirstOrDefault(i => i.Info.Slot == slot);

                // 만약 비어있다면
                if (item == null)
                    return slot;
            }

            return null;
        }
    }
}
