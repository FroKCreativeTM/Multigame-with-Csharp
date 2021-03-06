using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public class Arrow : Projectile
	{
		public GameObject Owner { get; set; }

		public override void Update()
		{
			if (Data == null || Data.projectile == null || Owner == null || Room == null)
				return;

			int tick = (int)(1000 / Data.projectile.speed);
			Room.PushAfter(tick, Update);	// 성능개선!
											// 매 프레임마다 하는 것이 아니라 
											// tick마다 예약제로 둔다.

			Vector2Int destPos = GetFrontCellPos();
			if (Room.Map.CanGo(destPos))
			{
				CellPos = destPos;

				S_Move movePacket = new S_Move();
				movePacket.ObjectId = Id;
				movePacket.PosInfo = PosInfo;

				// 존 안의 자기의 위치를 넘겨준다.
				Room.Broadcast(CellPos, movePacket);

				Console.WriteLine("Move Arrow");
			}
			else
			{
				GameObject target = Room.Map.Find(destPos);
				if (target != null)
				{
					target.OnDamaged(this, Data.damage + Owner.TotalAttack);
				}

				// 소멸
				Room.Push(Room.LeaveGame, Id);
			}
		}

		public override GameObject GetOwner()
        {
			return Owner;
        }
	}
}
