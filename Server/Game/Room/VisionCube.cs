using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Room
{
    public class VisionCube
    {
        public Player Owner { get; private set; }
        public HashSet<GameObject> PreviousObject { get; private set; } = new HashSet<GameObject>();

        public VisionCube(Player owner)
        {
            Owner = owner;
        }

        public HashSet<GameObject> GatherObjects()
        {
            if (Owner == null || Owner.Room == null)
                return null;

            HashSet<GameObject> objects = new HashSet<GameObject>();

            Vector2Int cellPos = Owner.CellPos;
            List<Zone> zones = Owner.Room.GetAbjacentZones(cellPos);

            foreach (Zone zone in zones)
            {
                foreach (Player p in zone.Players)
                {
                    int dx = p.CellPos.x - cellPos.x;
                    int dy = p.CellPos.y - cellPos.y;

                    // 눈 밖이면 체크할 필요 없다.
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;
                    objects.Add(p);
                }

                foreach (Monster m in zone.Monsters)
                {
                    int dx = m.CellPos.x - cellPos.x;
                    int dy = m.CellPos.y - cellPos.y;

                    // 눈 밖이면 체크할 필요 없다.
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;
                    objects.Add(m);
                }

                foreach (Projectile p in zone.Projectiles)
                {
                    int dx = p.CellPos.x - cellPos.x;
                    int dy = p.CellPos.y - cellPos.y;

                    // 눈 밖이면 체크할 필요 없다.
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;
                    objects.Add(p);
                }
            }

            return objects;
        }

        public void Update()
        {
            if (Owner == null || Owner.Room == null)
                return;

            HashSet<GameObject> currentObject = GatherObjects();

            // 기존에 없었는데 생긴
            List<GameObject> added = currentObject.Except(PreviousObject).ToList();

            // 만약 있다면
            if(added.Count > 0)
            {
                S_Spawn spawnPacket = new S_Spawn();

                foreach(GameObject obj in added)
                {
                    ObjectInfo objectInfo = new ObjectInfo();
                    objectInfo.MergeFrom(obj.Info);
                    spawnPacket.Objects.Add(objectInfo);
                }

                Owner.Session.Send(spawnPacket);
            }

            // 기존에 있었는데 없어진
            List<GameObject> subtracted = PreviousObject.Except(currentObject).ToList();
            if (subtracted.Count > 0)
            {
                S_Despawn despawnPacket = new S_Despawn();

                foreach (GameObject obj in subtracted)
                {
                    despawnPacket.ObjectIds.Add(obj.Id);
                }

                Owner.Session.Send(despawnPacket);
            }

            PreviousObject = currentObject;

            Owner.Room.PushAfter(500, Update);
        }
    }
}
