using HealthcareMS.Web.Models;
using System.Net.Http.Json;

namespace HealthcareMS.Web.Services
{
    public class AppointmentApiService
    {
        private readonly HttpClient _httpClient;

        public AppointmentApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<AppointmentModel>> GetAllAsync()
        {
            using var response = await _httpClient.GetAsync("gateway/appointments");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return new List<AppointmentModel>();

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<List<AppointmentModel>>();
            return result ?? new List<AppointmentModel>();
        }

        public async Task<List<AppointmentModel>> GetByPatientIdAsync(int patientId)
        {
            using var response = await _httpClient.GetAsync(
                $"gateway/appointments/patient/{patientId}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return new List<AppointmentModel>();

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<List<AppointmentModel>>();
            return result ?? new List<AppointmentModel>();
        }

        public async Task<(bool Success, string? Error)> CreateAsync(CreateAppointmentModel model)
        {
            var response = await _httpClient.PostAsJsonAsync("gateway/appointments", model);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }

        public async Task<bool> CancelAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"gateway/appointments/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
