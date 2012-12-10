using System;
using PushSharp.Android;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PushSharp;
using System.Text;
using System.Globalization;
using MySql.Data.MySqlClient;
using System.Web.Configuration;
using System.Data.Common;

namespace TrackMeServer
{
	public static class TrackService
	{
		static GcmPushService pushService;
		
		static Random rand = new Random ();
		
		static Dictionary<string,MeetingData> meetings = new Dictionary<string, MeetingData> ();
		
		static TrackService ()
		{
			string senderID = "719481524012";
			string senderAuthToken = "AIzaSyA9zRa6qCBuso46zW-PzTiNQUkp4rGjFC4";
			string applicationIdPackageName = "xamarin.TrackMe";
			
			GcmPushChannelSettings settings = new GcmPushChannelSettings (senderID, senderAuthToken, applicationIdPackageName);
			pushService = new GcmPushService (settings);

			pushService.Events.OnDeviceSubscriptionExpired += delegate(PushSharp.Common.PlatformType platform, string deviceInfo, PushSharp.Common.Notification notification) {
				LogService.Log ("DeviceSubscriptionExpired: " + deviceInfo);
			};
			pushService.Events.OnDeviceSubscriptionIdChanged += delegate(PushSharp.Common.PlatformType platform, string oldDeviceInfo, string newDeviceInfo, PushSharp.Common.Notification notification) {
				LogService.Log ("DeviceSubscriptionIdChanged: " + oldDeviceInfo + " " + newDeviceInfo);
			};
			pushService.Events.OnChannelException += delegate(Exception exception, PushSharp.Common.PlatformType platformType, PushSharp.Common.Notification notification) {
				LogService.Log ("ChannelException: " + exception);
			};
			pushService.Events.OnNotificationSendFailure += delegate(PushSharp.Common.Notification notification, Exception notificationFailureException) {
				LogService.Log ("NotificationSendFailure: " + notificationFailureException);
			};
			pushService.Events.OnNotificationSent += delegate(PushSharp.Common.Notification notification) {
				LogService.Log ("NotificationSent: " + notification);
			};

		}

		public static MySqlConnection GetConnection ()
		{
			MySqlConnection db = null;
			try {
				string conn_string = WebConfigurationManager.ConnectionStrings["TrackMeConnectionString"].ConnectionString;
				db = new MySqlConnection (conn_string);
				db.Open ();
				return db;
			} catch (Exception ex) {
				if (db != null)
					db.Close ();
				throw new Exception ("Database connection failed", ex);
			}
		}

		public static void Init ()
		{
/*			ShareSettings settings = new ShareSettings ();
			settings.TimeoutMinutes = 60;
			var user = new UserInfo () { Name = "Lluis", Id="SomeId" };
			CreateLocationShare (user, settings);*/
			GetTrackedShares ("bla");
		}
		
		public static string RegisterMeeting (MeetingConfiguration meetingConfig)
		{
			lock (meetings) {
				var data = new MeetingData (meetingConfig);
				data.Id = GenerateRandomId ();
				data.Timestamp = DateTime.Now;
				meetings[data.Id] = data;
				LogService.Log ("Registered Meeting {0}", data.Id);
				return data.Id;
			}
		}
		
		public static void UpdateMeeting (string id, MeetingConfiguration meetingConfig)
		{
			lock (meetings) {
				MeetingData m;
				meetings.TryGetValue (id, out m);
				m.CopyFrom (meetingConfig);
				LogService.Log ("Updated Meeting {0}", id);
				NotifyMeetingChanged (m);
			}
		}
		
		public static MeetingData RegisterMeetingTarget (string meetingId, string targetId)
		{
			lock (meetings) {
				MeetingData m;
				meetings.TryGetValue (meetingId, out m);
				if (m == null)
					return null;
				LogService.Log ("Registered Target {0} for Meeting {1}", targetId, meetingId);
				m.Targets.Add (targetId);
				NotifyMeetingTargetAdded (meetingId, targetId);
				return m;
			}
		}

