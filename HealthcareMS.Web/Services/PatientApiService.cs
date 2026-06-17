using HealthcareMS.Web.Models;
using System.Net.Http.Json;

namespace HealthcareMS.Web.Services
{
    public class PatientApiService
    {
        private readonly HttpClient _httpClient;

        public PatientApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<PatientModel>> GetAllAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<PatientModel>>("gateway/patients");
            return result ?? new List<PatientModel>();
        }

        public async Task<PatientModel?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"gateway/patients/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PatientModel>();
        }

        public async Task<(bool Success, string? Error)> CreateAsync(CreatePatientModel model)
        {
            var response = await _httpClient.PostAsJsonAsync("gateway/patients", model);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdatePatientModel model)
        {
            var response = await _httpClient.PutAsJsonAsync($"gateway/patients/{id}", model);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"gateway/patients/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
