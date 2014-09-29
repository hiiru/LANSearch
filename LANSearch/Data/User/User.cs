using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LANSearch.Data.User
{
    public class User : IUserIdentity
    {
        public User()
        {
            ClaimList = new List<string>();
        }

        public int Id { get; set; }

        public string UserName { get; set; }

        public List<string> ClaimList { get; set; }

        public IEnumerable<string> Claims { get { return ClaimList.AsEnumerable(); } }

        public void ClaimClear()
        {
            ClaimList.Clear();
        }

        public void ClaimRemove(string claim)
        {
            ClaimList.RemoveAll(x => x == claim);
        }

        public void ClaimAdd(string claim)
        {
            if (ClaimList.All(x => x != claim))
                ClaimList.Add(claim);
        }

        public bool ClaimHas(string claim)
        {
            return ClaimList.Any(x => x == claim);
        }

        /// <summary>
        /// DO NOT STORE THE PASSWORD PLAINTEXT!
        /// This field should be a hash
        /// </summary>
        public string Password { get; set; }

        public string Email { get; set; }

        /// <summary>
        /// Used for Registration to validate email address
        /// </summary>
        public string EmailValidationKey { get; set; }

        /// <summary>
        /// Very simple login log, each entry will contain the datetime and ip.
        /// The last entry is the most recent.
        /// </summary>
        public List<string> LoginLog { get; set; }

        public IEnumerable<string> GetReversedLoginLog()
        {
            return LoginLog.AsEnumerable().Reverse();
        }

        public DateTime Registed { get; set; }

        public DateTime LastLogin { get; set; }

        public bool Disabled { get; set; }
    }
}