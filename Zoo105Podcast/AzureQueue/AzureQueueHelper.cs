using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace Zoo105Podcast.AzureQueue
{
	public static class AzureQueueHelper
	{
		private const string queueName = "podcast2download";

		public static async Task<CloudQueue> GetAzureQueueAsync(IConfiguration config)
		{
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config["AzureWebJobsStorage"]);
			CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
			CloudQueue queue = queueClient.GetQueueReference(queueName);
			_ = await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
			return queue;
		}

		public static Task EnqueueItemAsync(CloudQueue queue, Podcast2Download episode)
		{
			string serializedObj = JsonConvert.SerializeObject(episode);
			CloudQueueMessage message = new CloudQueueMessage(serializedObj);
			return queue.AddMessageAsync(message);
		}

		public static Podcast2Download DeserializeItem(string serialized)
		{
			Podcast2Download result = JsonConvert.DeserializeObject<Podcast2Download>(serialized);
			return result;
		}
	}
}