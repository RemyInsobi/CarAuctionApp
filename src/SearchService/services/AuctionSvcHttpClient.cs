using System;
using Meilisearch;
using Microsoft.AspNetCore.Mvc;
using SearchService.Models;

namespace SearchService.services;

public class AuctionSvcHttpClient(HttpClient httpClient, IConfiguration config, MeilisearchClient seachClient)
{
    public async Task<List<Item>> GetItemsForSearch()
    {
        var search = await seachClient.Index("items")
        .SearchAsync<Item>(string.Empty, new SearchQuery
        {
            Sort = ["updatedAt:desc"],
            Limit = 1,
        });

        var lastUpadted = search.Hits.FirstOrDefault()?.UpdatedAt;

        var dateParam = lastUpadted?.ToString("0") ?? string.Empty;

        var items = await httpClient.GetFromJsonAsync<List<Item>>(
            $"{config["AuctionSvcUrl"]}/api/auctions?dateParam={Uri.EscapeDataString(dateParam)}"
        );

        return items ?? [];
    }
}
