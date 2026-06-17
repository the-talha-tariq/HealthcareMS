namespace AppointmentService.Services
{
    public interface IMessagePublisher
    {
        void Publish<T>(T message, string queueName);
    }
}
