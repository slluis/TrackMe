
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.TrackMe.TrackMeWebService;

namespace Xamarin.TrackMe
{
	[Activity (Label = "StartTrackingActivity", ConfigurationChanges=Android.Content.PM.ConfigChanges.Orientation)]			
	[IntentFilter (new[]{Intent.ActionView}, Categories=new[]{Intent.CategoryDefault,Intent.CategoryBrowsable}, DataScheme="https", DataHost="app.trackme")]
	public class StartTrackingActivity : Activity
	{
		Button buttonOK;
		LinearLayout boxProgress;
		LinearLayout boxUser;
		TextView userName;
		ImageView userIcon;
		TextView textMessage;
		string targetId;
		PublicLocationShare target;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.StartTracking);

			textMessage = FindViewById<TextView> (Resource.Id.textMessage);
			var buttonCancel = FindViewById<Button> (Resource.Id.buttonCancel);
			buttonOK = FindViewById<Button> (Resource.Id.buttonOk);

			userName = FindViewById<TextView> (Resource.Id.textUserName);
			userIcon = FindViewById<ImageView> (Resource.Id.imageUserIcon);

			boxProgress = FindViewById<LinearLayout> (Resource.Id.boxProgress);
			boxUser = FindViewById<LinearLayout> (Resource.Id.boxUser);

			boxUser.Visibility = ViewStates.Gone;
			buttonOK.Enabled = false;

			buttonCancel.Click += delegate {
				Finish ();
			};
			buttonOK.Click += delegate {
				buttonOK.Enabled = false;
				buttonCancel.Enabled = false;
				ThreadPool.QueueUserWorkItem (StartTracking);
			};

			targetId = Intent.Data.LastPathSegment;

			if (targetId == null) {
				ShowError ();
				return;
			}

			ThreadPool.QueueUserWorkItem (LoadTargetInfo);
		}

		void ShowError ()
		{
			textMessage.Text = Resources.GetString (Resource.String.invalidReference);
			boxProgress.Visibility = ViewStates.Gone;
			boxUser.Visibility = ViewStates.Gone;
			buttonOK.Visibility = ViewStates.Gone;
		}

		void ShowTarget ()
		{
			userName.Text = target.User.Name;
			userIcon.SetImageDrawable (target.IconDrawable);
			boxProgress.Visibility = ViewStates.Gone;
			boxUser.Visibility = ViewStates.Visible;
			buttonOK.Enabled = true;
		}

		void StartTracking (object o)
		{
			try {
				TrackMeApp.AddTarget (target);
				RunOnUiThread (delegate {
					var t = Toast.MakeText (this, Resource.String.startedTracking, ToastLength.Short);
					t.Show ();
					StartActivity (typeof(MainActivity));
				});
			} catch (Exception ex) {
				Console.WriteLine (ex);
				RunOnUiThread (ShowError);
			}
		}

		void LoadTargetInfo (object d)
		{
			try {
				target = TrackMeApp.GetPublicShareInfo (targetId);
				if (target != null)
					RunOnUiThread (ShowTarget);
				else
					RunOnUiThread (ShowError);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				RunOnUiThread (ShowError);
			}
		}
	}
}

