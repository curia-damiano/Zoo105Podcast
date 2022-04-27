using System;

namespace Zoo105Podcast
{
	public class MyApplicationException : Exception
	{
		public MyApplicationException() : base() { }
		public MyApplicationException(string message) : base(message) { }
		public MyApplicationException(string message, Exception innerException) : base(message, innerException) { }
	}
}