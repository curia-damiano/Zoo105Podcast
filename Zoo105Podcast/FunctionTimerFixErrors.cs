using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zoo105Podcast.AzureQueue;
using Zoo105Podcast.Cosmos;

namespace Zoo105Podcast;

public class FunctionTimerFixErrors(
	IHost host,
	ILogger<FunctionZoo105Podcast> logger,
	IConfiguration configuration)
{
	[Function("TimerFixErrors")]
	public async Task Run([TimerTrigger("0 0 */2 * * *"
		#if DEBUG
			, RunOnStartup= true
		#endif
		)] TimerInfo myTimer)
	{
		logger.LogInformation("C# HTTP trigger function processed a request.");

		// Change to a culture that has the correct date and time separators
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("it-IT");

		using CosmosHelper cosmosHelper = new(configuration);
		await cosmosHelper.InitializeCosmosContainerAsync().ConfigureAwait(false);

		AzureQueueHelper azureQueueHelper = host.Services.GetService<AzureQueueHelper>()!;
		await azureQueueHelper.InitializeQueueClientAsync().ConfigureAwait(false);

		await foreach (var podcastEpisode in cosmosHelper.GetEpisodesToFix())
		{
			Console.WriteLine(podcastEpisode.Id);

			var episode2download = new Podcast2Download
			{
				Id = podcastEpisode.Id,
				DateUtc = podcastEpisode.DateUtc,
				FileName = podcastEpisode.FileName,
				CompleteUri = podcastEpisode.CompleteUri
			};
			await azureQueueHelper.EnqueueItemAsync(episode2download).ConfigureAwait(false);
		}
	}
}