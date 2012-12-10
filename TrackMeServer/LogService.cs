using System;

namespace TrackMeServer
{
	public static class LogService
	{
		public static void Log (string message)
		{
			Console.WriteLine (message);
		}

		public static void Log (string message, params object[] args)
		{
			Console.WriteLine (message, args);
		}

		public static void LogError (Exception ex)
		{
			Console.WriteLine (ex);
		}
	}
}

