﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NLayer.NAudioSupport;
using Zoo105Podcast.AzureBlob;
using Zoo105Podcast.AzureQueue;
using Zoo105Podcast.Cosmos;

namespace Zoo105Podcast;

public class FunctionDownload2Blob(
	IHost host,
	ILogger<FunctionZoo105Podcast> logger,
	IConfiguration configuration)
{
	[Function("Download2Blob")]
#pragma warning disable CA1506 // Avoid excessive class coupling
	public async Task Run([QueueTrigger("podcast2download", Connection = "AzureWebJobsStorage")]
#pragma warning restore CA1506 // Avoid excessive class coupling
		string myQueueItem)
	{
		logger.LogInformation("C# Queue trigger function processed: {MyQueueItem}", myQueueItem);

		// Change to a culture that has the correct date and time separators
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("it-IT");

		Podcast2Download episode2download = AzureQueueHelper.DeserializeItem(myQueueItem);

		long fileSize;
		TimeSpan duration;
		using (MemoryStream memoryStream = new())
		{
			AzureBlobHelper azureBlobHelper = host.Services.GetService<AzureBlobHelper>()!;
			await azureBlobHelper.InitializeBlobContainerClientAsync().ConfigureAwait(false);

			if (!await azureBlobHelper.CheckIfFileIsAlreadyStoredAsync(episode2download.DateUtc, episode2download.FileName).ConfigureAwait(false))
			{
				using HttpClient httpClient = new();

				Uri afterRedirectUri;
				using (HttpRequestMessage request = new(HttpMethod.Head, episode2download.CompleteUri))
				{
					using HttpResponseMessage httpResponse = await httpClient.SendAsync(request).ConfigureAwait(false);

					if (httpResponse.IsSuccessStatusCode)
						afterRedirectUri = episode2download.CompleteUri;
					else if ((int)httpResponse.StatusCode >= 300 && (int)httpResponse.StatusCode <= 399)
						// Follow the redirects manually, because they don't work in .NET Core anymore
						afterRedirectUri = httpResponse.Headers.Location ?? throw new MyApplicationException("Location header missing");
					else
						throw new MyApplicationException($"Error downloading url: '{episode2download.CompleteUri}'.");
				}

				using (HttpResponseMessage httpResponse = await httpClient.GetAsync(afterRedirectUri).ConfigureAwait(false))
				{
					// Download the file
					Stream stream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
					await using (stream.ConfigureAwait(false))
					{
						await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
						_ = memoryStream.Seek(0, SeekOrigin.Begin);

						// Save the file to the blob
						fileSize = await azureBlobHelper.StoreFileAsync(episode2download.DateUtc, episode2download.FileName, memoryStream).ConfigureAwait(false);
					}
				}
			}
			else
			{
				// Get the file size of the blob
				fileSize = await azureBlobHelper.GetBlobSizeAsync(episode2download.DateUtc, episode2download.FileName).ConfigureAwait(false);
				await azureBlobHelper.GetBlobContentAsync(episode2download.DateUtc, episode2download.FileName, memoryStream).ConfigureAwait(false);
			}

			_ = memoryStream.Seek(0, SeekOrigin.Begin);
			Mp3FileReaderBase mp3Reader = new(memoryStream, waveFormat => new Mp3FrameDecompressor(waveFormat));
			await using (mp3Reader.ConfigureAwait(false))
			{
				duration = mp3Reader.TotalTime;
			}
		}

		// After downloading the file, check the file size and the duration in Cosmos, and if wrong, update it
		using CosmosHelper cosmosHelper = new(configuration);
		await cosmosHelper.InitializeCosmosContainerAsync().ConfigureAwait(false);

		PodcastEpisode? episode = await cosmosHelper.GetPodcastEpisodeAsync(episode2download.Id).ConfigureAwait(false);
		if (episode == null)
			throw new MyApplicationException($"Episode '{episode2download.Id}' not found in storage.");
		if (episode.FileLength == fileSize && episode.Duration == duration)
			return;

		episode.FileLength = fileSize;
		episode.Duration = duration;
		await cosmosHelper.UpdateExistingEpisodeAsync(episode).ConfigureAwait(false);
	}
}