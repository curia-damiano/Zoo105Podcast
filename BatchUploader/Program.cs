using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using NAudio.Wave;
using NLayer.NAudioSupport;
using Zoo105Podcast.AzureBlob;
using Zoo105Podcast.Cosmos;

namespace BatchUploader;

public static class Program
{
	private const string folder = @"C:\Users\dacuri\OneDrive - Microsoft\Desktop\Zoo105";

	public static async Task Main()
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
			string xDate = xName[4..(4 + 8)];//.Substring(4).Substring(0, 8);
			string yDate = yName[4..(4 + 8)];//.Substring(4).Substring(0, 8);
			DateTime dateX = DateTime.ParseExact(xDate, "ddMMyyyy", CultureInfo.InvariantCulture).ToUniversalTime();
			DateTime dateY = DateTime.ParseExact(yDate, "ddMMyyyy", CultureInfo.InvariantCulture).ToUniversalTime();
			return dateX > dateY ? 1 : -1;
		});
		Array.Sort(fileNames, comparison);

		using CosmosHelper cosmosHelper = new CosmosHelper(config);
		CloudBlobContainer cloudBlobContainer = AzureBlobHelper.GetBlobContainer(config);

		foreach (string fileName in fileNames)
		{
			Console.WriteLine($"Uploading: {fileName}");

			string xName = Path.GetFileName(fileName);
			string xDate = xName[4..(4 + 8)];//.Substring(4).Substring(0, 8);
			DateTime dateX = DateTime.ParseExact(xDate, "ddMMyyyy", CultureInfo.InvariantCulture).AddHours(2).ToUniversalTime();

			TimeSpan duration;
			using (var mp3reader = new Mp3FileReaderBase(fileName, waveFormat => new Mp3FrameDecompressor(waveFormat)))
			{
				duration = mp3reader.TotalTime;
			}

			PodcastEpisode episode = new PodcastEpisode
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
			await cosmosHelper.CreateNewEpisodeAsync(episode).ConfigureAwait(true);

			using (Stream stream = File.OpenRead(fileName))
			{
				_ = await AzureBlobHelper.StoreFileAsync(cloudBlobContainer, episode.DateUtc, episode.FileName, stream).ConfigureAwait(true);
			}

			Console.WriteLine($"Uploaded: {fileName}");
		}
	}
}