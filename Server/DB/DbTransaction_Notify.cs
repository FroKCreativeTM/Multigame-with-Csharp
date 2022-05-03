using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Game;
using Server.Game.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DB
{
    // 단순히 DB에 통보하는 Notify 종류 행동을 하기 위한 클래스
    public partial class DbTransaction : JobSerializer
    {
        public static void EquipItemNotify(Player player, Item item)
        {
            if (player == null || item == null)
                return;

            ItemDb itemDb = new ItemDb()
            {
                ItemDbId = item.ItemDbId,
                Equipped = item.Equipped
            };


            // DB에겐 행동 일감을 job단위로 보내주자.
            Instance.Push(() =>
            {
                using (AppDBContext db = new AppDBContext())
                {
                    // 일단 아이템 정보를 조회해온다.
                    db.Entry(itemDb).State = EntityState.Unchanged;
                    db.Entry(itemDb).Property(nameof(itemDb.Equipped)).IsModified = true;

                    bool success = db.SaveChangesEx();

                    // 만약 실행된 경우
                    if (success)
                    {
                        // 실패했으면 Kick!
                        
                    }
                }
            });
        }
    }
}
