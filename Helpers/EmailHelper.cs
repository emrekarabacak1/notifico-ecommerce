using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Serilog;   // <-- ekle

public class EmailHelper
{
    private readonly IConfiguration _config;

    public EmailHelper(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var emailSettings = _config.GetSection("EmailSettings");
        var smtpHost = emailSettings["SmtpHost"];
        var smtpPort = int.Parse(emailSettings["SmtpPort"]);
        var smtpUser = emailSettings["SmtpUser"];
        var smtpPass = emailSettings["SmtpPass"];
        var senderEmail = emailSettings["SenderEmail"];
        var senderName = emailSettings["SenderName"];

        try
        {
            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = true;

                var mail = new MailMessage();
                mail.From = new MailAddress(senderEmail, senderName);
                mail.To.Add(to);
                mail.Subject = subject;
                mail.Body = htmlBody;
                mail.IsBodyHtml = true;

                await client.SendMailAsync(mail);

                Log.Information("E-posta gönderildi: {To} | Konu: {Subject}", to, subject);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "E-posta gönderim hatası: {To} | Konu: {Subject}", to, subject);
            throw new Exception("Eposta gönderim hatası: " + ex.Message, ex);
        }
    }
}
