using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Zoo105Podcast.AzureBlob
{
	public static class AzureBlobHelper
	{
		private const string blobContainerName = "zoo105podcast";

		public static CloudBlobContainer GetBlobContainer(IConfiguration config)
		{
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config["AzureWebJobsStorage"]);
			CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
			CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(blobContainerName);
			return cloudBlobContainer;
		}

		public static async Task<bool> CheckIfFileIsAlreadyStoredAsync(CloudBlobContainer cloudBlobContainer, DateTime dateUtc, string fileName)
		{
			// Check the existence of the container
			if (!await cloudBlobContainer.ExistsAsync().ConfigureAwait(false)) {
				return false;
			}

			// Check the existence of the file
			string pathFileName = $"{dateUtc:yyyy/MM}/{fileName}";
			CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(pathFileName);
			return await cloudBlockBlob.ExistsAsync().ConfigureAwait(false);
		}

		public static async Task<long> StoreFileAsync(CloudBlobContainer cloudBlobContainer, DateTime dateUtc, string fileName, Stream stream)
		{
			// If the container doesn't exist, create it
			if (!await cloudBlobContainer.ExistsAsync().ConfigureAwait(false))
#pragma warning disable CA1031 // Do not catch general exception types
				try { await cloudBlobContainer.CreateAsync().ConfigureAwait(false); } catch { }
#pragma warning restore CA1031 // Do not catch general exception types

			string pathFileName = $"{dateUtc:yyyy/MM}/{fileName}";
			CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(pathFileName);
			await cloudBlockBlob.UploadFromStreamAsync(stream).ConfigureAwait(false);

			return stream.Position;
		}

		internal static async Task<long> GetBlobSizeAsync(CloudBlobContainer cloudBlobContainer, DateTime dateUtc, string fileName)
		{
			string pathFileName = $"{dateUtc:yyyy/MM}/{fileName}";
			CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(pathFileName);
			await cloudBlockBlob.FetchAttributesAsync().ConfigureAwait(false);
			long result = cloudBlockBlob.Properties.Length;
			return result;
		}

		internal static Task GetBlobContentAsync(CloudBlobContainer cloudBlobContainer, DateTime dateUtc, string fileName, Stream target)
		{
			string pathFileName = $"{dateUtc:yyyy/MM}/{fileName}";
			CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(pathFileName);
			return cloudBlockBlob.DownloadToStreamAsync(target);
		}
	}
}