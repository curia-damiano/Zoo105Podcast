﻿using System.IO;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Zoo105Podcast.Cosmos;

/// <remarks>
/// Code adapted from: https://ankitvijay.net/2021/06/20/custom-json-serializer-settings-with-cosmos-db-sdk/
/// See: https://github.com/Azure/azure-cosmos-dotnet-v3/blob/master/Microsoft.Azure.Cosmos.Samples/Usage/SystemTextJson/CosmosSystemTextJsonSerializer.cs
/// </remarks>
internal sealed class CosmosSystemTextJsonSerializer(
		JsonSerializerOptions? jsonSerializerOptions = null
	) : CosmosSerializer
{
	public override T FromStream<T>(Stream stream)
	{
		if (stream is { CanSeek: true, Length: 0 })
		{
			return default!;
		}

		if (typeof(Stream).IsAssignableFrom(typeof(T)))
		{
			return (T)(object)stream;
		}

		using (stream)
		{
			return JsonSerializer.Deserialize<T>(stream, jsonSerializerOptions)!;
		}
	}

	public override Stream ToStream<T>(T input)
	{
		var streamPayload = new MemoryStream();
		JsonSerializer.Serialize(streamPayload, input, jsonSerializerOptions);
		streamPayload.Position = 0;
		return streamPayload;
	}
}