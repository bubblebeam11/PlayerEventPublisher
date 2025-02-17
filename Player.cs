﻿namespace PlayerEventPublisher
{
    public class Player
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Age { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public List<Achievement> Achievements { get; set; } = new List<Achievement>();
    }

    public class Achievement
    {
        public string Year { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
