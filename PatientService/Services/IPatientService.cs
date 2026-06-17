using PatientService.DTOs;

namespace PatientService.Services
{
    public interface IPatientService
    {
        Task<IEnumerable<PatientResponseDto>> GetAllPatientsAsync();
        Task<PatientResponseDto?> GetPatientByIdAsync(int id);
        Task<PatientResponseDto> CreatePatientAsync(CreatePatientDto dto);
        Task<PatientResponseDto?> UpdatePatientAsync(int id, UpdatePatientDto dto);
        Task<bool> DeletePatientAsync(int id);
    }
}