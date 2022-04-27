using System;

namespace Zoo105Podcast.PodcastRssGenerator4DotNet
{
	public partial class RssGenerator
	{
		private void ValidatePodcastProperties()
		{
			if (string.IsNullOrEmpty(Language))
				Language = "en-US";
			if (string.IsNullOrEmpty(PodcastUrl.ToString()))
				throw new ArgumentException($"{nameof(PodcastUrl)} is null or empty");
			if (string.IsNullOrEmpty(Title))
				throw new ArgumentException($"{nameof(Title)} is null or empty");
			if (string.IsNullOrEmpty(HomePageUrl.ToString()))
				throw new ArgumentException($"{nameof(HomePageUrl)} is null or empty");
			if (string.IsNullOrEmpty(Description))
				throw new ArgumentException($"{nameof(Description)} is null or empty");
			if (string.IsNullOrEmpty(AuthorName))
				throw new ArgumentException($"{nameof(AuthorName)} is null or empty");
			if (string.IsNullOrEmpty(Copyright))
				throw new ArgumentException($"{nameof(Copyright)} is null or empty");
			if (string.IsNullOrEmpty(iTunesCategory))
				throw new ArgumentException($"{nameof(iTunesCategory)} is null or empty");
			// Subcategories can be null: https://validator.w3.org/feed/docs/error/InvalidItunesCategory.html
			//if (string.IsNullOrEmpty(iTunesSubCategory))
			//	throw new ArgumentException($"{nameof(iTunesSubCategory)} is null or empty");
			if (string.IsNullOrEmpty(OwnerName))
				throw new ArgumentException($"{nameof(OwnerName)} is null or empty");
			if (string.IsNullOrEmpty(OwnerEmail))
				throw new ArgumentException($"{nameof(OwnerEmail)} is null or empty");
			if (string.IsNullOrEmpty(ImageUrl.ToString()))
				throw new ArgumentException($"{nameof(ImageUrl)} is null or empty");
		}

#pragma warning disable CA1822 // Mark members as static
		private void ValidateEpisodeProperties(Episode episode)
#pragma warning restore CA1822 // Mark members as static
		{
			if (string.IsNullOrEmpty(episode.Title))
				throw new ArgumentException($"{nameof(Episode.Title)} is null or empty");
			if (string.IsNullOrEmpty(episode.FileDownloadUrl.ToString()))
				throw new ArgumentException($"{nameof(Episode.FileDownloadUrl)} is null or empty");
			if (string.IsNullOrEmpty(episode.Description))
				throw new ArgumentException($"{nameof(Episode.Description)} is null or empty");
			if (episode.FileLength <= 0)
				throw new ArgumentException($"{nameof(Episode.FileLength)} is null or empty");
		}
	}
}