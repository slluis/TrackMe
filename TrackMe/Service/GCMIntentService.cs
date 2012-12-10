using System;
using GCMSharp.Client;
using Android.App;
using Android.Content;

namespace Xamarin.TrackMe
{
	[Service (Name="xamarin.TrackMe.GCMIntentService")]
	public class GCMIntentService: GCMBaseIntentService
	{
		public const string SenderId = "719481524012";

		public const string BroadcastAction = "com.xamarin.trackme.broadcastaction";

		public GCMIntentService (): base (SenderId)
		{
			LogService.Log ("GCM Service Started");
		}

		public override void OnCreate ()
		{
			base.OnCreate ();
		}

		protected override void OnRegistered (Android.Content.Context context, string registrationId)
		{
			LogService.Log ("GCM Registered: " + registrationId);
		}

		protected override void OnUnRegistered (Android.Content.Context context, string registrationId)
		{
			LogService.Log ("GCM Unregistered: " + registrationId);
		}

		protected override void OnMessage (Android.Content.Context context, Android.Content.Intent intent)
		{
			LogService.Log ("GCM Message: " + intent);

			try {
				var not = AppNotification.ReadNotification (intent);

				if (not != null) {
					ProcessServerNotification (not);

					// Broadcast the message to all running activities
					var bintent = new Intent (BroadcastAction);
					bintent.PutExtras (intent);
					Application.Context.SendBroadcast (bintent);
				}
			}
			catch (Exception ex) {
				LogService.LogError (ex);
			}
		}

		protected override void OnError (Android.Content.Context context, string errorId)
		{
			LogService.Log ("GCM Error: " + errorId);
		}

		void ProcessServerNotification (AppNotification not)
		{
			if (not is TrackerAddedNotification) {
				// Show a notification to the user
				var n = (TrackerAddedNotification) not;
				var t = TrackMeApp.WebService.GetTrackerInfo (n.ShareId, n.TrackerId);
				if (t != null) {
					string msg = string.Format (Application.Context.GetString (Resource.String.trackerNotification), t.Name);
					TrackMeApp.Notify (msg, Application.Context.GetString (Resource.String.touchToOpen));
				}
			}
		}
	}
}

