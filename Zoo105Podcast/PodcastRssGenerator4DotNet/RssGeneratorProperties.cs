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
#pragma warning disable IDE1006 // Naming Styles
	public string? iTunesCategory { get; init; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
	public string? iTunesSubCategory { get; init; }
#pragma warning restore IDE1006 // Naming Styles
	public bool IsExplicit { get; init; }
	public string? OwnerName { get; init; }
	public string? OwnerEmail { get; init; }
	public Uri? ImageUrl { get; init; }
	public IReadOnlyList<Episode>? Episodes { get; set; }
}