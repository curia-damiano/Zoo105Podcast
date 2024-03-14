using System;
using System.Collections.Generic;

namespace Zoo105Podcast.PodcastRssGenerator4DotNet;

public partial class RssGenerator
{
	public string? Language { get; set; }
	public Uri? PodcastUrl { get; init; }
	public string? Title { get; init; }
	public Uri? HomePageUrl { get; init; }
	public string? Description { get; init; }
	public string? AuthorName { get; init; }
	public string? Copyright { get; init; }
	public string? iTunesCategory { get; init; }
	public string? iTunesSubCategory { get; init; }
	public bool IsExplicit { get; init; }
	public string? OwnerName { get; init; }
	public string? OwnerEmail { get; init; }
	public Uri? ImageUrl { get; init; }
	public IReadOnlyList<Episode>? Episodes { get; set; }
}