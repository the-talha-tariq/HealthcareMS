using AppointmentService.DTOs;
using System.Text.Json;

namespace AppointmentService.Services
{
    public class PatientLookupService : IPatientLookupService
    {
        private readonly HttpClient _httpClient;

        public PatientLookupService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PatientLookupDto?> GetPatientAsync(int patientId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/patients/{patientId}");
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<PatientLookupDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }
    }
}
