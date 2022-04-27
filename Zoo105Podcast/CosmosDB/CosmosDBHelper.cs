using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;

namespace Zoo105Podcast.CosmosDB
{
	public static class CosmosDBHelper
	{
		public const string dbName = "DB_Zoo105Podcast";
		public const string collectionName = "PodcastEpisodes";

		public static async Task<DocumentClient> GetCosmosDBClientAsync(IConfiguration config)
		{
			Uri cosmosDBEndpointUri = new Uri(config["CosmosDBEndpointUrl"]);
			string cosmosDBPrimaryKey = config["CosmosDBPrimaryKey"];

			DocumentClient cosmosDBClient = new DocumentClient(cosmosDBEndpointUri, cosmosDBPrimaryKey);

			await cosmosDBClient.CreateDatabaseIfNotExistsAsync(new Database { Id = dbName });
			await cosmosDBClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(dbName), new DocumentCollection { Id = collectionName });

			return cosmosDBClient;
		}

		public static async Task<PodcastEpisode> GetPodcastEpisodeAsync(DocumentClient cosmosDBClient, string podcastId)
		{
			try
			{
				var tempResult = await cosmosDBClient.ReadDocumentAsync(UriFactory.CreateDocumentUri(dbName, collectionName, podcastId));
				PodcastEpisode result = (dynamic)tempResult.Resource;
				return result;
			}
			catch (DocumentClientException de)
			{
				if (de.StatusCode == HttpStatusCode.NotFound)
					return null;
				throw;
			}
		}

		public static async Task CreateNewEpisodeAsync(DocumentClient cosmosDBClient, PodcastEpisode episode)
		{
			Uri documentCollectionUri = UriFactory.CreateDocumentCollectionUri(dbName, collectionName);
			await cosmosDBClient.CreateDocumentAsync(documentCollectionUri, episode);
		}

		public static async Task UpdateExistingEpisodeAsync(DocumentClient cosmosDBClient, PodcastEpisode episode)
		{
			Uri documentUri = UriFactory.CreateDocumentUri(dbName, collectionName, episode.Id);
			await cosmosDBClient.ReplaceDocumentAsync(documentUri, episode);
		}
	}
}