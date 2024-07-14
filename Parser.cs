using Microsoft.Extensions.Logging;
using System.Xml;

namespace PlayerEventPublisher
{
    public class Parser
    {
        private readonly ILogger<Parser> _logger;

        public Parser(ILogger<Parser> logger)
        {
            _logger = logger;
        }

        // Parses the XML file to extract player information
        public async IAsyncEnumerable<Player> ParseAsync(string xmlFilePath)
        {
            if (string.IsNullOrEmpty(xmlFilePath))
            {
                _logger.LogError("XML file path is null or empty");
                throw new ArgumentNullException(nameof(xmlFilePath));
            }

            var doc = new XmlDocument();

            try
            {
                _logger.LogInformation("Loading XML file: {XmlFilePath}", xmlFilePath);
                var xmlContent = await File.ReadAllTextAsync(xmlFilePath); // Asynchronous file read
                doc.LoadXml(xmlContent);
                _logger.LogInformation("Successfully loaded XML file: {XmlFilePath}", xmlFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading XML file: {XmlFilePath}", xmlFilePath);
                throw;
            }

            XmlNodeList playerNodes = doc.SelectNodes("/players/player_registration");
            if (playerNodes == null)
            {
                _logger.LogWarning("No player registration nodes found in the XML file: {XmlFilePath}", xmlFilePath);
                yield break;
            }

            foreach (XmlNode playerNode in playerNodes)
            {
                var player = new Player
                {
                    Id = playerNode["id"]?.InnerText ?? string.Empty,
                    Name = playerNode["name"]?.InnerText ?? string.Empty,
                    Age = playerNode["age"]?.InnerText ?? string.Empty,
                    Country = playerNode["country"]?.InnerText ?? string.Empty,
                    Position = playerNode["position"]?.InnerText ?? string.Empty,
                    Achievements = new List<Achievement>()
                };

                if (string.IsNullOrEmpty(player.Id))
                {
                    _logger.LogWarning("Player ID is missing or empty in XML node: {PlayerNode}", playerNode.OuterXml);
                    continue;
                }

                XmlNodeList achievementNodes = playerNode.SelectNodes("achievements/achievement");
                if (achievementNodes != null)
                {
                    foreach (XmlNode achievementNode in achievementNodes)
                    {
                        var achievement = new Achievement
                        {
                            Year = achievementNode.Attributes?["year"]?.Value ?? string.Empty,
                            Title = achievementNode.InnerText ?? string.Empty
                        };
                        player.Achievements.Add(achievement);
                    }
                }

                _logger.LogInformation("Parsed player: {PlayerId}, Name: {PlayerName}, Achievements Count: {AchievementsCount}", player.Id, player.Name, player.Achievements.Count);
                yield return player; // Yield each player as soon as it's parsed
            }
        }
    }
}
