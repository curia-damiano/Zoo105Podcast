using System;

namespace Zoo105Podcast.AzureQueue;

public class Podcast2Download
{
	// Id of the podcast, in format zoo_yyyyMMdd or 105polaroyd_yyyyMMdd
	public string Id { get; init; } = null!;
	public DateTime DateUtc { get; init; }
	public string FileName { get; init; } = null!;
	public Uri CompleteUri { get; init; } = null!;
}