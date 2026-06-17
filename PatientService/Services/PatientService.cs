using PatientService.DTOs;
using PatientService.Models;
using PatientService.Repositories;

namespace PatientService.Services
{
    public class PatientServiceImpl : IPatientService
    {
        private readonly IPatientRepository _repository;

        public PatientServiceImpl(IPatientRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<PatientResponseDto>> GetAllPatientsAsync()
        {
            var patients = await _repository.GetAllAsync();
            return patients.Select(MapToResponseDto);
        }

        public async Task<PatientResponseDto?> GetPatientByIdAsync(int id)
        {
            var patient = await _repository.GetByIdAsync(id);
            return patient == null ? null : MapToResponseDto(patient);
        }

        public async Task<PatientResponseDto> CreatePatientAsync(CreatePatientDto dto)
        {
            // Check duplicate email
            var existing = await _repository.GetByEmailAsync(dto.Email);
            if (existing != null)
                throw new InvalidOperationException($"Patient with email {dto.Email} already exists.");

            var patient = new Patient
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                Address = dto.Address,
                BloodGroup = dto.BloodGroup
            };

            var created = await _repository.CreateAsync(patient);
            return MapToResponseDto(created);
        }

        public async Task<PatientResponseDto?> UpdatePatientAsync(int id, UpdatePatientDto dto)
        {
            var patient = await _repository.GetByIdAsync(id);
            if (patient == null) return null;

            patient.Phone = dto.Phone;
            patient.Address = dto.Address;
            patient.Email = dto.Email;

            var updated = await _repository.UpdateAsync(patient);
            return MapToResponseDto(updated);
        }

        public async Task<bool> DeletePatientAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        // Private mapper — we'll replace this with AutoMapper later
        private static PatientResponseDto MapToResponseDto(Patient patient)
        {
            return new PatientResponseDto
            {
                Id = patient.Id,
                FullName = $"{patient.FirstName} {patient.LastName}",
                Email = patient.Email,
                Phone = patient.Phone,
                Gender = patient.Gender,
                BloodGroup = patient.BloodGroup,
                Address = patient.Address,
                DateOfBirth = patient.DateOfBirth,
                CreatedAt = patient.CreatedAt
            };
        }
    }
}