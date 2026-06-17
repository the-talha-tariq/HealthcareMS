using AppointmentService.DTOs;

namespace AppointmentService.Services
{
    public interface IAppointmentService
    {
        Task<IEnumerable<AppointmentResponseDto>> GetAllAsync();
        Task<IEnumerable<AppointmentResponseDto>> GetByPatientIdAsync(int patientId);
        Task<AppointmentResponseDto?> GetByIdAsync(int id);
        Task<AppointmentResponseDto> CreateAsync(CreateAppointmentDto dto);
        Task<AppointmentResponseDto?> UpdateAsync(int id, UpdateAppointmentDto dto);
        Task<bool> CancelAsync(int id);
    }
}
