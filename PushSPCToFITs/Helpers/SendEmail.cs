using PushSPCToFITs.Context;
using PushSPCToFITs.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

namespace PushSPCToFITs.Helpers
{
    public class SendEmail
    {
        public static void SendNotification(string emailBody, string subject)
        {
            Log.Information("Email subject:  {0}", subject);
            Log.Information("Email body:  {0}", emailBody);

            try
            {
                MailAddress from = new MailAddress("CIMAlerts@nlight.net");
                MailAddress to = new MailAddress("sha_itswalter@nlight.net");
                
                using (MailMessage mail = new MailMessage(from, to))
                {
                    using (SmtpClient client = new SmtpClient())
                    {
                        client.Port = 25;
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        client.UseDefaultCredentials = false;
                        client.Host = "10.10.5.4";
                        mail.Subject = subject;
                        
                        mail.To.Add(to);                        
                        mail.Body = emailBody;
                        client.Send(mail);
                    }
                }
                
            }
            catch (Exception e)
            {
                Log.Error("{0}", e.Message);
                if (e.InnerException != null)
                {
                    Log.Error("Inner exception {0}", e.InnerException.Message);
                }
            }
        }

    }
}

