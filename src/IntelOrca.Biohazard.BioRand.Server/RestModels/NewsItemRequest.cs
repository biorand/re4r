﻿namespace IntelOrca.Biohazard.BioRand.Server.RestModels
{
    public class NewsItemRequest
    {
        public int GameId { get; set; }
        public long Timestamp { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
    }
}
