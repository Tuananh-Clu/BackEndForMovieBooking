﻿using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model.Article;
using System.Text.Json;

namespace MovieTicketWebApi.Service.Article
{
    public class ArticleService
    {
        public readonly HttpClient httpClient;
        public readonly IMongoCollection<MainArticle> mongoCollection;
        public ArticleService(HttpClient http,MongoDbContext dbContext)
        {
            httpClient = http;
            mongoCollection = dbContext.Article;
        }
        public async Task<List<MainArticle>> GetFromUrl()
             
        {
            var AllArticle = new List<MainArticle>();
            var rssUrls = new List<string>
            
          {
            "https://screenrant.com/feed/",
            "https://collider.com/feed/",
            "https://theplaylist.net/feed/",
            "https://www.indiewire.com/c/news/feed/",
            "https://www.comingsoon.net/feed"
            };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            foreach (var url in rssUrls)
            {
                var apiUrl = $"https://api.rss2json.com/v1/api.json?rss_url={url}";
                var response= await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(jsonString); 

                    var result = JsonSerializer.Deserialize<ListArticle>(jsonString);

                    if (result?.items != null)
                    {
                        AllArticle.AddRange(result.items);
                    }
                }
            }
            return AllArticle;



        }
        public async Task Save()
        {
            var data= await GetFromUrl();
            await mongoCollection.DeleteManyAsync(_ => true);
            await mongoCollection.InsertManyAsync(data);

        }
    }
    }
