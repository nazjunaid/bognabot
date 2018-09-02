﻿using System;
using Bognabot.Net.Api;
using Newtonsoft.Json;

namespace Bognabot.Bitmex.Socket.Responses
{
    public class BookSocketResponse : SocketResponse
    {
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("price")]
        public long Price { get; set; }
    }
}