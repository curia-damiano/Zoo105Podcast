using System;

namespace Zoo105Podcast.AzureQueue
{
	public class Podcast2Download
	{
		// Id of the podcast, in format zoo_yyyyMMdd or 105polaroyd_yyyyMMdd
		public string Id { get; set; }
		public DateTime DateUtc { get; set; }
		public string FileName { get; set; }
		public Uri CompleteUri { get; set; }
	}
}