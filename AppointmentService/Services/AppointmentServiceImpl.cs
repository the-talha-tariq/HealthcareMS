using AppointmentService.DTOs;
using AppointmentService.Events;
using AppointmentService.Models;
using AppointmentService.Repositories;

namespace AppointmentService.Services
{
    public class AppointmentServiceImpl : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IPatientLookupService _patientLookup;
        private readonly IMessagePublisher _publisher;

        public AppointmentServiceImpl(
            IAppointmentRepository repository,
            IPatientLookupService patientLookup,
            IMessagePublisher publisher)
        {
            _repository = repository;
            _patientLookup = patientLookup;
            _publisher = publisher;
        }

        public async Task<IEnumerable<AppointmentResponseDto>> GetAllAsync()
        {
            var appointments = await _repository.GetAllAsync();
            return appointments.Select(MapToDto);
        }

        public async Task<IEnumerable<AppointmentResponseDto>> GetByPatientIdAsync(int patientId)
        {
            var appointments = await _repository.GetByPatientIdAsync(patientId);
            return appointments.Select(MapToDto);
        }

        public async Task<AppointmentResponseDto?> GetByIdAsync(int id)
        {
            var appointment = await _repository.GetByIdAsync(id);
            return appointment == null ? null : MapToDto(appointment);
        }

        public async Task<AppointmentResponseDto> CreateAsync(CreateAppointmentDto dto)
        {
            var patient = await _patientLookup.GetPatientAsync(dto.PatientId);
            if (patient == null)
                throw new InvalidOperationException($"Patient with ID {dto.PatientId} not found.");

            var appointment = new Appointment
            {
                PatientId = dto.PatientId,
                PatientName = patient.FullName,
                PatientEmail = patient.Email,
                DoctorName = dto.DoctorName,
                Department = dto.Department,
                AppointmentDate = dto.AppointmentDate,
                TimeSlot = dto.TimeSlot,
                Notes = dto.Notes
            };

            var created = await _repository.CreateAsync(appointment);

            var bookingEvent = new AppointmentBookedEvent
            {
                AppointmentId = created.Id,
                PatientId = created.PatientId,
                PatientName = created.PatientName,
                PatientEmail = created.PatientEmail,
                DoctorName = created.DoctorName,
                Department = created.Department,
                AppointmentDate = created.AppointmentDate,
                TimeSlot = created.TimeSlot
            };

            _publisher.Publish(bookingEvent, "appointment.booked");

            return MapToDto(created);
        }

        public async Task<AppointmentResponseDto?> UpdateAsync(int id, UpdateAppointmentDto dto)
        {
            var appointment = await _repository.GetByIdAsync(id);
            if (appointment == null) return null;

            appointment.DoctorName = dto.DoctorName;
            appointment.AppointmentDate = dto.AppointmentDate;
            appointment.TimeSlot = dto.TimeSlot;
            appointment.Notes = dto.Notes;

            var updated = await _repository.UpdateAsync(appointment);
            return MapToDto(updated);
        }

        public async Task<bool> CancelAsync(int id)
        {
            return await _repository.CancelAsync(id);
        }

        private static AppointmentResponseDto MapToDto(Appointment a)
        {
            return new AppointmentResponseDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.PatientName,
                PatientEmail = a.PatientEmail,
                DoctorName = a.DoctorName,
                Department = a.Department,
                AppointmentDate = a.AppointmentDate,
                TimeSlot = a.TimeSlot,
                Status = a.Status.ToString(),
                Notes = a.Notes,
                CreatedAt = a.CreatedAt
            };
        }
    }
}