		public static LocationShare[] GetActiveShares (string userId)
		{
			try {
				var db = GetConnection ();
				var shares = db.SelectObjectsWhere<LocationShareData> ("userId = {0}", userId);
				return shares.Select (s => GetFullShare (db, s)).ToArray ();
			} catch (Exception e) {
				ThrowFailure (e);
				throw;
			}
		}

		static LocationShare GetFullShare (DbConnection db, LocationShareData s)
		{
			var trackers = db.SelectObjectsWhere<TrackerData> ("sharePrivateId={0}", s.PrivateId);
			return new LocationShare (s, trackers);
		}
		
		public static LocationShare CreateLocationShare (UserInfo user, ShareSettings settings)
		{
			try {
				Console.WriteLine ("test");
				LocationShareData data = new LocationShareData () {
					UserId = user.Id,
					UserName = user.Name,
					UserIcon = user.Icon,
					UserRegistrationId = user.RegistrationId,
					PublicId = GenerateRandomId (),
					PrivateId = GenerateRandomId (),
					Longitude = 0,
					Latitude = 0,
					SharedTime = DateTime.Now,
					ExpireTime = settings.CalculateTimeout (),
					ExpireOnArrival = settings.CancelOnArrival
				};

				var db = GetConnection ();
				db.InsertObject (data);
				LogService.Log ("Registered Share " + data.PublicId + ", with private Id " + data.PrivateId + ", registration id " + user.RegistrationId);
				return new LocationShare (data, null);
			} catch (Exception e) {
				ThrowFailure (e);
				throw;
			}
		}

		public static LocationShare[] GetAllShares ()
		{
			try {
				var db = GetConnection ();
				return db.SelectObjects<LocationShareData> ().Select (s => new LocationShare (s, null)).ToArray ();
			} catch (Exception e) {
				ThrowFailure (e);
				throw;
			}
		}
		
		public static TrackedShare[] GetTrackedShares (string userId)
		{
			try {
				var db = GetConnection ();
				return db.SelectObjects<TrackedShareData> ("SELECT locationshare.*, tracker.trackerId FROM locationshare, tracker WHERE locationshare.PrivateId = tracker.sharePrivateId AND tracker.userId={0}", userId).Select (s => new TrackedShare (s, s.TrackerId)).ToArray ();
			} catch (Exception e) {
				Console.WriteLine (e);
				ThrowFailure (e);
				throw;
			}
		}
		
		public static PublicLocationShare GetPublicShareInfo (string shareId)
		{
			try {
				var db = GetConnection ();
				var s = db.SelectObjectWhere<LocationShareData> ("publicId = {0}", shareId);
				if (s != null)
					return new PublicLocationShare (s);
				else
					return null;
			} catch (Exception ex) {
				ThrowFailure (ex);
				throw;
			}
		}

		static void ThrowFailure (Exception ex)
		{
			LogService.LogError (ex);
			throw new Exception ("Operation failed");
		}
		
		public static LocationShare GetPrivateShareInfo (string shareId)
		{
			try {
				var db = GetConnection ();
				var s = db.SelectObjectWhere<LocationShareData> ("privateId = {0}", shareId);
				if (s != null)
					return GetFullShare (db, s);
				else
					return null;
			} catch (Exception ex) {
				ThrowFailure (ex);
				throw;
			}
		}
		
