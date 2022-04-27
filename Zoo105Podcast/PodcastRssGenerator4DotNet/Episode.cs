using System;

namespace Zoo105Podcast.PodcastRssGenerator4DotNet;

public class Episode
{
	public string? Title { get; init; }

	public Uri FileDownloadUrl { get; init; } = null!;

	public string? Description { get; init; }

	public Uri ImageUrl { get; init; } = null!;

	public long? FileLength { get; init; }

	public TimeSpan? Duration { get; init; }

	public DateTime PublicationDate { get; init; }
}