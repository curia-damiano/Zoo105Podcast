using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using NAudio.Wave;
using NLayer.NAudioSupport;
using Zoo105Podcast.AzureBlob;
using Zoo105Podcast.AzureQueue;
using Zoo105Podcast.Cosmos;

namespace Zoo105Podcast
{
	public static class FunctionDownload2Blob
	{
		[FunctionName("Download2Blob")]
#pragma warning disable CA1506 // Avoid excessive class coupling
		public static async Task Run([QueueTrigger("podcast2download", Connection = "AzureWebJobsStorage")]
#pragma warning restore CA1506 // Avoid excessive class coupling
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
			TimeSpan duration;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				CloudBlobContainer cloudBlobContainer = AzureBlobHelper.GetBlobContainer(config);
				if (!await AzureBlobHelper.CheckIfFileIsAlreadyStoredAsync(cloudBlobContainer, episode2download.DateUtc, episode2download.FileName).ConfigureAwait(false))
				{
					using HttpClient httpClient = new HttpClient();

					Uri afterRedirectUri;
					using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, episode2download.CompleteUri))
					{
						using HttpResponseMessage httpResponse = await httpClient.SendAsync(request).ConfigureAwait(false);

						if (httpResponse.IsSuccessStatusCode)
							afterRedirectUri = episode2download.CompleteUri;
						else if ((int)httpResponse.StatusCode >= 300 && (int)httpResponse.StatusCode <= 399)
							// Follow the redirects manually, because they don't work in .NET Core anymore
							afterRedirectUri = httpResponse.Headers.Location;
						else
							throw new MyApplicationException($"Error downloading url: '{episode2download.CompleteUri}'.");
					}

					using (HttpResponseMessage httpResponse = await httpClient.GetAsync(afterRedirectUri).ConfigureAwait(false))
					{
						// Download the file
						using Stream stream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);

						await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
						_ = memoryStream.Seek(0, SeekOrigin.Begin);

						// Save the file to the blob
						fileSize = await AzureBlobHelper.StoreFileAsync(cloudBlobContainer, episode2download.DateUtc, episode2download.FileName, memoryStream).ConfigureAwait(false);
					}
				}
				else
				{
					// Get the file size of the blob
					fileSize = await AzureBlobHelper.GetBlobSizeAsync(cloudBlobContainer, episode2download.DateUtc, episode2download.FileName).ConfigureAwait(false);
					await AzureBlobHelper.GetBlobContentAsync(cloudBlobContainer, episode2download.DateUtc, episode2download.FileName, memoryStream).ConfigureAwait(false);
				}

				_ = memoryStream.Seek(0, SeekOrigin.Begin);
				using var mp3reader = new Mp3FileReaderBase(memoryStream, waveFormat => new Mp3FrameDecompressor(waveFormat));
				duration = mp3reader.TotalTime;
			}

			// After downloading the file, check the file size and the duration in Cosmos, and if wrong, update it
			using CosmosHelper cosmosHelper = new CosmosHelper(config);
			PodcastEpisode episode = await cosmosHelper.GetPodcastEpisodeAsync(episode2download.Id).ConfigureAwait(false);
			if (episode == null)
				throw new MyApplicationException($"Episode '{episode2download.Id}' not found in storage.");
			if (episode.FileLength == fileSize && episode.Duration == duration)
				return;

			episode.FileLength = fileSize;
			episode.Duration = duration;
			await cosmosHelper.UpdateExistingEpisodeAsync(episode).ConfigureAwait(false);
		}
	}
}