		public static TrackedShare RegisterTracker (string publicShareId, UserInfo trackerInfo)
		{
			try {
				var db = GetConnection ();

				var s = db.SelectObjectWhere<LocationShareData> ("publicId = {0}", publicShareId);
				if (s == null)
					return null;

				var t = new TrackerData () {
					TrackerId = GenerateRandomId (),
					SharePrivateId = s.PrivateId,
					UserId = trackerInfo.Id,
					UserRegistrationId = trackerInfo.RegistrationId,
					UserName = trackerInfo.Name,
					UserIcon = trackerInfo.Icon
				};
				db.InsertObject (t);

				LogService.Log ("Registered Tracker " + trackerInfo.Id + " for Target " + publicShareId + ", registration id " + trackerInfo.RegistrationId);
				NotifyTrackerAdded (t);
				return new TrackedShare (s, t.TrackerId);
			} catch (Exception ex) {
				ThrowFailure (ex);
				throw;
			}
		}
		
		public static UserInfo GetTrackerInfo (string privateShareId, string trackerId)
		{
			try {
				var db = GetConnection ();
				TrackerData t = db.SelectObjectWhere<TrackerData> ("sharePrivateId={0} AND trackerId={1}", privateShareId, trackerId);
				if (t != null) {
					return new UserInfo () {
						Name = t.UserName,
						Icon = t.UserIcon,
						Id = trackerId
					};
				} else
					return null;
			} catch (Exception ex) {
				ThrowFailure (ex);
				throw;
			}
		}
		
		public static void SetTrackerStatus (string privateShareId, string trackerId, bool allowTracking)
		{
		}
		
		public static void UpdateTargetPosition (string privateShareId, float longitude, float latitude)
		{
//			NotifyTargetChanged (r);
		}
		
		static void NotifyTargetChanged (LocationShare t)
		{
			var not = new GcmNotification ();
			not.RegistrationIds.AddRange (t.Trackers.Select (tr => tr.RegistrationId));
			not.JsonData = string.Format ("{event:\"TargetChanged\", trackedShareId:\"{0}\", latitude:\"{1}\", longitude:\"{2}\"}", t.PublicId, t.Latitude, t.Longitude);
			StringBuilder sb = new StringBuilder ();
			sb.Append ("{");
			sb.Append ("event:\"TargetChanged\", ");
			sb.Append ("trackedShareId:\"").Append (t.PublicId).Append ("\", ");
			sb.Append ("latitude:\"").Append (t.Latitude.ToString (CultureInfo.InvariantCulture)).Append ("\", ");
			sb.Append ("longitude:\"").Append (t.Longitude.ToString (CultureInfo.InvariantCulture)).Append ("\"");
			sb.Append ("}");
			not.JsonData = sb.ToString ();
			pushService.QueueNotification (not);
		}
		
		static void NotifyTrackerAdded (TrackerData t)
		{
			var not = new GcmNotification ();
			not.RegistrationIds.Add (t.UserRegistrationId);
			StringBuilder sb = new StringBuilder ();
			sb.Append ("{event:\"TrackerAdded\", shareId:\"").Append (t.SharePrivateId).Append ("\", trackerId:\"").Append (t.TrackerId).Append ("\"}");
			not.JsonData = sb.ToString ();
			LogService.Log ("Sending message: " + sb.ToString ());
			pushService.QueueNotification (not);
		}
		
		static void NotifyTrackerStatusChanged (LocationShare t, string trackerId, bool enabled)
		{
		}
		
		static void NotifyMeetingTargetAdded (string meetingId, string targetId)
		{
		}
		
		static void NotifyMeetingChanged (MeetingData meeting)
		{
		}
		
		static string GenerateRandomId ()
		{
			byte[] aid = new byte[40];
			rand.NextBytes (aid);
			var s = Convert.ToBase64String (aid);
			return HttpUtility.UrlEncode (s).Replace ('%','x');
		}
	}

	
	public class TrackSession
	{
		public string PublicId { get; set; }
		public string SessionId { get; set; }
	}
	
	public class ShareSettings
	{
		public int TimeoutMinutes { get; set; }
		public bool CancelOnArrival { get; set; }
		internal DateTime Timeout { get; set; }
		
		public DateTime CalculateTimeout ()
		{
			return DateTime.Now.AddMinutes (TimeoutMinutes);
		}
	}
	
