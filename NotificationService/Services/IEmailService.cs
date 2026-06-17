namespace NotificationService.Services
{
    public interface IEmailService
    {
        Task SendAppointmentConfirmationAsync(
            string toEmail,
            string patientName,
            string doctorName,
            string department,
            DateTime appointmentDate,
            string timeSlot
        );
    }
}