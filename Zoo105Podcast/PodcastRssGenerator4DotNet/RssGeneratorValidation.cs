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
				throw new InvalidOperationException($"{nameof(PodcastUrl)} is null or empty");
			if (string.IsNullOrEmpty(Title))
				throw new ArgumentNullException($"{nameof(Title)} is null or empty");
			if (string.IsNullOrEmpty(HomePageUrl.ToString()))
				throw new ArgumentNullException($"{nameof(HomePageUrl)} is null or empty");
			if (string.IsNullOrEmpty(Description))
				throw new ArgumentNullException($"{nameof(Description)} is null or empty");
			if (string.IsNullOrEmpty(AuthorName))
				throw new ArgumentNullException($"{nameof(AuthorName)} is null or empty");
			if (string.IsNullOrEmpty(Copyright))
				throw new ArgumentNullException($"{nameof(Copyright)} is null or empty");
			if (string.IsNullOrEmpty(iTunesCategory))
				throw new ArgumentNullException($"{nameof(iTunesCategory)} is null or empty");
			// Subcategories can be null: https://validator.w3.org/feed/docs/error/InvalidItunesCategory.html
			//if (string.IsNullOrEmpty(iTunesSubCategory))
			//	throw new ArgumentNullException("iTunesSubCategory cannot be empty or null.");
			if (string.IsNullOrEmpty(OwnerName))
				throw new ArgumentNullException($"{nameof(OwnerName)} is null or empty");
			if (string.IsNullOrEmpty(OwnerEmail))
				throw new ArgumentNullException($"{nameof(OwnerEmail)} is null or empty");
			if (string.IsNullOrEmpty(ImageUrl.ToString()))
				throw new ArgumentNullException($"{nameof(ImageUrl)} is null or empty");
		}

#pragma warning disable CA1822 // Mark members as static
		private void ValidateEpisodeProperties(Episode episode)
#pragma warning restore CA1822 // Mark members as static
		{
			if (string.IsNullOrEmpty(episode.Title))
				throw new ArgumentNullException($"{nameof(Episode.Title)} is null or empty");
			if (string.IsNullOrEmpty(episode.FileDownloadUrl.ToString()))
				throw new ArgumentNullException($"{nameof(Episode.FileDownloadUrl)} is null or empty");
			if (string.IsNullOrEmpty(episode.Description))
				throw new ArgumentNullException($"{nameof(Episode.Description)} is null or empty");
			if (episode.FileLength <= 0)
				throw new ArgumentNullException($"{nameof(Episode.FileLength)} is <= 0");
		}
	}
}