using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuePlainte
{
   public class HuePlainteOrm : ModKit.ORM.ModEntity<HuePlainteOrm>
   {
        [PrimaryKey][AutoIncrement] public int Id { get; set; } 

        public string Demandeur { get; set; }
        public string Défendeur { get; set; }
        public string Raison { get; set; }
        public bool Résolu { get; set; }
   }
}
