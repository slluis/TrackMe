
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.GoogleMaps;
using Android.Locations;

namespace Xamarin.TrackMe
{
	[Activity (Label = "MapActivity")]			
	public class MapActivity : Android.GoogleMaps.MapActivity, ILocationListener
	{
		MyLocationOverlay myLocationOverlay;
		LocationManager locationManager;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			
			locationManager = GetSystemService (Context.LocationService) as LocationManager;
			
			RequestWindowFeature (WindowFeatures.NoTitle);
			
			SetContentView (Resource.Layout.Map);
			
			var map = FindViewById<MapView>(Resource.Id.map);
			map.Clickable = true;
			map.Traffic = true;
			map.SetBuiltInZoomControls (true);
			
			myLocationOverlay = new MyLocationOverlay (this, map);
			map.Overlays.Add (myLocationOverlay);
			
			myLocationOverlay.RunOnFirstFix (() => {
				map.Controller.SetZoom (15);
				map.Controller.AnimateTo (myLocationOverlay.MyLocation);
			});
		}

		protected override bool IsRouteDisplayed {
			get {
				return false;
			}
		}
		
		protected override void OnResume ()
		{
			base.OnResume ();
			myLocationOverlay.EnableMyLocation ();
			
			var locationCriteria = new Criteria();
			locationCriteria.Accuracy = Accuracy.NoRequirement;
			locationCriteria.PowerRequirement = Power.NoRequirement;
			
			string locationProvider = locationManager.GetBestProvider(locationCriteria, true);
			
			locationManager.RequestLocationUpdates (locationProvider, 2000, 1, this);		
		}
		
		protected override void OnPause ()
		{
			base.OnPause ();
			myLocationOverlay.DisableMyLocation ();
			locationManager.RemoveUpdates (this);
		}
		
		#region ILocationListener implementation
		
		void ILocationListener.OnLocationChanged (Location location)
		{
		}
		
		void ILocationListener.OnProviderDisabled (string provider)
		{
		}
		
		void ILocationListener.OnProviderEnabled (string provider)
		{
		}
		
		void ILocationListener.OnStatusChanged (string provider, Availability status, Bundle extras)
		{
		}
		
		#endregion	
	}
}

