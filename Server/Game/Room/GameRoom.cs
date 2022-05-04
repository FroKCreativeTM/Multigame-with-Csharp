using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Game
{
	public partial class GameRoom : JobSerializer
	{
		public const int VisionCells = 5;
		public int RoomId { get; set; }

		Dictionary<int, Player> _players = new Dictionary<int, Player>();
		Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
		Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();

		// GameRoom을 나누는 단위
		public Zone[,] Zones { get; private set; }
		public int ZoneCells { get; private set; }

		public Map Map { get; private set; } = new Map();

		public void Init(int mapId, int zoneCells)
		{
			Map.LoadMap(mapId);

			// Zone
			ZoneCells = zoneCells;

			// 8 * 8
			// 1~10 = 1존
			// 11~20 = 2존
			// 21~30 = 3존
			int cntY = (Map.SizeY + ZoneCells - 1) / ZoneCells;
			int cntX = (Map.SizeX + ZoneCells - 1) / ZoneCells;
			Zones = new Zone[cntY, cntX];

            for (int y = 0; y < cntY; y++)
            {
                for (int x = 0; x < cntX; x++)
                {
					Zones[y, x] = new Zone(y, x);
                }
            }

			// TEMP
			Monster monster = ObjectManager.Instance.Add<Monster>();
			monster.Init(1);
			monster.CellPos = new Vector2Int(5, 5);
			EnterGame(monster);
		}

		// 현재 위치가 어떤 존에 있는지 반환한다.
		public Zone GetZone(Vector2Int pos)
        {
			int x = (pos.x - Map.MinX) / ZoneCells;
			int y = (Map.MinY - pos.y) / ZoneCells;

			// 존 밖에 있는 경우
			// 즉 말이 안 된다!
			if (x < 0 || x >= Zones.GetLength(1))
				return null;
			if (y < 0 || y >= Zones.GetLength(0))
				return null;

			return Zones[y, x];
        }

		// 누군가 주기적으로 호출해줘야 한다
		public void Update()
		{
			foreach (Monster monster in _monsters.Values)
			{
				monster.Update();
			}

			Flush();
		}

		public void EnterGame(GameObject gameObject)
		{
			if (gameObject == null)
				return;

			GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

			if (type == GameObjectType.Player)
			{
				Player player = gameObject as Player;
				_players.Add(gameObject.Id, player);
				player.Room = this;

				player.RefreshAdditionalStat();

				Map.ApplyMove(player, new Vector2Int(player.CellPos.x, player.CellPos.y));

				// 플레이어가 있는 존에 넣어준다.
				GetZone(player.CellPos).Players.Add(player);

				// 본인한테 정보 전송
				{
					S_EnterGame enterPacket = new S_EnterGame();
					enterPacket.Player = player.Info;
					player.Session.Send(enterPacket);

					// 내 시야각을 주기적으로 체크하게 해준다.
					player.Vision.Update();
				}
			}
			else if (type == GameObjectType.Monster)
			{
				Monster monster = gameObject as Monster;
				_monsters.Add(gameObject.Id, monster);
				monster.Room = this;

				Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));
				GetZone(monster.CellPos).Monsters.Add(monster);
			}
			else if (type == GameObjectType.Projectile)
			{
				Projectile projectile = gameObject as Projectile;
				_projectiles.Add(gameObject.Id, projectile);
				projectile.Room = this;

				GetZone(projectile.CellPos).Projectiles.Add(projectile);

				Push(projectile.Update);
			}
		}

        public void LeaveGame(int objectId)
		{
			GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

			if (type == GameObjectType.Player)
			{
				Player player = null;
				if (_players.Remove(objectId, out player) == false)
					return;

				GetZone(player.CellPos).Players.Remove(player);
				
				player.OnLeaveGame();
				Map.ApplyLeave(player);

				player.Room = null;

				// 본인한테 정보 전송
				{
					S_LeaveGame leavePacket = new S_LeaveGame();
					player.Session.Send(leavePacket);
				}
			}
			else if (type == GameObjectType.Monster)
			{
				Monster monster = null;
				if (_monsters.Remove(objectId, out monster) == false)
					return;
				
				GetZone(monster.CellPos).Monsters.Remove(monster);

				Map.ApplyLeave(monster);
				monster.Room = null;
			}
			else if (type == GameObjectType.Projectile)
			{
				Projectile projectile = null;
				if (_projectiles.Remove(objectId, out projectile) == false)
					return;

				GetZone(projectile.CellPos).Projectiles.Remove(projectile);

				projectile.Room = null;
			}
		}

		public void HandleMove(Player player, C_Move movePacket)
		{
			if (player == null)
				return;

			// TODO : 검증
			PositionInfo movePosInfo = movePacket.PosInfo;
			ObjectInfo info = player.Info;

			// 다른 좌표로 이동할 경우, 갈 수 있는지 체크
			if (movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
			{
				if (Map.CanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false)
					return;
			}

			info.PosInfo.State = movePosInfo.State;
			info.PosInfo.MoveDir = movePosInfo.MoveDir;
			Map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));

			// 다른 플레이어한테도 알려준다
			S_Move resMovePacket = new S_Move();
			resMovePacket.ObjectId = player.Info.ObjectId;
			resMovePacket.PosInfo = movePacket.PosInfo;

			Broadcast(player.CellPos, resMovePacket);
		}

		public Player FindPlayer(Func<GameObject, bool> condition)
		{
			foreach (Player player in _players.Values)
			{
				if (condition.Invoke(player))
					return player;
			}

			return null;
		}

		public void Broadcast(Vector2Int pos, IMessage packet)
		{
			List<Zone> zones = GetAbjacentZones(pos);

			foreach (Zone zone in zones)
            {
				// 내가 있는 존에만 보내자
				foreach (Player p in _players.Values)
				{
					int dx = p.CellPos.x - pos.x;
					int dy = p.CellPos.y - pos.y;

					// 눈 밖이면 체크할 필요 없다.
					if (Math.Abs(dx) > GameRoom.VisionCells)
						continue;
					if (Math.Abs(dy) > GameRoom.VisionCells)
						continue;

					p.Session.Send(packet);
				}
			}
		}

		public List<Zone> GetAbjacentZones(Vector2Int cell, int cells = GameRoom.VisionCells)
        {
			HashSet<Zone> zones = new HashSet<Zone>();
			int[] delta = new int[2] { -cells, +cells };

			foreach (int dy in delta)
            {
				foreach(int dx in delta)
                {
					int x = cell.x + dx;
					int y = cell.y + dy;

					Zone zone = GetZone(new Vector2Int(y, x));
					if (zone == null)
						continue;

					zones.Add(zone);
                }
            }

			return zones.ToList();
        }
	}
}
