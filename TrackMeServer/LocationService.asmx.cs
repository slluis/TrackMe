using System;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Collections.Generic;
using PushSharp.Android;

namespace TrackMeServer
{
	[WebService]
	[WebServiceBinding ("TrackMeService", "http://xamarin.com/services")]
	public class LocationService : System.Web.Services.WebService
	{
		[WebMethod]
		public LocationShare[] GetActiveShares (string userId)
		{
			return TrackService.GetActiveShares (userId);
		}
		
		[WebMethod]
		public LocationShare CreateLocationShare (UserInfo user, ShareSettings settings)
		{
			return TrackService.CreateLocationShare (user, settings);
		}

		[WebMethod]
		public PublicLocationShare GetPublicShareInfo (string shareId)
		{
			return TrackService.GetPublicShareInfo (shareId);
		}
		
		[WebMethod]
		public TrackedShare[] GetTrackedShares (string userId)
		{
			return TrackService.GetTrackedShares (userId);
		}
		
		[WebMethod]
		public TrackedShare RegisterTracker (string publicShareId, UserInfo trackerInfo)
		{
			return TrackService.RegisterTracker (publicShareId, trackerInfo);
		}

		[WebMethod]
		public UserInfo GetTrackerInfo (string privateShareId, string trackerId)
		{
			return TrackService.GetTrackerInfo (privateShareId, trackerId);
		}
		
		[WebMethod]
		public void SetTrackerStatus (string privateShareId, string trackerId, bool allowTracking)
		{
			TrackService.SetTrackerStatus (privateShareId, trackerId, allowTracking);
		}

		[WebMethod]
		public void UpdateTargetPosition (string privateShareId, float longitude, float latitude)
		{
			TrackService.UpdateTargetPosition (privateShareId, longitude, latitude);
		}
	}
}

