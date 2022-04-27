using System;
using Newtonsoft.Json;

namespace Zoo105Podcast.Cosmos
{
	public class PodcastEpisode
	{
		// Id of the podcast, in format zoo_yyyyMMdd or 105polaroyd_yyyyMMdd
		[JsonProperty(PropertyName = "id")]
		public string Id { get; set; }
		// "zoo" or "105polaroyd"
		public string ShowName { get; set; }
		public DateTime DateUtc { get; set; }
		public string FileName { get; set; }
		public Uri CompleteUri { get; set; }
		public long? FileLength { get; set; }
		public TimeSpan? Duration { get; set; }
	}
}