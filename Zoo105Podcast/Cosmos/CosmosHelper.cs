using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace Zoo105Podcast.Cosmos;

public class CosmosHelper : IDisposable
{
	private const string databaseId = "DB_Zoo105Podcast";
	private const string containerId = "PodcastEpisodes";

	private CosmosClient cosmosClient;
	private Container? cosmosContainerCache;

	public CosmosHelper(IConfiguration config)
	{
		ArgumentNullException.ThrowIfNull(config);

		string cosmosEndpointUri = config["CosmosEndpointUrl"];
		string cosmosAuthorizationKey = config["CosmosAuthorizationKey"];

		this.cosmosClient = new CosmosClient(cosmosEndpointUri, cosmosAuthorizationKey, new CosmosClientOptions
		{
			Serializer = new CosmosSystemTextJsonSerializer()
		});
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			// Dispose managed resources
#pragma warning disable CA1031 // Do not catch general exception types
			try { cosmosClient?.Dispose(); cosmosClient = null!; } catch { }
#pragma warning restore CA1031 // Do not catch general exception types
		}
		// Dispose unmanaged resources
	}

	private async Task<Container> GetCosmosContainerAsync()
	{
		if (this.cosmosContainerCache == null)
		{
			var dbResponse = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId).ConfigureAwait(false);
			var containerResponse = await dbResponse.Database.CreateContainerIfNotExistsAsync(containerId, "/_partitionKey").ConfigureAwait(false);
			this.cosmosContainerCache = containerResponse.Container;
		}

		return this.cosmosContainerCache;
	}

	public async Task<PodcastEpisode?> GetPodcastEpisodeAsync(string podcastId)
	{
		try
		{
			var cosmosContainer = await GetCosmosContainerAsync().ConfigureAwait(false);
			var itemResponse = await cosmosContainer.ReadItemAsync<PodcastEpisode>(podcastId, PartitionKey.None).ConfigureAwait(false);
			PodcastEpisode result = itemResponse.Resource;
			return result;
		}
		catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
	}

	public async Task CreateNewEpisodeAsync(PodcastEpisode episode)
	{
		var cosmosContainer = await GetCosmosContainerAsync().ConfigureAwait(false);
		_ = await cosmosContainer.CreateItemAsync(episode, PartitionKey.None).ConfigureAwait(false);
	}

	public async Task UpdateExistingEpisodeAsync(PodcastEpisode episode)
	{
		var cosmosContainer = await GetCosmosContainerAsync().ConfigureAwait(false);
		_ = await cosmosContainer.UpsertItemAsync(episode, PartitionKey.None).ConfigureAwait(false);
	}

	public async IAsyncEnumerable<PodcastEpisode> GetEpisodesToFix()
	{
		var cosmosContainer = await GetCosmosContainerAsync().ConfigureAwait(false);
		using var iterator = cosmosContainer.GetItemQueryIterator<PodcastEpisode>("SELECT * FROM PodcastEpisodes c WHERE IS_NULL(c.FileLength)");

		while (iterator.HasMoreResults)
		{
			var response = await iterator.ReadNextAsync().ConfigureAwait(false);
			foreach (var item in response.Resource)
			{
				yield return item;
			}
		}
	}
}