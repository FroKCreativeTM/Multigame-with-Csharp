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
        public AccountDb AccountDB { get; set; }
    }
}
