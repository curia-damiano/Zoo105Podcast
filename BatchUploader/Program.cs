using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NAudio.Wave;
using NLayer.NAudioSupport;
using Zoo105Podcast.AzureBlob;
using Zoo105Podcast.Cosmos;

namespace BatchUploader;

public static class Program
{
	private const string FolderName = @"C:\Users\dacuri\OneDrive - Microsoft\Desktop\Zoo105";

	public static async Task Main()
	{
		var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

		IHost host = Host.CreateDefaultBuilder()
			.ConfigureServices((hostContext, services) =>
			{
				// Services to be instantiated by the Dependency Injection Host
				services.AddSingleton<AzureBlobHelper, AzureBlobHelper>();

				// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/microsoft.extensions.azure-readme
				var configuration = hostContext.Configuration;
				services.AddAzureClients(clientBuilder =>
				{
					clientBuilder.AddBlobServiceClient(configuration["Values:AzureWebJobsStorage"]);
				});
			})
			.ConfigureAppConfiguration(builder =>
			{
				builder
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
					.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
					.AddEnvironmentVariables();
				var configuration = builder.Build();
			})
			.Build();

		// Change to a culture that has the correct date and time separators
		Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");

		// Load file names
		string[] fileNames = Directory.GetFiles(FolderName, "*.mp3");

		// Sort array by dates
		static int Comparison(string x, string y)
		{
			string xName = Path.GetFileName(x);
			string yName = Path.GetFileName(y);
			string xDate = xName[4..(4 + 8)]; //.Substring(4).Substring(0, 8);
			string yDate = yName[4..(4 + 8)]; //.Substring(4).Substring(0, 8);
			DateTime dateX = DateTime.ParseExact(xDate, "ddMMyyyy", CultureInfo.InvariantCulture).ToUniversalTime();
			DateTime dateY = DateTime.ParseExact(yDate, "ddMMyyyy", CultureInfo.InvariantCulture).ToUniversalTime();
			return dateX > dateY ? 1 : -1;
		}
		Array.Sort(fileNames, Comparison);

		IConfiguration configuration = host.Services.GetService<IConfiguration>()!;
		
		using CosmosHelper cosmosHelper = new(configuration);
		await cosmosHelper.InitializeCosmosContainerAsync().ConfigureAwait(false);

		AzureBlobHelper azureBlobHelper = host.Services.GetService<AzureBlobHelper>()!;
		await azureBlobHelper.InitializeBlobContainerClientAsync().ConfigureAwait(false);

		foreach (string fileName in fileNames)
		{
			Console.WriteLine($"Uploading: {fileName}");

			string xName = Path.GetFileName(fileName);
			string xDate = xName[4..(4 + 8)];//.Substring(4).Substring(0, 8);
			DateTime dateX = DateTime.ParseExact(xDate, "ddMMyyyy", CultureInfo.InvariantCulture).AddHours(2).ToUniversalTime();

			TimeSpan duration;
			Mp3FileReaderBase mp3Reader = new(fileName, waveFormat => new Mp3FrameDecompressor(waveFormat));
			await using (mp3Reader.ConfigureAwait(false))
			{
				duration = mp3Reader.TotalTime;
			}

			PodcastEpisode episode = new()
			{
				Id = "zoo_" + dateX.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
				ShowName = "zoo",
				DateUtc = dateX,
				FileName = xName,
				//CompleteUri = new Uri($"http://www.105.net/upload/uploadedContent/repliche/zoo/{xName}"),
				CompleteUri = new Uri($"https://podcast.mediaset.net/repliche//{dateX:yyyy}/{dateX.Month}/{dateX.Day}/{xName}"),
				FileLength = new FileInfo(fileName).Length,
				Duration = duration
			};
			await cosmosHelper.CreateNewEpisodeAsync(episode).ConfigureAwait(false);

			Stream stream = File.OpenRead(fileName);
			await using (stream.ConfigureAwait(false))
			{
				_ = await azureBlobHelper.StoreFileAsync(episode.DateUtc, episode.FileName, stream).ConfigureAwait(false);
			}

			Console.WriteLine($"Uploaded: {fileName}");
		}
	}
}