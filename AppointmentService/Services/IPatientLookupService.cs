using AppointmentService.DTOs;

namespace AppointmentService.Services
{
    public interface IPatientLookupService
    {
        Task<PatientLookupDto?> GetPatientAsync(int patientId);
    }
}
