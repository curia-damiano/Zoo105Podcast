using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Zoo105Podcast.AzureBlob;
using Zoo105Podcast.CosmosDB;

namespace BatchUploader
{
	class Program
	{
		private const string folder = @"C:\Users\dacuri\Desktop\Zoo105";

#pragma warning disable CA1801 // Review unused parameters
		static void Main(string[] args)
#pragma warning restore CA1801 // Review unused parameters
		{
			IConfiguration config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build();

			// Change to a culture that has the correct date and time separators
			Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");

			// Load file names
			string[] fileNames = Directory.GetFiles(folder, "*.mp3");

			// Sort array by dates
			Comparison<string> comparison = new Comparison<string>((x, y) =>
			{
				string xName = Path.GetFileName(x);
				string yName = Path.GetFileName(y);
				string xDate = xName.Substring(4).Substring(0, 8);
				string yDate = yName.Substring(4).Substring(0, 8);
				DateTime dateX = DateTime.ParseExact(xDate, "ddMMyyyy", CultureInfo.InvariantCulture).ToUniversalTime();
				DateTime dateY = DateTime.ParseExact(yDate, "ddMMyyyy", CultureInfo.InvariantCulture).ToUniversalTime();
				return dateX > dateY ? 1 : -1;
			});
			Array.Sort(fileNames, comparison);

			using (DocumentClient cosmosDBClient = CosmosDBHelper.GetCosmosDBClientAsync(config).Result)
			{
				CloudBlobContainer cloudBlobContainer = AzureBlobHelper.GetBlobContainer(config);

				foreach (string fileName in fileNames)
				{
					Console.WriteLine($"Uploading: {fileName}");

					string xName = Path.GetFileName(fileName);
					string xDate = xName.Substring(4).Substring(0, 8);
					DateTime dateX = DateTime.ParseExact(xDate, "ddMMyyyy", CultureInfo.InvariantCulture).AddHours(2).ToUniversalTime();

					PodcastEpisode episode = new PodcastEpisode
					{
						Id = dateX.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
						DateUtc = dateX,
						FileName = xName,
						CompleteUri = new Uri($"http://www.105.net/upload/uploadedContent/repliche/zoo/{xName}"),
						FileLength = new FileInfo(fileName).Length
					};
					CosmosDBHelper.CreateNewEpisodeAsync(cosmosDBClient, episode).Wait();

					using (Stream stream = File.OpenRead(fileName))
					{
						AzureBlobHelper.StoreFileAsync(cloudBlobContainer, episode.DateUtc, episode.FileName, stream).Wait();
					}

					Console.WriteLine($"Uploaded: {fileName}");
				}
			}
		}
	}
}