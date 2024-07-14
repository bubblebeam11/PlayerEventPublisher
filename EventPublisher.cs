using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace PlayerEventPublisher
{
    public class EventPublisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _exchangeName;
        private readonly ILogger<EventPublisher> _logger;
        private readonly string _encryptionKey;
        private readonly bool _encryptMessages;

        public EventPublisher(string hostName, string exchangeName, string encryptionKey, bool encryptMessages, ILogger<EventPublisher> logger)
        {
            _logger = logger;
            _logger.LogInformation("Creating RabbitMQ connection to host: {HostName}", hostName);
            var factory = new ConnectionFactory() { HostName = hostName };

            _connection = factory.CreateConnection();
            _logger.LogInformation("Successfully created RabbitMQ connection to host: {HostName}", hostName);

            _logger.LogInformation("Creating RabbitMQ channel");
            _channel = _connection.CreateModel();

            _exchangeName = exchangeName;
            _encryptionKey = encryptionKey;
            _encryptMessages = encryptMessages;

            _channel.ExchangeDeclare(exchange: exchangeName, type: "headers");
            _logger.LogInformation("Declared RabbitMQ exchange: {ExchangeName} with type 'headers'", exchangeName);
        }

        public async Task PublishPlayerRegistrationEventAsync(Player player, string xmlFilePath)
        {
            var registrationEvent = new
            {
                event_type = "player_registration",
                player = new
                {
                    id = player.Id,
                    name = player.Name,
                    age = player.Age,
                    country = player.Country,
                    position = player.Position
                }
            };

            _logger.LogInformation("Publishing player registration event for player ID: {PlayerId}", player.Id);
            await PublishEventAsync(registrationEvent, player.Id, registrationEvent.event_type, xmlFilePath);
        }

        public async Task PublishPlayerAchievementsEventAsync(Player player, string xmlFilePath)
        {
            var achievementsEvent = new
            {
                event_type = "player_achievements",
                player_id = player.Id,
                achievements = player.Achievements
            };

            _logger.LogInformation("Publishing player achievements event for player ID: {PlayerId}", player.Id);
            await PublishEventAsync(achievementsEvent, player.Id, achievementsEvent.event_type, xmlFilePath);
        }

        private async Task PublishEventAsync(object eventMessage, string playerId, string eventType, string xmlFilePath)
        {
            var eventId = Guid.NewGuid().ToString();
            _logger.LogInformation("Generated new event ID: {EventId} for event type: {EventType}", eventId, eventType);

            var eventWithId = new
            {
                event_id = eventId,
                eventMessage
            };

            var plainText = JsonConvert.SerializeObject(eventWithId);
            _logger.LogInformation("Serialized event to JSON for event ID: {EventId}", eventId);

            var messageToSend = _encryptMessages ? EncryptWithAes(plainText, _encryptionKey) : plainText;
            _logger.LogInformation("Encrypted event message for event ID: {EventId}", eventId);

            var eventBody = Encoding.UTF8.GetBytes(messageToSend);
            var props = _channel.CreateBasicProperties();

            props.Headers = new Dictionary<string, object>
            {
                { "event_type", eventType },
                { "player_id", playerId },
                { "event_id", eventId },
                { "filename", xmlFilePath }
            };

            await Task.Run(() =>
                _channel.BasicPublish(exchange: _exchangeName,
                                      routingKey: "",
                                      basicProperties: props,
                                      body: eventBody));

            _logger.LogInformation("Published event {EventType} with ID {EventId} for player {PlayerId} from file {XmlFilePath}",
                                   eventType, eventId, playerId, xmlFilePath);
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing RabbitMQ channel and connection");
            _channel?.Dispose();
            _connection?.Dispose();
        }

        private static string EncryptWithAes(string plainText, string key)
        {
            if (key.Length != 32)
            {
                throw new ArgumentException("Encryption key must be 32 bytes long");
            }

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipher = new PaddedBufferedBlockCipher(new AesEngine());
            cipher.Init(true, new KeyParameter(keyBytes));
            var output = new byte[cipher.GetOutputSize(plainBytes.Length)];
            var length = cipher.ProcessBytes(plainBytes, 0, plainBytes.Length, output, 0);
            cipher.DoFinal(output, length);
            return Convert.ToBase64String(output);
        }
    }
}
