﻿using System.Text.Json.Serialization;

namespace MovieTicketWebApi.Model.Article
{
    public class ListArticle
    {
        [JsonPropertyName("items")]
        public List<MainArticle> items { get; set; }
    }
}
