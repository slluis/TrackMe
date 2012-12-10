using System;
using Android.App;
using Android.Content;
using Xamarin.TrackMe.TrackMeWebService;
using GCMSharp.Client;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.IO;
using Android.Graphics.Drawables;

namespace Xamarin.TrackMe
{
	/// <summary>
	/// This class contains all the information that the application handles, and provides
	/// events which allow tracking data changes. The events are not guaranteed to be
	/// fired in the UI thread.
	/// </summary>
	public static class TrackMeApp
	{
		static object StatusLock = new object ();
		static List<TrackedShare> Targets = new List<TrackedShare> ();
		static List<LocationShare> Shares = new List<LocationShare> ();

		public static readonly LocationService WebService;
		public static readonly UserInfo LocalUser;

		public static event EventHandler sharesChanged;
		public static event EventHandler trackedSharesChanged;

		public static bool EnableStatusPersistence { get; set; }

		static TrackMeApp ()
		{
			WebService = new LocationService ();
			WebService.Url = "http://ideary.com:8080/LocationService.asmx";

			var userId = Android.Provider.Settings.Secure.GetString(Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId); 		
			var userName = "Some User";

			LoadConfiguration ();

			try {
				GCMRegistrar.CheckDevice (Application.Context);
				GCMRegistrar.CheckManifest (Application.Context);
				var regId = GCMRegistrar.GetRegistrationId (Application.Context);
				if (regId == "")
					GCMRegistrar.Register (Application.Context, GCMIntentService.SenderId);
				else
					Console.WriteLine ("GCM client already registered: " + regId);
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
			
			LocalUser = new UserInfo () {
				Name = userName,
				Id = userId,
				RegistrationId = GCMRegistrar.GetRegistrationId (Application.Context)
			};

			Application.Context.RegisterReceiver (new LocalBroadcastReceiver (), new IntentFilter (GCMIntentService.BroadcastAction));
			
			System.Threading.ThreadPool.QueueUserWorkItem (delegate {
				var trackedShares = WebService.GetTrackedShares (TrackMeApp.LocalUser.Id);
				var shares = WebService.GetActiveShares (TrackMeApp.LocalUser.Id);
				lock (StatusLock) {
					Targets.Clear ();
					Targets.AddRange (trackedShares);
					Shares.Clear ();
					Shares.AddRange (shares);
					NotifySharesChanged ();
					NotifyTrackedSharesChanged ();
				}
			});
		}

		public static event EventHandler SharesChanged {
			add {
				lock (StatusLock)
					sharesChanged += value;
			}
			remove { 
				lock (StatusLock)
					sharesChanged -= value;
			}
		}

		public static event EventHandler TrackedSharesChanged {
			add {
				lock (StatusLock)
					trackedSharesChanged += value;
			}
			remove { 
				lock (StatusLock)
					trackedSharesChanged -= value;
			}
		}

		public static string PublishTarget (ShareSettings timeoutInfo)
		{
			var ts = WebService.CreateLocationShare (TrackMeApp.LocalUser, timeoutInfo);
			lock (StatusLock) {
				Shares.Add (ts);
				SaveConfiguration ();
			}
			NotifySharesChanged ();
			return "https://app.trackme/" + ts.PublicId;
		}

		public static PublicLocationShare GetPublicShareInfo (string targetId)
		{
			var t = WebService.GetPublicShareInfo (targetId);
			t.IconDrawable = LoadDrawable (t.User.Icon);
			return t;
		}
		
		public static void AddTarget (PublicLocationShare target)
		{
			var t = new TrackedShare () {
				Id = target.Id,
				Latitude = target.Latitude,
				Longitude = target.Longitude,
				User = target.User
			};
			lock (StatusLock) {
				Targets.Add (t);
			}
			NotifyTrackedSharesChanged ();
			System.Threading.ThreadPool.QueueUserWorkItem (delegate {
				var rt = WebService.RegisterTracker (target.Id, TrackMeApp.LocalUser);
				t.TrackerId = rt.TrackerId;
				NotifyTrackedSharesChanged ();
			});
		}
		
		public static LocationShare[] GetShares ()
		{
			lock (StatusLock)
				return Shares.ToArray ();
		}
		
		public static TrackedShare[] GetTrackedShares ()
		{
			lock (StatusLock) {
				foreach (var t in Targets) {
					if (t.IconDrawable == null)
						t.IconDrawable = LoadDrawable (t.User.Icon);
				}
				return Targets.ToArray ();
			}
		}
		
		static Drawable LoadDrawable (byte[] icon)
		{
			if (icon == null)
				return Application.Context.Resources.GetDrawable (Resource.Drawable.user);
			else {
				using (var s = new MemoryStream (icon))
					return new BitmapDrawable (s);
			}
		}

		static void NotifySharesChanged ()
		{
			var e = sharesChanged;
			if (e != null)
				e (null, EventArgs.Empty);
		}
		
		static void NotifyTrackedSharesChanged ()
		{
			var e = trackedSharesChanged;
			if (e != null)
				e (null, EventArgs.Empty);
		}
		
		public static void Notify (string msg, string secondaryMsg)
		{
			var nMgr = (NotificationManager)Application.Context.GetSystemService (Context.NotificationService);
			var notification = new Notification (Resource.Drawable.Icon, msg);
			var pendingIntent = PendingIntent.GetActivity (Application.Context, 0, new Intent (Application.Context, typeof(MainActivity)), 0);
			notification.SetLatestEventInfo (Application.Context, msg, secondaryMsg, pendingIntent);
			nMgr.Notify (0, notification);
		}

		class LocalBroadcastReceiver: BroadcastReceiver
		{
			public override void OnReceive (Context context, Intent intent)
			{
				lock (StatusLock) {
					ProcessBroadcastIntent (intent);
				}
			}
		}

		internal static void ProcessBroadcastIntent (Intent intent)
		{
			LogService.Log ("Processing broadcast intent: " + intent.Extras);
			
			var not = AppNotification.ReadNotification (intent);
			
			if (not is TargetChangedNotification) {
				var tn = (TargetChangedNotification) not;
				var t = Targets.FirstOrDefault (tr => tr.Id == tn.ShareId);
				if (t != null) {
					t.Latitude = tn.Latitude;
					t.Longitude = tn.Longitude;
				}
				NotifyTrackedSharesChanged ();
			}
			
			if (not is TrackerAddedNotification) {
				var tn = (TrackerAddedNotification) not;
				try {
					var t = WebService.GetTrackerInfo (tn.ShareId, tn.TrackerId);
					if (t != null) {
						lock (StatusLock) {
							var sh = Shares.FirstOrDefault (s => s.PrivateId == tn.ShareId);
							if (sh != null) {
								var arr = sh.Trackers;
								if (arr != null) {
									Array.Resize (ref arr, arr.Length + 1);
								} else
									arr = new UserInfo[1];
								arr [arr.Length - 1] = t;
								sh.Trackers = arr;
							}
						}
					}
					NotifySharesChanged ();
				} catch (Exception ex) {
					LogService.LogError (ex);
				}
			}
		}

		static void SaveConfiguration ()
		{
			if (!EnableStatusPersistence)
				return;

			lock (StatusLock) {
				ServiceStatus status = new ServiceStatus () {
					Targets = Targets,
					Shares = Shares
				};
				XmlSerializer ser = new XmlSerializer (typeof(ServiceStatus));
				using (var s = Application.Context.OpenFileOutput ("ServiceStatus", FileCreationMode.Private))
					ser.Serialize (s, status);
			}
		}
		
		static void LoadConfiguration ()
		{
			XmlSerializer ser = new XmlSerializer (typeof(ServiceStatus));
			try {
				using (var s = Application.Context.OpenFileInput ("ServiceStatus")) {
					var status = (ServiceStatus) ser.Deserialize (s);
					lock (StatusLock) {
						Targets = status.Targets;
						Shares = status.Shares;
					}
				}
			} catch {
				// Ignore
			}
		}
	}

	public class ServiceStatus
	{
		public List<TrackedShare> Targets = new List<TrackedShare> ();
		public List<LocationShare> Shares = new List<LocationShare> ();
	}
}

