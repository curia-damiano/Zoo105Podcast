﻿using System;

namespace Zoo105Podcast.PodcastRssGenerator4DotNet
{
	public class Episode
	{
		public string Title { get; set; }

		public Uri FileDownloadUrl { get; set; }

		public string Description { get; set; }

		public long FileLength { get; set; }

		public DateTime PublicationDate { get; set; }
	}
}