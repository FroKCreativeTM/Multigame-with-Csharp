using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.DB;
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

		// 기존 정보에 추가될 정보들
		public int WeaponDamage { get; private set; }
		public int ArmorDefence { get; private set; }
		public override int TotalAttack { get { return Stat.Attack + WeaponDamage; } }
		public override int TotalDefence { get { return ArmorDefence; } }

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

		public void HandleEquipItem(C_EquipItem equipPacket)
		{
			Item item = _Inventory.Get(equipPacket.ItemDbID);
			if (item == null)
				return;

			if (item.ItemType == ItemType.Consumable)
				return;

			// 착용 요청이라면, 겹치는 부위 해제
			if (equipPacket.Equipped)
			{
				Item unequipItem = null;

				if (item.ItemType == ItemType.Weapon)
				{
					unequipItem = _Inventory.Find(
						i => i.Equipped && i.ItemType == ItemType.Weapon);
				}
				else if (item.ItemType == ItemType.Armor)
				{
					ArmorType armorType = ((Armor)item).ArmorType;
					unequipItem = _Inventory.Find(
						i => i.Equipped && i.ItemType == ItemType.Armor
							&& ((Armor)i).ArmorType == armorType);
				}

				if (unequipItem != null)
				{
					// 메모리 선적용
					unequipItem.Equipped = false;

					// DB에 Noti
					DbTransaction.EquipItemNotify(this, unequipItem);

					// 클라에 통보
					S_EquipItem equipOkItem = new S_EquipItem();
					equipOkItem.ItemDbID = unequipItem.ItemDbId;
					equipOkItem.Equipped = unequipItem.Equipped;
					Session.Send(equipOkItem);
				}
			}

			{
				// 메모리 선적용
				item.Equipped = equipPacket.Equipped;

				// DB에 Noti
				DbTransaction.EquipItemNotify(this, item);

				// 클라에 통보
				S_EquipItem equipOkItem = new S_EquipItem();
				equipOkItem.ItemDbID = equipPacket.ItemDbID;
				equipOkItem.Equipped = equipPacket.Equipped;
				Session.Send(equipOkItem);
			}

			RefreshAdditionalStat();
		}

		public void RefreshAdditionalStat()
		{
			WeaponDamage = 0;
			ArmorDefence = 0;

			foreach (Item item in _Inventory.Items.Values)
			{
				if (item.Equipped == false)
					continue;

				switch (item.ItemType)
				{
					case ItemType.Weapon:
						WeaponDamage += ((Weapon)item).Damage;
						break;
					case ItemType.Armor:
						ArmorDefence += ((Armor)item).Defence;
						break;
				}
			}
		}
	}
}
