using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PlayerEventPublisher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var configuration = host.Services.GetRequiredService<IConfiguration>();

            try
            {
                var xmlFilePath = GetConfigValue(configuration, "XmlFilePath", logger);
                var rabbitMqHostName = GetConfigValue(configuration, "RabbitMQ:HostName", logger);
                var rabbitMqExchangeName = GetConfigValue(configuration, "RabbitMQ:ExchangeName", logger);
                var encryptionKey = GetConfigValue(configuration, "EncryptionKey", logger);
                var encryptMessages = bool.Parse(GetConfigValue(configuration, "EncryptMessages", logger));

                if (!File.Exists(xmlFilePath))
                {
                    throw new FileNotFoundException($"The specified XML file does not exist: {xmlFilePath}");
                }

                var playerParser = host.Services.GetRequiredService<Parser>();
                var rabbitMqLogger = host.Services.GetRequiredService<ILogger<EventPublisher>>();

                using (var publisher = new EventPublisher(rabbitMqHostName, rabbitMqExchangeName, encryptionKey, encryptMessages, rabbitMqLogger))
                {
                    await foreach (var player in playerParser.ParseAsync(xmlFilePath)) // Asynchronously process each player
                    {
                        await publisher.PublishPlayerRegistrationEventAsync(player, xmlFilePath);
                        await publisher.PublishPlayerAchievementsEventAsync(player, xmlFilePath);
                    }
                }

                logger.LogInformation("Successfully published player data to RabbitMQ.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during execution.");
            }
        }


        // unified method for reading config values
        private static string GetConfigValue(IConfiguration configuration, string key, ILogger logger)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Configuration value for '{key}' is missing or empty.");
            }
            logger.LogInformation("Loaded configuration value for {Key}: {Value}", key, value);
            return value;
        }

        // build host and add services
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Read config
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(configure => configure.AddConsole());
                    // Transient is clean and safe, parser is stateless.
                    services.AddTransient<Parser>();
                    // Singleton for ensuring only one connection
                    services.AddSingleton<EventPublisher>(provider =>
                    {
                        // Setup parameters for event publisher
                        var configuration = provider.GetRequiredService<IConfiguration>();
                        var logger = provider.GetRequiredService<ILogger<EventPublisher>>();
                        return new EventPublisher(
                            GetConfigValue(configuration, "RabbitMQ:HostName", logger),
                            GetConfigValue(configuration, "RabbitMQ:ExchangeName", logger),
                            GetConfigValue(configuration, "EncryptionKey", logger),
                            bool.Parse(GetConfigValue(configuration, "EncryptMessages", logger)),
                            logger);
                    });
                });
    }
}
