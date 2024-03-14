using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace Zoo105Podcast.Cosmos;

public class CosmosHelper : IDisposable
{
	private const string DatabaseId = "DB_Zoo105Podcast";
	private const string ContainerId = "PodcastEpisodes";

	private CosmosClient _cosmosClient;
	private Container? _cosmosContainer;

	public CosmosHelper(IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);

		string cosmosEndpointUri = configuration["CosmosEndpointUrl"]!;
		string cosmosAuthorizationKey = configuration["CosmosAuthorizationKey"]!;

		this._cosmosClient = new CosmosClient(cosmosEndpointUri, cosmosAuthorizationKey, new CosmosClientOptions
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
			try { this._cosmosClient?.Dispose(); this._cosmosClient = null!; } catch { }
#pragma warning restore CA1031 // Do not catch general exception types
		}
		// Dispose unmanaged resources
	}

	public async Task InitializeCosmosContainerAsync()
	{
		var dbResponse = await this._cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId).ConfigureAwait(false);
		var containerResponse = await dbResponse.Database.CreateContainerIfNotExistsAsync(ContainerId, "/_partitionKey").ConfigureAwait(false);
		this._cosmosContainer = containerResponse.Container;
	}

	public async Task<PodcastEpisode?> GetPodcastEpisodeAsync(string podcastId)
	{
		ArgumentNullException.ThrowIfNull(this._cosmosContainer);

		try
		{
			var itemResponse = await this._cosmosContainer.ReadItemAsync<PodcastEpisode>(podcastId, PartitionKey.None).ConfigureAwait(false);
			PodcastEpisode result = itemResponse.Resource;
			return result;
		}
		catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
	}

	public Task CreateNewEpisodeAsync(PodcastEpisode episode)
	{
		ArgumentNullException.ThrowIfNull(this._cosmosContainer);

		return this._cosmosContainer.CreateItemAsync(episode, PartitionKey.None);
	}

	public Task UpdateExistingEpisodeAsync(PodcastEpisode episode)
	{
		ArgumentNullException.ThrowIfNull(this._cosmosContainer);

		return this._cosmosContainer.UpsertItemAsync(episode, PartitionKey.None);
	}

	public async IAsyncEnumerable<PodcastEpisode> GetEpisodesToFix()
	{
		ArgumentNullException.ThrowIfNull(this._cosmosContainer);

		using var iterator = this._cosmosContainer.GetItemQueryIterator<PodcastEpisode>("SELECT * FROM PodcastEpisodes c WHERE IS_NULL(c.FileLength)");

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