	public class MeetingConfiguration
	{
		public string Name { get; set; }
		public float Longitude { get; set; }
		public float Latitude { get; set; }
		public ShareSettings Timeout { get; set; }
	}
	
	public class MeetingData
	{
		HashSet<string> targets = new HashSet<string> ();
		
		public MeetingData ()
		{
		}
		
		public MeetingData (MeetingConfiguration meetingConfig)
		{
			CopyFrom (meetingConfig);
		}
		
		public void CopyFrom (MeetingConfiguration meetingConfig)
		{
			Longitude = meetingConfig.Longitude;
			Latitude = meetingConfig.Latitude;
			Name = meetingConfig.Name;
			Timeout = meetingConfig.Timeout;
		}
		
		internal HashSet<string> Targets {
			get { return targets; }
		}
		
		internal DateTime Timestamp { get; set; }
		
		public string Id { get; set; }
		public string Name { get; set; }
		public float Longitude { get; set; }
		public float Latitude { get; set; }
		public ShareSettings Timeout { get; set; }
	}
	
	public class UserInfo
	{
		public UserInfo ()
		{
		}

		public UserInfo (UserInfo u)
		{
			Id = u.Id;
			Name = u.Name;
			Icon = u.Icon;
		}

		public string PrivateId {
			get;
			set;
		}

		public string Id { get; set; }
		public string RegistrationId { get; set; }
		public string Name { get; set; }
		public byte[] Icon { get; set; }
	}
	
	public class TrackedShare
	{
		public TrackedShare ()
		{
		}

		public TrackedShare (LocationShareData s, string trackerId)
		{
			TrackerId = trackerId;
			Id = s.PublicId;
			Longitude = s.Longitude;
			Latitude = s.Latitude;
			User = new UserInfo () {
				Name = s.UserName,
				Icon = s.UserIcon
			};
		}

		public string Id { get; set; }
		public string TrackerId { get; set; }
		public float Longitude { get; set; }
		public float Latitude { get; set; }
		public UserInfo User { get; set; }
	}

	public class PublicLocationShare
	{
		public PublicLocationShare ()
		{
		}
		
		public PublicLocationShare (LocationShareData t)
		{
			Id = t.PublicId;
			Longitude = t.Longitude;
			Latitude = t.Latitude;
			User = new UserInfo () {
				Name = t.UserName,
				Icon = t.UserIcon
			};
		}
		
		public string Id { get; set; }
		public float Longitude { get; set; }
		public float Latitude { get; set; }
		public UserInfo User { get; set; }
	}

	public class LocationShare
	{
		public LocationShare ()
		{
		}

		public LocationShare (LocationShareData data, IEnumerable<TrackerData> shareTrackers)
		{
			PrivateId = data.PrivateId;
			PublicId = data.PublicId;
			Longitude = data.Longitude;
			Latitude = data.Latitude;
			User = new UserInfo () {
				Id = data.UserId,
				Name = data.UserName,
				Icon = data.UserIcon
			};
			SharedTime = data.SharedTime;
			ExpireTime = data.ExpireTime;

			if (shareTrackers != null) {
				foreach (var t in shareTrackers) {
					trackers.Add (new UserInfo () {
						Id = t.TrackerId,
						Name = t.UserName,
						Icon = t.UserIcon
					});
				}
			}
		}

		List<UserInfo> trackers = new List<UserInfo> ();

		public string PrivateId { get; set; }
		public string PublicId { get; set; }
		public float Longitude { get; set; }
		public float Latitude { get; set; }
		public UserInfo User { get; set; }
		public ShareSettings ShareSettings { get; set; }

		public DateTime SharedTime { get; set; }
		public DateTime ExpireTime { get; set; }
		
		internal bool AllowTracking { get; set; }
		
		public List<UserInfo> Trackers {
			get { return trackers; }
		}
	}
}

