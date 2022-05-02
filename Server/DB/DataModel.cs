using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DB
{
    [Table("Account")]
    public class AccountDb
    {
        public int AccountDBId { get; set; }
        public string AccountName { get; set; } 
        public ICollection<PlayerDb> Players { get; set; }    
    }

    [Table("Player")]
    public class PlayerDb
    {
        public int PlayerDBId { get; set; } 
        public string PlayerName { get; set; }

        [ForeignKey("Account")]
        public int AccountDBId { get; set; }
        public AccountDb Account { get; set; }

        public ICollection<ItemDb> Items { get; set; }  

        public int Level { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Attack { get; set; }
        public float Speed { get; set; }
        public int TotalExp { get; set; }
    }

    [Table("Item")]
    public class ItemDb
    {
        public int ItemDbId { get; set; } // 데이터베이스에서 지정해주는 아이디
        public int TemplateId { get; set; } // 데이터 시트 상에서의 아이디
        public int Count { get; set; }

        public int Slot { get; set; }

        [ForeignKey("Owner")]
        // ? -> nullable
        public int? OwnerDbId { get; set; }
        public PlayerDb Owner { get; set; }
    }
}
