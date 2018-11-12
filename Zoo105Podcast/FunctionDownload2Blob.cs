using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Zoo105Podcast.AzureBlob;
using Zoo105Podcast.AzureQueue;
using Zoo105Podcast.CosmosDB;

namespace Zoo105Podcast
{
	public static class FunctionDownload2Blob
	{
		[FunctionName("Download2Blob")]
		public static async Task Run([QueueTrigger("podcast2download", Connection = "AzureWebJobsStorage")]
			string myQueueItem, ILogger logger, Microsoft.Azure.WebJobs.ExecutionContext context)
		{
			logger.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

			var config = new ConfigurationBuilder()
				.SetBasePath(context.FunctionAppDirectory)
				.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build();

			// Change to a culture that has the correct date and time separators
			Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("it-IT");

			Podcast2Download episode2download = AzureQueueHelper.DeserializeItem(myQueueItem);
			long fileSize;

			CloudBlobContainer cloudBlobContainer = AzureBlobHelper.GetBlobContainer(config);
			if (!await AzureBlobHelper.CheckIfFileIsAlreadyStoredAsync(cloudBlobContainer, episode2download.DateUtc, episode2download.FileName))
			{
				using (HttpClient httpClient = new HttpClient())
				{
					// Download the file
					using (HttpResponseMessage httpResponse = await httpClient.GetAsync(episode2download.CompleteUri))
					{
						using (Stream stream = await httpResponse.Content.ReadAsStreamAsync())
						{
							// Save the file to the blob
							fileSize = await AzureBlobHelper.StoreFileAsync(cloudBlobContainer, episode2download.DateUtc, episode2download.FileName, stream);
						}
					}
				}
			}
			else
				// Get the file size of the blob
				fileSize = await AzureBlobHelper.GetBlobSizeAsync(cloudBlobContainer, episode2download.DateUtc, episode2download.FileName);

			// After downloading the file, check the file size in CosmosDB, and if wrong, update it
			using (DocumentClient cosmosDBClient = await CosmosDBHelper.GetCosmosDBClientAsync(config))
			{
				PodcastEpisode episode = await CosmosDBHelper.GetPodcastEpisodeAsync(cosmosDBClient, episode2download.Id);
				if (episode.FileLength == fileSize)
					return;

				episode.FileLength = fileSize;
				await CosmosDBHelper.UpdateExistingEpisodeAsync(cosmosDBClient, episode);
			}
		}
	}
}