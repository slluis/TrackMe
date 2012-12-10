using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.GoogleMaps;
using Android.Locations;
using Xamarin.TrackMe.TrackMeWebService;

namespace Xamarin.TrackMe
{
	[Activity (Label = "TrackMe", MainLauncher = true)]
	public class MainActivity : ActivityGroup
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			TrackMeApp.EnableStatusPersistence = true;

			RequestWindowFeature (WindowFeatures.NoTitle);

			SetContentView (Resource.Layout.Main);

			var shareButton = FindViewById<LinearLayout>(Resource.Id.boxShare);
			shareButton.Click += HandleShareClick;

			var tabHost = FindViewById<TabHost> (Resource.Id.tabHost);
			tabHost.Setup (this.LocalActivityManager);

			TabHost.TabSpec spec;     // Resusable TabSpec for each tab
			Intent intent;            // Reusable Intent for each tab
			
			// Create an Intent to launch an Activity for the tab (to be reused)
			intent = new Intent (this, typeof (MapActivity));
			intent.AddFlags (ActivityFlags.NewTask);
			
			// Initialize a TabSpec for each tab and add it to the TabHost
			spec = tabHost.NewTabSpec ("map");
			spec.SetIndicator ("Map", Resources.GetDrawable (Android.Resource.Drawable.IcDialogMap));
			spec.SetContent (intent);
			tabHost.AddTab (spec);
			
			// Do the same for the other tabs
			intent = new Intent (this, typeof (TrackListActivity));
			intent.AddFlags (ActivityFlags.NewTask);
			
			spec = tabHost.NewTabSpec ("tracking");
			spec.SetIndicator ("Tracking", Resources.GetDrawable (Android.Resource.Drawable.IcMenuMyPlaces));
			spec.SetContent (intent);
			tabHost.AddTab (spec);
			
			// Do the same for the other tabs
			intent = new Intent (this, typeof (SharesListActivity));
			intent.AddFlags (ActivityFlags.NewTask);
			
			spec = tabHost.NewTabSpec ("shares");
			spec.SetIndicator ("Shares", Resources.GetDrawable (Android.Resource.Drawable.IcMenuShare));
			spec.SetContent (intent);
			tabHost.AddTab (spec);

			tabHost.CurrentTab = 0;
		}

		void HandleShareClick (object sender, EventArgs e)
		{
			StartActivity (typeof(ShareLocationActivity));
		}

		protected override void OnResume ()
		{
			base.OnResume ();
		}
		
		protected override void OnPause ()
		{
			base.OnPause ();
		}
	}
}


