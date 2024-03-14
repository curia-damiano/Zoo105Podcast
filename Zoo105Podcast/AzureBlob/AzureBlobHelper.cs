using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Zoo105Podcast.AzureBlob;

public class AzureBlobHelper(
	BlobServiceClient blobServiceClient)
{
	private const string BlobContainerName = "zoo105podcast";
	private BlobContainerClient? _blobContainerClient;

	public async Task InitializeBlobContainerClientAsync()
	{
		this._blobContainerClient = blobServiceClient.GetBlobContainerClient(BlobContainerName);
		if (!await this._blobContainerClient.ExistsAsync().ConfigureAwait(false))
		{
			this._blobContainerClient = await blobServiceClient.CreateBlobContainerAsync(BlobContainerName).ConfigureAwait(false);
		}
	}

	public async Task<bool> CheckIfFileIsAlreadyStoredAsync(DateTime dateUtc, string fileName)
	{
		ArgumentNullException.ThrowIfNull(this._blobContainerClient);

		// Check the existence of the container
		if (!await this._blobContainerClient.ExistsAsync().ConfigureAwait(false))
		{
			return false;
		}

		// Check the existence of the file
		string pathFileName = $"{dateUtc:yyyy/MM}/{fileName}";
		BlobClient blobClient = this._blobContainerClient.GetBlobClient(pathFileName);
		bool result = await blobClient.ExistsAsync().ConfigureAwait(false);
		return result;
	}

	public async Task<long> StoreFileAsync(DateTime dateUtc, string fileName, Stream stream)
	{
		ArgumentNullException.ThrowIfNull(this._blobContainerClient);
		ArgumentNullException.ThrowIfNull(stream);

		// If the container doesn't exist, create it
		if (!await this._blobContainerClient.ExistsAsync().ConfigureAwait(false))
		{
#pragma warning disable CA1031 // Do not catch general exception types
			try { await this._blobContainerClient.CreateAsync().ConfigureAwait(false); } catch { }
#pragma warning restore CA1031 // Do not catch general exception types
		}

		string pathFileName = $"{dateUtc:yyyy/MM}/{fileName}";
		BlobClient blobClient = this._blobContainerClient.GetBlobClient(pathFileName);
		await blobClient.UploadAsync(stream).ConfigureAwait(false);
		long result = stream.Position;
		return result;
	}

	internal async Task<long> GetBlobSizeAsync(DateTime dateUtc, string fileName)
	{
		ArgumentNullException.ThrowIfNull(this._blobContainerClient);

		string pathFileName = $"{dateUtc:yyyy/MM}/{fileName}";
		BlobClient blobClient = this._blobContainerClient.GetBlobClient(pathFileName);
		BlobProperties blobProperties = await blobClient.GetPropertiesAsync().ConfigureAwait(false);
		long result = blobProperties.ContentLength;
		return result;
	}

	internal Task GetBlobContentAsync(DateTime dateUtc, string fileName, Stream target)
	{
		ArgumentNullException.ThrowIfNull(this._blobContainerClient);

		string pathFileName = $"{dateUtc:yyyy/MM}/{fileName}";
		BlobClient blobClient = this._blobContainerClient.GetBlobClient(pathFileName);
		return blobClient.DownloadToAsync(target);
	}
}