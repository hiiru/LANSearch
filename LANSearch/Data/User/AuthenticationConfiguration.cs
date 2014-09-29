using Nancy.Authentication.Forms;
using Nancy.Cryptography;

namespace LANSearch.Data.User
{
    public class AuthenticationConfiguration
    {
        protected static AuthenticationConfiguration _Instance;

        public static AuthenticationConfiguration GetContext()
        {
            return _Instance ?? (_Instance = new AuthenticationConfiguration());
        }

        private AuthenticationConfiguration()
        {
            var appCtx = AppContext.GetContext();
            var cryptographyConfiguration = new CryptographyConfiguration(
                    new RijndaelEncryptionProvider(new PassphraseKeyGenerator(appCtx.Config.AppSecurityAesPass, appCtx.Config.AppSecurityAesSalt)),
                    new DefaultHmacProvider(new PassphraseKeyGenerator(appCtx.Config.AppSecurityHmacPass, appCtx.Config.AppSecurityHmacSalt))
                );

            FormsAuthenticationConfiguration = new FormsAuthenticationConfiguration
            {
                RedirectUrl = "~/login",
                CryptographyConfiguration = cryptographyConfiguration,
            };
        }

        public FormsAuthenticationConfiguration FormsAuthenticationConfiguration { get; private set; }
    }
}