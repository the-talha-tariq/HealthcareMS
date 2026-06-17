using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;

namespace NotificationService.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAppointmentConfirmationAsync(
            string toEmail,
            string patientName,
            string doctorName,
            string department,
            DateTime appointmentDate,
            string timeSlot)
        {
            // Validate inputs before even trying
            if (string.IsNullOrEmpty(toEmail))
                throw new ArgumentException("Patient email cannot be empty.");

            if (string.IsNullOrEmpty(_configuration["Mailjet:ApiKey"]))
                throw new InvalidOperationException("Mailjet API key is not configured.");

            try
            {
                var client = new MailjetClient(
                    _configuration["Mailjet:ApiKey"],
                    _configuration["Mailjet:SecretKey"]
                );

                var email = new TransactionalEmailBuilder()
                    .WithFrom(new SendContact(
                        _configuration["Email:SenderEmail"],
                        _configuration["Email:SenderName"]))
                    .WithSubject("Appointment Confirmation — HealthcareMS")
                    .WithHtmlPart($@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <div style='background-color: #2196F3; padding: 20px; text-align: center;'>
                                <h1 style='color: white; margin: 0;'>Appointment Confirmed</h1>
                            </div>
                            <div style='padding: 30px; background-color: #f9f9f9;'>
                                <p style='font-size: 16px;'>Dear <strong>{patientName}</strong>,</p>
                                <p>Your appointment has been successfully booked. Here are your details:</p>
                                <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                                    <tr style='background-color: #e3f2fd;'>
                                        <td style='padding: 12px; border: 1px solid #ddd;'><strong>Doctor</strong></td>
                                        <td style='padding: 12px; border: 1px solid #ddd;'>{doctorName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 12px; border: 1px solid #ddd;'><strong>Department</strong></td>
                                        <td style='padding: 12px; border: 1px solid #ddd;'>{department}</td>
                                    </tr>
                                    <tr style='background-color: #e3f2fd;'>
                                        <td style='padding: 12px; border: 1px solid #ddd;'><strong>Date</strong></td>
                                        <td style='padding: 12px; border: 1px solid #ddd;'>{appointmentDate:dddd, MMMM dd yyyy}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 12px; border: 1px solid #ddd;'><strong>Time Slot</strong></td>
                                        <td style='padding: 12px; border: 1px solid #ddd;'>{timeSlot}</td>
                                    </tr>
                                </table>
                                <p>Please arrive 10 minutes before your appointment.</p>
                                <p style='color: #888; font-size: 13px;'>This is an automated message from HealthcareMS.</p>
                            </div>
                        </div>")
                    .WithTo(new SendContact(toEmail, patientName))
                    .Build();

                var response = await client.SendTransactionalEmailAsync(email);

                if (response.Messages != null && response.Messages.Length > 0)
                    _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                else
                    _logger.LogWarning("Email may not have been sent to {Email}", toEmail);
            }
            catch (HttpRequestException ex)
            {
                // Network issue — safe to retry
                _logger.LogError(ex, "Network error sending email to {Email}", toEmail);
                throw; // Rethrow so consumer retries
            }
            catch (Exception ex)
            {
                // Unknown error — log and rethrow
                _logger.LogError(ex, "Unexpected error sending email to {Email}", toEmail);
                throw;
            }
        }
    }
}