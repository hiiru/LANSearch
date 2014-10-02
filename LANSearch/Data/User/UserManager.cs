using System.Security.Cryptography;
using System.Text;
using Hangfire;
using LANSearch.Data.Redis;
using Nancy;
using Nancy.Authentication.Forms;
using ServiceStack.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LANSearch.Data.User
{
    public class UserManager
    {
        protected RedisManager RedisManager;

        protected AppContext Ctx { get { return AppContext.GetContext(); } }

        public UserManager(RedisManager redisManager)
        {
            RedisManager = redisManager;
        }

        public User Get(string name)
        {
            return RedisManager.UserGet(name);
        }

        public User Get(int id)
        {
            return RedisManager.UserGet(id);
        }

        public List<User> GetPaged(int page, int pagesize, out int count)
        {
            var offset = page * pagesize;
            var users = RedisManager.UserGetAll();
            count = users.Count;
            var filtered = users.OrderByDescending(x => x.Registed);
            return filtered.Skip(offset).Take(pagesize).ToList();
        }

        #region Login / Registration

        public int Login(string name, string password, Request request, out Guid guid)
        {
            guid = Guid.Empty;
            if (string.IsNullOrWhiteSpace(name) || request == null)
            {
                return 1;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                return 2;
            }
            using (var lockUser = RedisManager.UserGetLock(name))
            {
                var user = RedisManager.UserGet(name);
                if (user == null)
                {
                    return 3;
                }
                if (user.Disabled)
                {
                    return 4;
                }
                if (!ValidatePassword(user,password))
                {
                    return 5;
                }
                user.LastLogin = DateTime.Now;
                user.LoginLog.Add(string.Format("[{0}] Login from {1}", user.LastLogin, request.UserHostAddress));
                RedisManager.UserSave(user);
                guid = RedisManager.UserSessionStart(user);
                return 0;
            }
        }

        public UserRegisterState Register(string username, string email, string password, string password2, Request request, out Guid guid, bool admin = false, string[] claims = null)
        {
            guid = Guid.Empty;
            var state = ValidateRegister(username, email, password, password2);
            if (state != UserRegisterState.Unknown)
            {
                return state;
            }

            using (var lockUser = RedisManager.UserGetLock(username))
            {
                if (RedisManager.UserIsNameUsed(username)) return UserRegisterState.UserAlreadyTaken;
                if (RedisManager.UserIsEmailUsed(email)) return UserRegisterState.EmailAlreadyUsed;

                var user = new User
                {
                    UserName = username,
                    Email = email,
                    Password = PasswordHash.CreateHash(password),
                    Registed = DateTime.Now,
                };

                user.LoginLog =
                    new List<string>
                    {
                        string.Format("[{0}] {2}Registration from {1}", user.Registed, request.UserHostAddress, admin?"Admin-":"")
                    };
                user.ClaimClear();

                if (claims != null)
                {
                    foreach (var claim in claims)
                    {
                        user.ClaimAdd(claim);
                    }
                }
                else
                {
                    user.ClaimAdd(Ctx.Config.UserRequireMailActivation ? UserRoles.UNVERIFIED : UserRoles.MEMBER);
                }

                if (user.ClaimHas(UserRoles.UNVERIFIED))
                {
                    user.EmailValidationKey = GetUniqueKey(6);
                    RedisManager.UserSave(user);
                    BackgroundJob.Enqueue(() => Ctx.MailManager.SendActivationMail(user, request.Url.HostName));
                } else
                    RedisManager.UserSave(user);


                guid = RedisManager.UserSessionStart(user);
                return UserRegisterState.Ok;
            }
        }

        private UserRegisterState ValidateRegister(string username, string email, string password, string password2)
        {
            var state = UserRegisterState.Unknown;
            if (string.IsNullOrWhiteSpace(username))
            {
                state = state.Add(UserRegisterState.UserEmpty);
            }
            else if (username.Length < 3)
            {
                state = state.Add(UserRegisterState.UserTooShort);
            }
            else if (username.Length > 20)
            {
                state = state.Add(UserRegisterState.UserTooLong);
            }
            else if (RedisManager.UserIsNameUsed(username))
            {
                state = state.Add(UserRegisterState.UserAlreadyTaken);
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                state = state.Add(UserRegisterState.EmailEmpty);
            }
            else if (email.Length > 50)
            {
                state = state.Add(UserRegisterState.EmailTooLong);
            }
            else if (!ValidationHelper.EmailValid(email))
            {
                state = state.Add(UserRegisterState.EmailInvalid);
            }
            else if (RedisManager.UserIsEmailUsed(email))
            {
                state = state.Add(UserRegisterState.EmailAlreadyUsed);
            }

            if (password != password2)
            {
                state = state.Add(UserRegisterState.PassMissmatch);
            }
            else if (string.IsNullOrWhiteSpace(password))
            {
                state = state.Add(UserRegisterState.PassEmpty);
            }
            return state;
        }

        public bool ValidatePassword(User user, string password)
        {
            if (user == null || string.IsNullOrWhiteSpace(password)) return false;
            return PasswordHash.ValidatePassword(password, user.Password);
        }

        public User GetByGuid(Guid identifier)
        {
            return RedisManager.UserSessionResolve(identifier);
        }

        #endregion Login / Registration

        public void SetDisabled(int id, bool status)
        {
            if (id < 1) return;
            var user = Get(id);
            if (user == null || user.Disabled == status) return;
            user.Disabled = status;
            RedisManager.UserSave(user);
        }

        public int UpdateUser(string username, string name = null, string email = null, string password = null, string emailkey = null, bool? disabled = null)
        {
            if (string.IsNullOrWhiteSpace(username)) return 0;
            var user = Get(username);
            if (user == null) return 0;

            if (!string.IsNullOrWhiteSpace(name) && user.UserName != name)
            {
                if (RedisManager.UserIsNameUsed(username))
                    return 1;
                user.UserName = name;
            }
            if (!string.IsNullOrWhiteSpace(email))
            {
                user.Email = email;
            }
            if (!string.IsNullOrWhiteSpace(password))
            {
                user.Password = PasswordHash.CreateHash(password);
            }
            if (!string.IsNullOrWhiteSpace(emailkey))
            {
                user.EmailValidationKey = emailkey;
            }
            if (disabled.HasValue)
            {
                user.Disabled = disabled.Value;
            }
            RedisManager.UserSave(user);
            return 2;
        }

        public void UpdateUserClaims(string name, string[] add = null, string[] remove = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (add == null && remove == null) return;
            var user = Get(name);
            if (user == null) return;
            bool changed = false;
            if (add != null)
            {
                foreach (var claim in add)
                {
                    if (user.ClaimHas(claim)) continue;

                    user.ClaimAdd(claim);
                    user.LoginLog.Add(string.Format("[{0}] Admin added role {1}", DateTime.Now, claim));
                    changed = true;
                }
            }
            if (remove != null)
            {
                foreach (var claim in remove)
                {
                    if (!user.ClaimHas(claim)) continue;

                    user.ClaimRemove(claim);
                    user.LoginLog.Add(string.Format("[{0}] Admin removed role {1}", DateTime.Now, claim));
                    changed = true;
                }
            }

            if (changed)
                RedisManager.UserSave(user);
        }

        public User GetUserByOwinEnvironment(IDictionary<string, object> owinEnvironment)
        {
            if (owinEnvironment == null) return null;
            var requestHeaders = ((IDictionary<string, string[]>)owinEnvironment["owin.RequestHeaders"]);
            if (!requestHeaders.ContainsKey("Cookie"))
                return null;

            var authCookieValue = requestHeaders["Cookie"]
                .Select(cookie => cookie.Split(';')[0])
                .Select(cookie =>
                {
                    var x = cookie.Split('=');
                    if (x.Length == 2 && x[0] == FormsAuthentication.FormsAuthenticationCookieName)
                        return x[1];
                    return null;
                }).FirstOrDefault(cookie => cookie != null);
            if (string.IsNullOrWhiteSpace(authCookieValue)) return null;

            var key = FormsAuthentication.DecryptAndValidateAuthenticationCookie(authCookieValue, AuthenticationConfiguration.GetContext().FormsAuthenticationConfiguration);
            Guid guid;
            if (!Guid.TryParse(key, out guid))
                return null;
            return GetByGuid(guid);
        }

        public void UpdateEmail(User user, string email)
        {
            user.Email = email;
            RedisManager.UserSave(user);
        }

        public void ChangePassword(User user, string newpass)
        {
            user.Password = PasswordHash.CreateHash(newpass);
            RedisManager.UserSave(user);
        }


        public string GetUniqueKey(int maxSize)
        {
            var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            var data = new byte[maxSize];
            var crypto = new RNGCryptoServiceProvider();
            crypto.GetNonZeroBytes(data);
            var result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }

        public bool ActivateAccount(User user, string code)
        {
            if (user == null || string.IsNullOrWhiteSpace(code)) return false;
            bool codeValid=user.EmailValidationKey == code;
            if (codeValid)
            {
                user.EmailValidationKey = null;
                user.ClaimRemove(UserRoles.UNVERIFIED);
                user.ClaimAdd(UserRoles.MEMBER);
                RedisManager.UserSave(user);
            }
            return codeValid;
        }

        public int ResendActivationMail(User user, string mail, Request request)
        {
            if (user == null || string.IsNullOrWhiteSpace(mail) || !ValidationHelper.EmailValid(mail)) return 2;
            if (user.Email != mail)
            {
                if (RedisManager.UserIsEmailUsed(mail)) return 3;
                user.Email = mail;
            }

            user.EmailValidationKey = GetUniqueKey(6);
            RedisManager.UserSave(user);
            BackgroundJob.Enqueue(() => Ctx.MailManager.SendActivationMail(user, request.Url.HostName));
            return 0;
        }
    }
}