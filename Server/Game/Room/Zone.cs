using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game
{
    public class Zone
    {
        public int idxX { get; private set; }   
        public int idxY { get; private set; }
        
        // 이 존 안에 있는 플레이어들을 가져온다.
        public HashSet<Player> Players { get; set; } = new HashSet<Player>();
        public HashSet<Monster> Monsters { get; set; } = new HashSet<Monster>();
        public HashSet<Projectile> Projectiles { get; set; } = new HashSet<Projectile>();


        public Zone(int y, int x)
        {
            idxY = y;
            idxX = x;   
        }

        // 여러 명의 플레이어를 찾는다.
        public List<Player> FindAll(Func<Player, bool> condition)
        {
            List<Player> findList = new List<Player>();
            foreach(Player p in Players)
            {
                if(condition.Invoke(p))
                    findList.Add(p);

            }
            return findList;
        }

        // 한 명의 플레이어만 찾는다.
        public Player FindOne(Func<Player, bool> condition)
        {
            foreach(Player p in Players)
            {
                if (condition.Invoke(p))
                    return p;
            }
            return null;
        }
    }
}
