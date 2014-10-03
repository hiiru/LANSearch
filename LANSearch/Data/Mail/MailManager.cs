using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using LANSearch.Data.Notification;
using LANSearch.Models.Search;

namespace LANSearch.Data.Mail
{
    public class MailManager
    {
        protected AppContext Ctx { get { return AppContext.GetContext();} }

        protected ILog Logger;
        protected ILog MailLogger;
        public MailManager()
        {
            Logger = LogManager.GetCurrentClassLogger();
            MailLogger = LogManager.GetLogger("LANSearch.Data.Mail.MailManager-MailLog");
        }

        protected async Task SendEmail(MailMessage mail)
        {
            try
            {
                Logger.Info("Sending Email to "+mail.To);
                MailLogger.Trace(string.Format("Mail To :{0} Subject: {1}\n{2}\n--------------------------", mail.To.First().Address, mail.Subject, mail.Body));
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

Note: If you didn't request this mail, please ignore it, or if there is a problem, contact me at lansearch@gmx.ch ",
user.UserName, user.EmailValidationKey, confirmLinkSnipet);

            SendEmail(mail).Wait();

        }

        //public void SendNotification(Notification.Notification notification, User.User user, IEnumerable<SearchFile> results)
        public void SendNotification(NotificationEvent notificationEvent)
        {
            if (notificationEvent == null || notificationEvent.Items == null || notificationEvent.Items.Count == 0)
            {
                Logger.Info("Empty NotificationEvent was passed to SendNotification.");
                return;
            }
            if(string.IsNullOrWhiteSpace(notificationEvent.UserEmail) ||
                string.IsNullOrWhiteSpace(notificationEvent.UserName) ||
                string.IsNullOrWhiteSpace(notificationEvent.Name) ||
                string.IsNullOrWhiteSpace(notificationEvent.SearchUrl))
            {
                Logger.WarnFormat("Invalid NotificationEvent was passed to SendNotification, Id:{0}", notificationEvent.NotificationId);
                return;
            }

            var sb = new StringBuilder();
            foreach (var item in notificationEvent.Items.Take(5))
            {
                sb.AppendLine(string.Format("File: {0} ({1})", item.FileName, item.FileSize));
                sb.AppendLine(string.Format("Url: {0}", item.FileUrl));
                sb.AppendLine();
            }

            var mail = new MailMessage(new MailAddress(Ctx.Config.MailFromAddress, Ctx.Config.MailFromName), new MailAddress(notificationEvent.UserEmail, notificationEvent.UserName));
            if (Ctx.Config.MailCopyToSelf)
                mail.Bcc.Add(Ctx.Config.MailFromAddress);
            mail.Subject = "LANSearch Search Notification: " + notificationEvent.Name;
            mail.IsBodyHtml = false;
            mail.Body = string.Format(
@"Hi {0},

We have new results for your Notification: {1}

{2}
Alternativly you can open the Search-results here:
{3}

Regards,
LANSearch

Note: If you didn't request this mail, please ignore it, or if there is a problem, contact me at lansearch@gmx.ch ",
notificationEvent.UserName, notificationEvent.Name, sb, notificationEvent.SearchUrl);
            //SendEmail(mail).Wait();

            LogEmail(mail);
        }

        private void LogEmail(MailMessage mail)
        {
            MailLogger.Warn(string.Format("Mail To :{0} Subject: {1}\n{2}\n--------------------------", mail.To.First().Address, mail.Subject, mail.Body));
        }
    }
}
