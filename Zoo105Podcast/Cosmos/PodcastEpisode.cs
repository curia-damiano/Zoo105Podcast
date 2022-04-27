using System;
using System.Text.Json.Serialization;

namespace Zoo105Podcast.Cosmos;

public class PodcastEpisode
{
	// Id of the podcast, in format zoo_yyyyMMdd or 105polaroyd_yyyyMMdd
	[JsonPropertyName("id")]
	public string Id { get; init; } = null!;
	// "zoo" or "105polaroyd"
	public string ShowName { get; init; } = null!;
	public DateTime DateUtc { get; init; }
	public string FileName { get; init; } = null!;
	public Uri CompleteUri { get; init; } = null!;
	public long? FileLength { get; set; }
	public TimeSpan? Duration { get; set; }
}