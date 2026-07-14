namespace AIAssistantService.Services
{
    public interface IDataContextService
    {
        Task<string> GetPatientContextAsync(int patientId);
        Task<string> GetAppointmentContextAsync(int patientId);
        Task<string> GetAllPatientsContextAsync();
    }
}