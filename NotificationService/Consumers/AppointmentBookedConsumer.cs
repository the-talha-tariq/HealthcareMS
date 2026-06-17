using NotificationService.Events;
using NotificationService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotificationService.Consumers
{
    public class AppointmentBookedConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentBookedConsumer> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        // Constants
        private const string MainQueue = "appointment.booked";
        private const string DeadLetterQueue = "appointment.booked.failed";
        private const string RetryQueue = "appointment.booked.retry";
        private const int MaxRetryCount = 3;
        private const int RetryDelayMs = 5000; // 5 seconds between retries

        public AppointmentBookedConsumer(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            ILogger<AppointmentBookedConsumer> logger)
        {
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest"
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // 1. Declare Dead Letter Queue first
            // This holds messages that failed after all retries
            await _channel.QueueDeclareAsync(
                queue: DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // 2. Declare Retry Queue with TTL
            // Messages sit here for RetryDelayMs before going back to main queue
            var retryQueueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", "" },
                { "x-dead-letter-routing-key", MainQueue }, // After TTL, send back to main queue
                { "x-message-ttl", RetryDelayMs }           // Wait 5 seconds before retry
            };

            await _channel.QueueDeclareAsync(
                queue: RetryQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: retryQueueArgs
            );

            // 3. Declare Main Queue
            await _channel.QueueDeclareAsync(
                queue: MainQueue,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Process one message at a time
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("NotificationService listening on queue: {Queue}", MainQueue);

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel!);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                // Read retry count from message headers
                var headers = ea.BasicProperties.Headers ?? new Dictionary<string, object?>();
                var retryCount = headers.ContainsKey("x-retry-count")
                    ? Convert.ToInt32(headers["x-retry-count"])
                    : 0;

                // Read message body
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                _logger.LogInformation(
                    "Processing message (Attempt {Attempt}/{Max}): {Message}",
                    retryCount + 1, MaxRetryCount, json);

                try
                {
                    // Deserialize event
                    var bookingEvent = JsonSerializer.Deserialize<AppointmentBookedEvent>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (bookingEvent == null)
                        throw new InvalidOperationException("Failed to deserialize message.");

                    // Send email
                    using var scope = _scopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    await emailService.SendAppointmentConfirmationAsync(
                        bookingEvent.PatientEmail,
                        bookingEvent.PatientName,
                        bookingEvent.DoctorName,
                        bookingEvent.Department,
                        bookingEvent.AppointmentDate,
                        bookingEvent.TimeSlot
                    );

                    // Success — acknowledge and remove from queue
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);

                    _logger.LogInformation(
                        "Successfully processed appointment {Id} for {Email}",
                        bookingEvent.AppointmentId,
                        bookingEvent.PatientEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to process message (Attempt {Attempt}/{Max})",
                        retryCount + 1, MaxRetryCount);

                    // Acknowledge original message — remove it from main queue
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);

                    if (retryCount < MaxRetryCount)
                    {
                        // Still have retries left — send to retry queue
                        await SendToRetryQueue(body, retryCount + 1, ex.Message);
                    }
                    else
                    {
                        // All retries exhausted — send to dead letter queue
                        await SendToDeadLetterQueue(body, ex.Message);
                    }
                }
            };

            await _channel!.BasicConsumeAsync(
                queue: MainQueue,
                autoAck: false,
                consumer: consumer
            );

            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000, stoppingToken);
        }

        // Send to retry queue — will come back to main queue after delay
        private async Task SendToRetryQueue(byte[] body, int retryCount, string errorMessage)
        {
            var properties = new BasicProperties
            {
                Persistent = true,
                Headers = new Dictionary<string, object?>
                {
                    { "x-retry-count", retryCount },
                    { "x-error-message", errorMessage },
                    { "x-first-failed-at", DateTime.UtcNow.ToString("o") }
                }
            };

            await _channel!.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: RetryQueue,
                mandatory: false,
                basicProperties: properties,
                body: body
            );

            _logger.LogWarning(
                "Message sent to retry queue. Attempt {Retry}/{Max}. Retrying in {Delay}ms",
                retryCount, MaxRetryCount, RetryDelayMs);
        }

        // Send to dead letter queue — manual intervention required
        private async Task SendToDeadLetterQueue(byte[] body, string errorMessage)
        {
            var properties = new BasicProperties
            {
                Persistent = true,
                Headers = new Dictionary<string, object?>
                {
                    { "x-retry-count", MaxRetryCount },
                    { "x-final-error", errorMessage },
                    { "x-failed-at", DateTime.UtcNow.ToString("o") }
                }
            };

            await _channel!.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: DeadLetterQueue,
                mandatory: false,
                basicProperties: properties,
                body: body
            );

            _logger.LogError(
                "Message moved to dead letter queue after {Max} failed attempts. Error: {Error}",
                MaxRetryCount, errorMessage);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _channel?.CloseAsync()!;
            await _connection?.CloseAsync()!;
            await base.StopAsync(cancellationToken);
        }
    }
}