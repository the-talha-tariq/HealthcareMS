using AIAssistantService.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AIAssistantService.Services
{
    public class DataContextService : IDataContextService
    {
        private readonly PatientReadDbContext _patientDb;
        private readonly AppointmentReadDbContext _appointmentDb;

        public DataContextService(
            PatientReadDbContext patientDb,
            AppointmentReadDbContext appointmentDb)
        {
            _patientDb = patientDb;
            _appointmentDb = appointmentDb;
        }

        public async Task<string> GetPatientContextAsync(int patientId)
        {
            var patient = await _patientDb.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && p.IsActive);

            if (patient == null)
                return "No patient found with this ID.";

            return $"""
                Patient ID: {patient.Id}
                Name: {patient.FirstName} {patient.LastName}
                Email: {patient.Email}
                Phone: {patient.Phone}
                Date of Birth: {patient.DateOfBirth:dd MMM yyyy}
                Gender: {patient.Gender}
                Blood Group: {patient.BloodGroup}
                Address: {patient.Address}
                """;
        }

        public async Task<string> GetAppointmentContextAsync(int patientId)
        {
            var appointments = await _appointmentDb.Appointments
                .Where(a => a.PatientId == patientId)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            if (!appointments.Any())
                return "No appointments found for this patient.";

            var sb = new StringBuilder();

            var upcoming = appointments
                .Where(a => a.AppointmentDate > DateTime.UtcNow
                    && a.Status != "Cancelled")
                .ToList();

            var past = appointments
                .Where(a => a.AppointmentDate <= DateTime.UtcNow
                    || a.Status == "Cancelled")
                .ToList();

            if (upcoming.Any())
            {
                sb.AppendLine("UPCOMING APPOINTMENTS:");
                foreach (var appt in upcoming)
                {
                    sb.AppendLine($"""
                        - Appointment #{appt.Id}
                          Doctor: {appt.DoctorName}
                          Department: {appt.Department}
                          Date: {appt.AppointmentDate:dddd, MMMM dd yyyy}
                          Time: {appt.TimeSlot}
                          Status: {appt.Status}
                          Notes: {appt.Notes ?? "None"}
                        """);
                }
            }

            if (past.Any())
            {
                sb.AppendLine("PAST/CANCELLED APPOINTMENTS:");
                foreach (var appt in past.Take(5)) // limit to last 5
                {
                    sb.AppendLine($"""
                        - Appointment #{appt.Id}
                          Doctor: {appt.DoctorName}
                          Department: {appt.Department}
                          Date: {appt.AppointmentDate:dddd, MMMM dd yyyy}
                          Status: {appt.Status}
                        """);
                }
            }

            return sb.ToString();
        }

        public async Task<string> GetAllPatientsContextAsync()
        {
            var patients = await _patientDb.Patients
                .Where(p => p.IsActive)
                .ToListAsync();

            if (!patients.Any())
                return "No patients registered in the system.";

            var sb = new StringBuilder();
            sb.AppendLine($"Total active patients: {patients.Count}");
            sb.AppendLine("Patient list:");

            foreach (var p in patients)
            {
                sb.AppendLine($"- #{p.Id}: {p.FirstName} {p.LastName} " +
                    $"(DOB: {p.DateOfBirth:dd MMM yyyy}, Blood: {p.BloodGroup})");
            }

            return sb.ToString();
        }
    }
}
