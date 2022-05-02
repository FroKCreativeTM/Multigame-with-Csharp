using Microsoft.EntityFrameworkCore;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DB
{
    // DB 코드들은 코드의 흐름을 막아버린다!
    // - 비동기가 ?
    // - 스레드에게?
    // -- 문제는 DB 작업은 결과를 받아서 이어서 처리해야되는 경우도 많음
    // 이런 문제를 해결하자!
    public class DbTransaction : JobSerializer
    {
        public static DbTransaction Instance { get; } = new DbTransaction();

        // 실행하는 주체 Me GameRoom -> You(DB) -> Me (GameRoom)
        public static void SavePlayerStates_AllInOne(Player player, GameRoom room)
        {
            if (player == null || room == null) return;

            // GameRoom 입장에서 보면
            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDBId = player.PlayerDbId;
            playerDb.Hp = player.Stat.Hp;

            // DB에겐 행동 일감을 job단위로 보내주자.
            Instance.Push(() =>
            {
                using (AppDBContext db = new AppDBContext())
                {
                    db.Entry(playerDb).State = EntityState.Unchanged;
                    db.Entry(playerDb).Property(nameof(playerDb.Hp)).IsModified = true;
                    bool success = db.SaveChangesEx();

                    // 만약 실행된 경우
                    if(success)
                    {
                        room.Push(() => { Console.WriteLine($"Hp Saved({playerDb.Hp})"); } );
                    }
                }
            });            
        }
    }
}
