using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.DB;
using Server.Game.Item;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public class Player : GameObject
	{
		public int PlayerDbId { get; set; }
		public ClientSession Session { get; set; }

		public Inventory _Inventory { get; private set; } = new Inventory();

		public Player()
		{
			ObjectType = GameObjectType.Player;
		}

		public override void OnDamaged(GameObject attacker, int damage)
		{
			base.OnDamaged(attacker, damage);

		}

		public override void OnDead(GameObject attacker)
		{
			base.OnDead(attacker);
		}

		public void OnLeaveGame()
		{
			// DB 접근해서 하는 것들은 매번 접속하기 그런 코드들!(느리기 때문)
			// 1) 서버가 다운 된다면????
			// 2) 코드의 흐름을 막아버린다!
			// - 비동기가 ?
			// - 스레드에게?
			// -- 문제는 DB 작업은 결과를 받아서 이어서 처리해야되는 경우도 많음
			DbTransaction.SavePlayerStates_AllInOne(this, Room);
		}
	}
}
