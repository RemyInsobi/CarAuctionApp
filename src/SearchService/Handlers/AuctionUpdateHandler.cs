using System;
using Contracts;
using Meilisearch;

namespace SearchService.Handlers;

public class AuctionUpdateHandler
{
    public async Task Handle(AuctionUpdated message, MeilisearchClient client)
    {
        await client.Index("items").UpdateDocumentsAsync([message]);
    }

}
