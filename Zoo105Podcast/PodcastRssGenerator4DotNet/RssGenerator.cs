using System;
using System.Globalization;
using System.Xml;

namespace Zoo105Podcast.PodcastRssGenerator4DotNet
{
	public partial class RssGenerator
	{
		public void Generate(XmlWriter writer)
		{
			ValidatePodcastProperties();

			string itunesUri = "http://www.itunes.com/dtds/podcast-1.0.dtd";
			string atomUri = "http://www.w3.org/2005/Atom";

			// Start document
			writer.WriteStartDocument();

			// Start rss
			writer.WriteStartElement("rss");
			writer.WriteAttributeString("xmlns", "itunes", null, itunesUri);
			writer.WriteAttributeString("xmlns", "atom", null, atomUri);
			writer.WriteAttributeString("version", "2.0");
			writer.WriteAttributeString("xml", "lang", null, this.Language);

			// Start channel
			writer.WriteStartElement("channel");

			// Start and end atom:link
			writer.WriteStartElement("atom", "link", atomUri);
			writer.WriteAttributeString("href", this.PodcastUrl.ToString());
			writer.WriteAttributeString("rel", "self");
			writer.WriteAttributeString("type", "application/rss+xml");
			writer.WriteEndElement();

			// Back to channel
			writer.WriteStartElement("title"); writer.WriteCData(this.Title); writer.WriteEndElement();
			writer.WriteElementString("link", this.HomePageUrl.ToString());
			writer.WriteStartElement("description"); writer.WriteCData(this.Description); writer.WriteEndElement();
			writer.WriteElementString("itunes", "author", itunesUri, this.AuthorName);
			writer.WriteElementString("language", this.Language);
			writer.WriteElementString("copyright", this.Copyright);

			// Start itunes:category
			writer.WriteStartElement("itunes", "category", itunesUri);
			writer.WriteAttributeString("text", this.iTunesCategory);
			if (!string.IsNullOrEmpty(this.iTunesSubCategory))
			{
				// Start itunes:category
				writer.WriteStartElement("itunes", "category", itunesUri);
				writer.WriteAttributeString("text", this.iTunesSubCategory);
				// End itunes:category
				writer.WriteEndElement();
			}
			// End itunes:category
			writer.WriteEndElement();

			// Back to channel
			writer.WriteElementString("itunes", "explicit", itunesUri, this.IsExplicit ? "Yes" : "No");

			// Start and end itunes:owner
			writer.WriteStartElement("itunes", "owner", itunesUri);
			writer.WriteElementString("itunes", "name", itunesUri, this.OwnerName);
			writer.WriteElementString("itunes", "email", itunesUri, this.OwnerEmail);
			writer.WriteEndElement();

			// Back to channel
			writer.WriteStartElement("itunes", "image", itunesUri); writer.WriteAttributeString("href", this.ImageUrl.ToString()); writer.WriteEndElement();

			if (this.Episodes != null)
				foreach (Episode episode in this.Episodes)
				{
					ValidateEpisodeProperties(episode);

					// Start podcast item
					writer.WriteStartElement("item");
					writer.WriteStartElement("title"); writer.WriteCData(episode.Title); writer.WriteEndElement();
					writer.WriteElementString("link", episode.FileDownloadUrl.ToString());
					writer.WriteStartElement("description"); writer.WriteCData(episode.Description); writer.WriteEndElement();
					writer.WriteElementString("itunes", "author", itunesUri, string.Empty);

					// Start and end enclosure
					writer.WriteStartElement("enclosure");
					writer.WriteAttributeString("length", episode.FileLength.ToString());
					writer.WriteAttributeString("type", "audio/mpeg");
					writer.WriteAttributeString("url", episode.FileDownloadUrl.ToString());
					writer.WriteEndElement();

					// Back to item
					writer.WriteElementString("pubDate", GetRFC822Date(episode.PublicationDate));
					writer.WriteElementString("guid", episode.FileDownloadUrl.ToString());

					// End podcast item
					writer.WriteEndElement();
				}

			// End channel
			writer.WriteEndElement();

			// End rss
			writer.WriteEndElement();

			// End document
			writer.WriteEndDocument();

			writer.Flush();
			writer.Close();
		}

		// From: https://madskristensen.net/blog/convert-a-date-to-the-rfc822-standard-for-use-in-rss-feeds/
		private static string GetRFC822Date(DateTime date)
		{
			CultureInfo formattingCulture = CultureInfo.GetCultureInfo("en-UK");

			int offset = 0;// TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours;
			string timeZone = "+" + offset.ToString().PadLeft(2, '0');
			if (offset < 0)
			{
				int i = offset * -1;
				timeZone = "-" + i.ToString().PadLeft(2, '0');
			}
			return date.ToString("ddd, dd MMM yyyy HH:mm:ss " + timeZone.PadRight(5, '0'), formattingCulture);
		}
	}
}