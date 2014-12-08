using System;
using System.Net;
using System.Net.Mail;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace StatusCheck.Lib
{
    public interface INotifier
    {
        void Notify(StatusCheckResult result);
    }

    public class EmailNotifier : INotifier
    {
        public void Notify(StatusCheckResult result)
        {
            var smtpSettings = new TextFileSettings("~/smtpSettings.txt".MapAbsolutePath(), ":");

            var client = new SmtpClient(smtpSettings.Get("host"), smtpSettings.Get("port", 587))
            {
                Credentials = new NetworkCredential(smtpSettings.Get("username"), smtpSettings.Get("password")),
                EnableSsl = true
            };
            var msg = new MailMessage
            {
                From = new MailAddress(smtpSettings.Get("from"), smtpSettings.Get("displayFrom")),
                Subject = smtpSettings.Get("subjectFmt").Fmt(result.Name),
                IsBodyHtml = true,
                Body = string.Format("A status check has failed on {0}<br/><br/>Status Check Result: <pre>{1}</pre>",
                    Environment.MachineName, result.Dump())
            };

            msg.To.Add(smtpSettings.Get("to"));
            client.Send(msg);
        }
    }
}