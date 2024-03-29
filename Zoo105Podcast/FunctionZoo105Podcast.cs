﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zoo105Podcast.AzureQueue;
using Zoo105Podcast.Cosmos;
using Zoo105Podcast.PodcastRssGenerator4DotNet;

namespace Zoo105Podcast;

public class FunctionZoo105Podcast(
	IHost host,
	ILogger<FunctionZoo105Podcast> logger,
	IConfiguration configuration)
{
	private static readonly (int PodcastSourceId, string ShowName, string TitleFormat, string DescriptionFormat, Uri ImageUrl)[] PodcastSources = {
		// old photo: https://w7.pngwing.com/pngs/665/126/png-transparent-italy-radio-105-network-radio-deejay-radiofonia-comedian-zoo-playful-purple-violet-magenta.png
		(0, "zoo",         "Lo Zoo di 105 del {0}", "Puntata completa dello Zoo di 105 del {0}", new Uri("https://www.105.net/upload/MicrosoftTeams-image_(4)-1683355472122.jpg")),
		//(1, "105polaroyd", "105 Polaroyd del {0}",  "Puntata completa di 105 Polaroyd del {0}",  new Uri("https://zoo.105.net/resizer/-1/-1/true/1540546271185.png--.png?1540546271000"))
	};

	[Function("Zoo105Podcast")]
	public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
	{
		logger.LogInformation("C# HTTP trigger function processed a request.");

		// Change to a culture that has the correct date and time separators
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("it-IT");

		// Podcast code taken initially from https://github.com/keyvan/PodcastRssGenerator4DotNet/blob/master/PodcastRssGenerator4DotNet/PodcastRssGenerator4DotNet.Test/Default.aspx.cs
		// How apply encoding to XmlWriter: https://stackoverflow.com/questions/427725/how-to-put-an-encoding-attribute-to-xml-other-that-utf-16-with-xmlwriter
		using MemoryStream memoryStream = new();
		XmlWriterSettings settings = new()
		{
			Encoding = Encoding.UTF8
		};

		using (XmlWriter writer = XmlWriter.Create(memoryStream, settings))
		{
			RssGenerator generator = await GetGeneratorAsync().ConfigureAwait(false);
			generator.Generate(writer);
		}
		string xmlString = settings.Encoding.GetString(memoryStream.ToArray());

		var response = req.CreateResponse(HttpStatusCode.OK);
		response.Headers.Add("Content-Type", "application/rss+xml; charset=utf-8");
		await response.WriteStringAsync(xmlString, Encoding.UTF8).ConfigureAwait(false);
		return response;
	}

	private async Task<RssGenerator> GetGeneratorAsync()
	{
		RssGenerator generator = new()
		{
			Language = "it-IT",
			PodcastUrl = new Uri("http://zoo105podcast.azurewebsites.net/api/Zoo105Podcast"),
			Title = "Lo Zoo di 105 - Full Episodes",
			HomePageUrl = new Uri("http://zoo105podcast.azurewebsites.net"),
			Description = "Podcast non ufficiale dello Zoo di 105",
			AuthorName = "Curia Damiano",
			Copyright = $"Copyright {DateTime.UtcNow.Year} Curia Damiano. All rights reserved.",
			iTunesCategory = "News & Politics",
			iTunesSubCategory = "Entertainment News",
			IsExplicit = true,
			OwnerName = "Curia Damiano",
			OwnerEmail = "damiano.curia@gmail.com",
			ImageUrl = new Uri("https://is3-ssl.mzstatic.com/image/thumb/Music71/v4/9c/0c/30/9c0c3072-3d42-e609-cbc8-e822c9f910fa/source/170x170bb.jpg")
		};

		List<Episode> episodes = [];
		foreach (PodcastEpisode podcast in await GetPodcastsAsync().ConfigureAwait(false))
		{
			var podcastSource = PodcastSources.Single(ps => ps.ShowName == podcast.ShowName);

			episodes.Add(new Episode()
			{
				Title = string.Format(podcastSource.TitleFormat, podcast.DateUtc.ToString("dd/MM/yyyy")),
				FileDownloadUrl = podcast.CompleteUri,
				Description = string.Format(podcastSource.DescriptionFormat, podcast.DateUtc.ToString("dd/MM/yyyy")),
				ImageUrl = podcastSource.ImageUrl,
				FileLength = podcast.FileLength,
				Duration = podcast.Duration,
				PublicationDate = podcast.DateUtc
			});
		}
		generator.Episodes = episodes;

		return generator;
	}

	private const string yyyyMMdd = "yyyyMMdd";
	private async Task<List<PodcastEpisode>> GetPodcastsAsync()
	{
		int maxNumberEpisodesToReturn = int.Parse(configuration["MaxNumberEpisodesToReturn"]!);
		int maxNumberDaysWithoutPodcast = int.Parse(configuration["MaxNumberDaysWithoutPodcast"]!);
		DateTime currDate = DateTime.UtcNow.Date;
		int numDaysWithoutPodcast = 0;

		using HttpClient httpClient = new();
		httpClient.Timeout = new TimeSpan(0, 0, 5);

		using CosmosHelper cosmosHelper = new(configuration);
		await cosmosHelper.InitializeCosmosContainerAsync().ConfigureAwait(false);

		AzureQueueHelper azureQueueHelper = host.Services.GetService<AzureQueueHelper>()!;
		await azureQueueHelper.InitializeQueueClientAsync().ConfigureAwait(false);

		List<PodcastEpisode> result = [];

		// We continue to search episodes in the past, and we stop when any of the following cases happen:
		// - our list of episodes to return is long enough (==maxNumberEpisodesToReturn)
		// - the number of days since the last returned episode is too big (>maxNumberDaysWithoutPodcast)
		while (result.Count < maxNumberEpisodesToReturn && numDaysWithoutPodcast <= maxNumberDaysWithoutPodcast)
		{
			bool foundAtLeastOnePodcast = false;
			foreach (var podcastSource in PodcastSources)
			{
				string podcastId = $"{podcastSource.ShowName}_{currDate.ToString(yyyyMMdd)}";

				// Check if the podcast info is already in Cosmos
				PodcastEpisode? episode = await cosmosHelper.GetPodcastEpisodeAsync(podcastId).ConfigureAwait(false);

				// If no episode found for this key, try to check if it is available to download
				if (episode == null)
				{
					string fileName = $"{GetDayOfWeek(currDate)}_{currDate:ddMMyyyy}_{podcastSource.ShowName}.mp3";
					Uri? completeUri = await SearchPodcastOnline(httpClient, fileName, currDate).ConfigureAwait(false);
					if (completeUri != null)
					{
						// Note: the order of the next two storages is important:
						// - if I would save first in Cosmos, then in case of error when enqueuing,
						//   on the next call of the function I would not enter her anymore (the item will be
						//   is found in Cosmos) but the download would be lost.
						var episode2download = new Podcast2Download
						{
							Id = podcastId,
							DateUtc = currDate,
							FileName = fileName,
							CompleteUri = completeUri
						};
						await azureQueueHelper.EnqueueItemAsync(episode2download).ConfigureAwait(false);

						episode = new PodcastEpisode
						{
							Id = podcastId,
							ShowName = podcastSource.ShowName,
							DateUtc = currDate,
							FileName = fileName,
							CompleteUri = completeUri,
							FileLength = null, // The real value will be fixed in DB after downloading the file
							Duration = null    // The real value will be fixed in DB after downloading the file
						};
						await cosmosHelper.CreateNewEpisodeAsync(episode).ConfigureAwait(false);
					}
				}

				if (episode != null)
				{
					// If an episode has been found, add it to the result collection and reset the number of days without episodes
					result.Add(episode);
					foundAtLeastOnePodcast = true;
					numDaysWithoutPodcast = 0;
				}
			}

			if (!foundAtLeastOnePodcast)
			{
				// If still no found any episode, increase numDaysWithoutPodcast
				numDaysWithoutPodcast++;
			}
			currDate = currDate.AddDays(-1);
		}

		return result;
	}

	private static async Task<Uri?> SearchPodcastOnline(HttpClient httpClient, string fileName, DateTime uploadDate)
	{
		while (uploadDate <= DateTime.Today)
		{
			//Uri completeUri = new Uri($"http://www.105.net/upload/uploadedContent/repliche/zoo/{fileName}");
			Uri completeUri = new($"https://podcast.mediaset.net/repliche//{uploadDate:yyyy}/{uploadDate.Month}/{uploadDate.Day}/{fileName}");
			using HttpRequestMessage request = new(HttpMethod.Head, completeUri);
			try
			{
				using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
				Console.WriteLine(response.Headers.ToString());
				if (response.IsSuccessStatusCode || ((int)response.StatusCode >= 300 && (int)response.StatusCode <= 399))
				{
					// We have found the podcast at uploadDate, so we return the Url
					return completeUri;
				}
			}
			catch (HttpRequestException)
			{
				// This exception can happen when the file is not available (the server returns 404 in a strange way)
			}
			catch (TaskCanceledException)
			{
				// This exception can happen when the file is not available (the server takes long time to reply)
			}

			// Try with next day
			uploadDate = uploadDate.AddDays(1);
		}

		// The tentative uploadDate would be in the future, so we return null
		return null;
	}

	private static string GetDayOfWeek(DateTime date)
	{
		return date.DayOfWeek switch
		{
			DayOfWeek.Monday	=> "lun",
			DayOfWeek.Tuesday	=> "mar",
			DayOfWeek.Wednesday	=> "mer",
			DayOfWeek.Thursday	=> "gio",
			DayOfWeek.Friday	=> "ven",
			DayOfWeek.Saturday	=> "sab",
			DayOfWeek.Sunday	=> "dom",
			_					=> throw new NotImplementedException($"getDayOfWeek: date=={date}"),
		};
	}
}