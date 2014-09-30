using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LANSearch.Data.User;

namespace LANSearch.Models.Member
{
    public class ProfileModel
    {
        public User User { get; set; }
        public bool ChangePassErrorOldPass { get; set; }
        public bool ChangePassErrorNewPass { get; set; }
        public string ChangePassErrorNewPassText { get; set; }
        public bool ChangePassSuccess { get; set; }

        public bool ChangeMailErrorPass { get; set; }
        public bool ChangeMailErrorMail { get; set; }
        public bool ChangeMailSuccess { get; set; }
    }
}
