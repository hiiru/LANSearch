using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LANSearch.Models.Admin
{
    public class SetupModel
    {
        public string Error { get; set; }

        public Data.User.User User { get; set; }

        public string AesPass { get; set; }
        public string AesSalt { get; set; }
        public string HmacPass { get; set; }
        public string HmacSalt { get; set; }
    }
}
