using System;
using System.Collections.Generic;

namespace Zoo105Podcast.PodcastRssGenerator4DotNet
{
	public partial class RssGenerator
	{
		public string Language { get; set; }
		public Uri PodcastUrl { get; set; }
		public string Title { get; set; }
		public Uri HomePageUrl { get; set; }
		public string Description { get; set; }
		public string AuthorName { get; set; }
		public string Copyright { get; set; }
#pragma warning disable IDE1006 // Naming Styles
		public string iTunesCategory { get; set; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning disable IDE1006 // Naming Styles
		public string iTunesSubCategory { get; set; }
#pragma warning restore IDE1006 // Naming Styles
		public bool IsExplicit { get; set; }
		public string OwnerName { get; set; }
		public string OwnerEmail { get; set; }
		public Uri ImageUrl { get; set; }
		public IReadOnlyList<Episode> Episodes { get; set; }
	}
}