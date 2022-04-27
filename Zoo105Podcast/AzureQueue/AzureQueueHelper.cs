using System;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Zoo105Podcast.AzureQueue;

public static class AzureQueueHelper
{
	private const string queueName = "podcast2download";

	public static async Task<CloudQueue> GetAzureQueueAsync(IConfiguration config)
	{
		if (config == null) throw new ArgumentNullException(nameof(config));

		CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config["AzureWebJobsStorage"]);
		CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
		CloudQueue queue = queueClient.GetQueueReference(queueName);
		_ = await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
		return queue;
	}

	public static Task EnqueueItemAsync(CloudQueue queue, Podcast2Download episode)
	{
		if (queue == null) throw new ArgumentNullException(nameof(queue));

		string serializedObj = JsonSerializer.Serialize(episode);
		CloudQueueMessage message = new CloudQueueMessage(serializedObj);
		return queue.AddMessageAsync(message);
	}

	public static Podcast2Download DeserializeItem(string serialized)
	{
		if (string.IsNullOrEmpty(serialized)) throw new ArgumentNullException(nameof(serialized));

		Podcast2Download result = JsonSerializer.Deserialize<Podcast2Download>(serialized)!;
		return result;
	}
}