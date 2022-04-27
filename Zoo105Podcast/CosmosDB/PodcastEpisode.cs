using System;
using Newtonsoft.Json;

namespace Zoo105Podcast.CosmosDB
{
	public class PodcastEpisode
	{
		// Id of the podcast, in format yyyyMMdd
		[JsonProperty(PropertyName = "id")]
		public string Id { get; set; }
		public DateTime DateUtc { get; set; }
		public string FileName { get; set; }
		public Uri CompleteUri { get; set; }
		public long FileLength { get; set; }
	}
}