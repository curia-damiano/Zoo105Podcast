using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Zoo105Podcast.AzureQueue;
using Zoo105Podcast.Cosmos;

namespace Zoo105Podcast;

public static class FunctionTimerFixErrors
{
	[FunctionName("TimerFixErrors")]
	public static async Task Run([TimerTrigger("0 0 */2 * * *"
		#if DEBUG
			, RunOnStartup= true
		#endif
		)] TimerInfo myTimer, ILogger logger, Microsoft.Azure.WebJobs.ExecutionContext context)
	{
		logger.LogInformation("C# HTTP trigger function processed a request.");

		ArgumentNullException.ThrowIfNull(context);

		var config = new ConfigurationBuilder()
			.SetBasePath(context.FunctionAppDirectory)
			.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
			.AddEnvironmentVariables()
			.Build();

		// Change to a culture that has the correct date and time separators
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("it-IT");

		using CosmosHelper cosmosHelper = new(config);
		CloudQueue queue = await AzureQueueHelper.GetAzureQueueAsync(config).ConfigureAwait(false);

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
			await AzureQueueHelper.EnqueueItemAsync(queue, episode2download).ConfigureAwait(false);
		}
	}
}