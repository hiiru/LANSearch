using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;

namespace LANSearch.Data.Mail
{
    public class MailManager
    {
        protected AppContext Ctx { get { return AppContext.GetContext();} }

        protected ILog Logger;
        public MailManager()
        {
            Logger = LogManager.GetCurrentClassLogger();
        }

        protected async Task SendEmail(MailMessage mail)
        {
            try
            {
                Logger.Info("Sending Email to "+mail.To);
                using (
                    var client = new SmtpClient
                    {
                        UseDefaultCredentials = false,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Host = Ctx.Config.MailServer,
                        Port = Ctx.Config.MailPort,
                        EnableSsl = Ctx.Config.MailSsl,
                        Credentials = new NetworkCredential(Ctx.Config.MailAccount, Ctx.Config.MailPassword),
                    })
                {
                    await client.SendMailAsync(mail);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Exception while sending mail to "+mail.To, e);
            }
        }

        public void SendTestMail()
        {
            var mail = new MailMessage(new MailAddress("lansearch@gmx.ch","LANSearch"),new MailAddress( "fhsfwegkgmkfso@mailinator.com"));
            mail.Subject = "Test mail";
            mail.Body = "This is a short test mail";
            SendEmail(mail);

        }

        public void SendActivationMail(User.User user, string host)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.EmailValidationKey))
                return;

            string confirmLinkSnipet="";
            if (!string.IsNullOrWhiteSpace(host) || InitConfig.ListenHost != "+")
            {
                var sbUrl = new StringBuilder("http://");
                sbUrl.Append(host ?? InitConfig.ListenHost);
                if (InitConfig.ListenPort != 80)
                    sbUrl.AppendFormat(":{0}", InitConfig.ListenPort);
                sbUrl.AppendFormat("/Member/Confirm/{0}/{1}", user.Id, user.EmailValidationKey);
                sbUrl.Insert(0, @"
Alternatively you can use this link:
");
                sbUrl.AppendLine();
                confirmLinkSnipet = sbUrl.ToString();
            }

            var mail = new MailMessage(new MailAddress(Ctx.Config.MailFromAddress, Ctx.Config.MailFromName), new MailAddress(user.Email,user.UserName));
            if (Ctx.Config.MailCopyToSelf)
                mail.Bcc.Add(Ctx.Config.MailFromAddress);
            mail.Subject = "LANSearch User Activation";
            mail.IsBodyHtml = false;
            mail.Body = string.Format(
@"Hi {0},

Please confirm your E-mail Address by entering this Code in your User Profile.
Code: {1}
{2}
Regards,
LANSearch

Note: If you didn't request this mail, please ignore it, or if there is a problem, contact us at lansearch@gmx.ch ",
user.UserName, user.EmailValidationKey, confirmLinkSnipet);

            SendEmail(mail);

        }
    }
}
