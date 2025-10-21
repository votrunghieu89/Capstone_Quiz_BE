using Capstone.Model;
using Capstone.Repositories;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Capstone.RabbitMQ
{
    public class AuditLogConsumer : BackgroundService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuditLogConsumer> _logger;
        private readonly RabbitMQModel _settings;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly SemaphoreSlim _semaphore = new(10); // Giới hạn 10 concurrent tasks

        public AuditLogConsumer(
            IServiceProvider serviceProvider,
            ILogger<AuditLogConsumer> logger,
            IOptions<RabbitMQModel> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _settings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await InitializeRabbitMQAsync(stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel!);
                consumer.ReceivedAsync += async (sender, e) =>
                {
                    await _semaphore.WaitAsync(stoppingToken);
                    try
                    {
                        await ProcessMessageAsync(e, stoppingToken);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                };

                await _channel!.BasicConsumeAsync(
                    queue: "AuditLog",
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("AuditLog consumer started successfully");

                // Keep running until cancelled
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("AuditLog consumer is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fatal error in AuditLog consumer");
                throw;
            }
        }

        private async Task InitializeRabbitMQAsync(CancellationToken cancellationToken)
        {
            int retryCount = 0;
            const int maxRetries = 5;
            const int retryDelayMs = 5000;

            while (retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Attempting to connect to RabbitMQ (Attempt {Attempt}/{MaxRetries})",
                        retryCount + 1, maxRetries);

                    var factory = new ConnectionFactory
                    {
                        HostName = _settings.HostName,
                        Port = _settings.Port,
                        UserName = _settings.UserName,
                        Password = _settings.Password,
                        AutomaticRecoveryEnabled = true,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                        RequestedHeartbeat = TimeSpan.FromSeconds(60)
                    };

                    _connection = await factory.CreateConnectionAsync(cancellationToken);
                    _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                    // Configure channel
                    await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken);

                    // Declare queue
                    await _channel.QueueDeclareAsync(
                        queue: "AuditLog",
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null,
                        cancellationToken: cancellationToken);

                    _logger.LogInformation("Successfully connected to RabbitMQ");
                    return;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogError(ex, "Failed to connect to RabbitMQ (Attempt {Attempt}/{MaxRetries})",
                        retryCount, maxRetries);

                    if (retryCount >= maxRetries)
                    {
                        _logger.LogCritical("Max retry attempts reached. Unable to connect to RabbitMQ");
                        throw;
                    }

                    await Task.Delay(retryDelayMs * retryCount, cancellationToken);
                }
            }
        }

        private async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
        {
            string? json = null;
            try
            {
                json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("Received empty message, skipping. DeliveryTag={DeliveryTag}",
                        eventArgs.DeliveryTag);
                    await _channel!.BasicNackAsync(eventArgs.DeliveryTag, false, false, cancellationToken);
                    return;
                }

                var log = JsonConvert.DeserializeObject<AuditLogModel>(json);

                if (log == null)
                {
                    _logger.LogWarning("Failed to deserialize audit log message. DeliveryTag={DeliveryTag}, JSON={JSON}",
                        eventArgs.DeliveryTag, json);
                    await _channel!.BasicNackAsync(eventArgs.DeliveryTag, false, false, cancellationToken);
                    return;
                }

                // Validate log data
                if (log.AccountId <= 0 || string.IsNullOrWhiteSpace(log.Action))
                {
                    _logger.LogWarning("Invalid audit log data: AccountId={AccountId}, Action={Action}, DeliveryTag={DeliveryTag}",
                        log.AccountId, log.Action, eventArgs.DeliveryTag);
                    await _channel!.BasicNackAsync(eventArgs.DeliveryTag, false, false, cancellationToken);
                    return;
                }

                // Insert log using scoped service
                using (var scope = _serviceProvider.CreateScope())
                {
                    var auditLogRepo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
                    bool success = await auditLogRepo.InsertLog(log);

                    if (success)
                    {
                        await _channel!.BasicAckAsync(eventArgs.DeliveryTag, false, cancellationToken);
                        _logger.LogInformation("Successfully processed audit log: AccountId={AccountId}, Action={Action}, DeliveryTag={DeliveryTag}",
                            log.AccountId, log.Action, eventArgs.DeliveryTag);
                    }
                    else
                    {
                        _logger.LogError("Failed to insert audit log into database: AccountId={AccountId}, Action={Action}, DeliveryTag={DeliveryTag}",
                            log.AccountId, log.Action, eventArgs.DeliveryTag);

                        // Retry với requeue = true
                        await _channel!.BasicNackAsync(eventArgs.DeliveryTag, false, true, cancellationToken);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error. DeliveryTag={DeliveryTag}, JSON={JSON}",
                    eventArgs.DeliveryTag, json);
                // Dead letter queue - không retry
                await _channel!.BasicNackAsync(eventArgs.DeliveryTag, false, false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audit log message. DeliveryTag={DeliveryTag}, JSON={JSON}",
                    eventArgs.DeliveryTag, json);

                // Retry lại sau một khoảng thời gian
                await _channel!.BasicNackAsync(eventArgs.DeliveryTag, false, true, cancellationToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping AuditLog consumer...");
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            try
            {
                _semaphore?.Dispose();
                _channel?.Dispose();
                _connection?.Dispose();
                _logger.LogInformation("AuditLog consumer resources disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing AuditLog consumer resources");
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}