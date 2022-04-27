using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zoo105Podcast.CosmosDB;
using Zoo105Podcast.PodcastRssGenerator4DotNet;

namespace Zoo105Podcast
{
	public static class FunctionZoo105Podcast
	{
		[FunctionName("Zoo105Podcast")]
#pragma warning disable CA1801 // Review unused parameters
		public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
			HttpRequest req, ILogger logger, Microsoft.Azure.WebJobs.ExecutionContext context)
#pragma warning restore CA1801 // Review unused parameters
		{
			logger.LogInformation("C# HTTP trigger function processed a request.");

			var config = new ConfigurationBuilder()
				.SetBasePath(context.FunctionAppDirectory)
				.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build();

			// Change to a culture that has the correct date and time separators
			Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("it-IT");

			// Podcast code taken initiallly from https://github.com/keyvan/PodcastRssGenerator4DotNet/blob/master/PodcastRssGenerator4DotNet/PodcastRssGenerator4DotNet.Test/Default.aspx.cs
			// How apply encoding to XmlWriter: https://stackoverflow.com/questions/427725/how-to-put-an-encoding-attribute-to-xml-other-that-utf-16-with-xmlwriter
			using (MemoryStream memoryStream = new MemoryStream())
			{
				XmlWriterSettings settings = new XmlWriterSettings
				{
					Encoding = Encoding.UTF8
				};

				using (XmlWriter writer = XmlWriter.Create(memoryStream, settings))
				{
					RssGenerator generator = await GetGeneratorAsync(config);
					generator.Generate(writer);
				}

				string xmlString = settings.Encoding.GetString(memoryStream.ToArray());

				return new ContentResult
				{
					Content = xmlString.ToString(),
					ContentType = "application/rss+xml"
				};
			}
		}

		private static async Task<RssGenerator> GetGeneratorAsync(IConfiguration config)
		{
			RssGenerator generator = new RssGenerator
			{
				Language = "it-IT",
				PodcastUrl = new Uri("https://functionapp20180423052306.azurewebsites.net/api/Zoo105Podcast"),
				Title = "Lo Zoo di 105 - Full Episodes",
				HomePageUrl = new Uri("https://functionapp20180423052306.azurewebsites.net/"),
				Description = "Podcast non ufficiale dello Zoo di 105",
				AuthorName = "Curia Damiano",
				Copyright = $"Copyright {DateTime.UtcNow.Year} Curia Damiano. All rights reserved.",
				iTunesCategory = "News & Politics",
				iTunesSubCategory = string.Empty,
				IsExplicit = true,
				OwnerName = "Curia Damiano",
				OwnerEmail = "damiano.curia@gmail.com",
				ImageUrl = new Uri("https://is3-ssl.mzstatic.com/image/thumb/Music71/v4/9c/0c/30/9c0c3072-3d42-e609-cbc8-e822c9f910fa/source/170x170bb.jpg")
			};

			List<Episode> episodes = new List<Episode>();
			foreach (PodcastEpisode podcast in await GetPodcastsAsync(config))
			{
				episodes.Add(new Episode()
				{
					Title = $"Puntata del {podcast.DateUtc.ToString("dd/MM/yyyy")}",
					FileDownloadUrl = podcast.CompleteUri,
					Description = $"Puntata completa dello Zoo di 105 del {podcast.DateUtc.ToString("dd/MM/yyyy")}",
					FileLength = podcast.FileLength,
					PublicationDate = podcast.DateUtc
				});
			}
			generator.Episodes = episodes;

			return generator;
		}

		private static async Task<List<PodcastEpisode>> GetPodcastsAsync(IConfiguration config)
		{
			int maxNumberEpisodesToReturn = int.Parse(config["MaxNumberEpisodesToReturn"]);
			int maxNumberDaysWithoutPodcast = int.Parse(config["MaxNumberDaysWithoutPodcast"]);
			DateTime currDate = DateTime.UtcNow.Date;
			int numDaysWithoutPodcast = 0;

			using (HttpClient httpClient = new HttpClient())
			{
				using (DocumentClient cosmosDBClient = await CosmosDBHelper.GetCosmosDBClientAsync(config))
				{
					List<PodcastEpisode> result = new List<PodcastEpisode>();

					// We continue to search episodes in the past and we stop when any of the following cases happen:
					// - our list of episodes to return is long enough (==maxNumberEpisodesToReturn)
					// - the number of  days since the last returned episode is too big (>maxNumberDaysWithoutPodcast)
					while (result.Count < maxNumberEpisodesToReturn &&
						numDaysWithoutPodcast <= maxNumberEpisodesToReturn)
					{
						string podcastId = currDate.ToString("yyyyMMdd");

						// Check if the podcast info is already in CosmosDB
						PodcastEpisode episode = await CosmosDBHelper.GetPodcastEpisodeAsync(cosmosDBClient, podcastId);

						// If no episode found for this key, try to check if it is available to download
						if (episode == null)
						{
							string fileName = $"{GetDayOfWeek(currDate)}_{currDate.ToString("ddMMyyyy")}_zoo.mp3";
							Uri completeUri = new Uri($"http://www.105.net/upload/uploadedContent/repliche/zoo/{fileName}");
							using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, completeUri))
							{
								try
								{
									using (HttpResponseMessage response = await httpClient.SendAsync(request))
									{
										Console.WriteLine(response.Headers.ToString());
										if (response.IsSuccessStatusCode)
										{
											episode = new PodcastEpisode
											{
												Id = podcastId,
												DateUtc = currDate,
												FileName = fileName,
												CompleteUri = completeUri,
												FileLength = 1 // The real value will be fixed in DB after downloading the file
											};
											await CosmosDBHelper.CreateNewEpisodeAsync(cosmosDBClient, episode);
										}
									}
								}
								catch (HttpRequestException)
								{
									// This exception can happen when the file is not available (the server returns 404 in a strange way)
								}
							}
						}

						if (episode != null)
						{
							// If an episode has been found, add it to the result collection and reset the number of days without episodes
							result.Add(episode);
							numDaysWithoutPodcast = 0;
						}
						else
							// If still no found any episode, increase numDaysWithoutPodcast
							numDaysWithoutPodcast++;

						currDate = currDate.AddDays(-1);
					}

					return result;
				}
			}
		}

		private static string GetDayOfWeek(DateTime date)
		{
			switch (date.DayOfWeek)
			{
				case DayOfWeek.Monday: return "lun";
				case DayOfWeek.Tuesday: return "mar";
				case DayOfWeek.Wednesday: return "mer";
				case DayOfWeek.Thursday: return "gio";
				case DayOfWeek.Friday: return "ven";
				case DayOfWeek.Saturday: return "sab";
				case DayOfWeek.Sunday: return "dom";
			}
			throw new NotImplementedException($"getDayOfWeek: date=={date}");
		}
	}
}