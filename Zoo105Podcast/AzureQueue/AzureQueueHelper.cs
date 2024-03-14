using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;

namespace Zoo105Podcast.AzureQueue;

public class AzureQueueHelper(
	QueueServiceClient queueServiceClient)
{
	private const string QueueName = "podcast2download";
	private QueueClient? _queueClient;

	public Task InitializeQueueClientAsync()
	{
		this._queueClient = queueServiceClient.GetQueueClient(QueueName);
		return this._queueClient.CreateIfNotExistsAsync();
	}

	public Task EnqueueItemAsync(Podcast2Download episode)
	{
		ArgumentNullException.ThrowIfNull(this._queueClient);

		string serializedObj = JsonSerializer.Serialize(episode);
		var bytes = Encoding.UTF8.GetBytes(serializedObj);
		return this._queueClient.SendMessageAsync(Convert.ToBase64String(bytes));
	}

	public static Podcast2Download DeserializeItem(string serialized)
	{
		ArgumentNullException.ThrowIfNull(serialized);

		Podcast2Download result = JsonSerializer.Deserialize<Podcast2Download>(serialized)!;
		return result;
	}
}