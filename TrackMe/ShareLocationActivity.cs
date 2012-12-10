
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
using Xamarin.TrackMe.TrackMeWebService;
using System.Threading;

namespace Xamarin.TrackMe
{
	[Activity (Label = "ShareLocationActivity")]			
	public class ShareLocationActivity : Activity
	{
		Spinner spinnerTime;
		ArrayAdapter arrayAdapter;
		int selectedTime;
		DateTime customDate;
		Button shareButton;
		LinearLayout boxProgress;
		TextView textDate;

		const int defaultTimeIndex = 3;

		int[] timeValues = { 10, 30, 45, 60, 60 * 2, 60 * 3, 60 * 6, 60 * 12, 60 * 24, 60 * 48, -1, 0 };

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.ShareLocation);

			customDate = DateTime.Now;

			boxProgress = FindViewById<LinearLayout> (Resource.Id.boxProgress);
			boxProgress.Visibility = ViewStates.Gone;

			textDate = FindViewById<TextView> (Resource.Id.textDate);
			textDate.Visibility = ViewStates.Gone;

			spinnerTime = FindViewById<Spinner> (Resource.Id.spinnerTime);
			spinnerTime.ItemSelected += HandleItemSelected;
			arrayAdapter = ArrayAdapter.CreateFromResource (this, Resource.Array.expiration_time_array, Android.Resource.Layout.SimpleSpinnerItem);
			arrayAdapter.SetDropDownViewResource (Android.Resource.Layout.SimpleSpinnerDropDownItem);
			spinnerTime.Adapter = arrayAdapter;
			spinnerTime.SetSelection (defaultTimeIndex);
			selectedTime = timeValues [defaultTimeIndex];

			shareButton = FindViewById<Button> (Resource.Id.buttonShare);
			shareButton.Click += HandleShareClick;

			textDate.Click += delegate {
				ShowDialog (0);
			};
		}

		void HandleItemSelected (object sender, AdapterView.ItemSelectedEventArgs e)
		{
			var time = timeValues [e.Position];
			if (time == -1) {
				ShowDialog (0);
			} else {
				selectedTime = time;
				textDate.Visibility = ViewStates.Gone;
			}
		}
		
		void HandleShareClick (object sender, EventArgs e)
		{
			var chb = FindViewById<CheckBox> (Resource.Id.checkMeet);
			var ti = new ShareSettings ();
			ti.CancelOnArrival = chb.Checked;
			ti.TimeoutMinutes = selectedTime;
			shareButton.Visibility = ViewStates.Gone;
			boxProgress.Visibility = ViewStates.Visible;

			ThreadPool.QueueUserWorkItem (delegate {
				try {
					var trackUrl = TrackMeApp.PublishTarget (ti);
					RunOnUiThread (delegate {
						var intent = new Intent (Intent.ActionSend);
						intent.SetType ("text/plain");
						intent.PutExtra (Intent.ExtraSubject, Resources.GetString (Resource.String.shareSubject));
						intent.PutExtra (Intent.ExtraText, trackUrl);
						StartActivity (Intent.CreateChooser (intent, Resources.GetString (Resource.String.shareVia)));
						Finish ();
					});
				} catch (Exception ex) {
					Console.WriteLine (ex);
					RunOnUiThread (ShowError);
				}
			});
		}

		void ShowError ()
		{
			boxProgress.Visibility = ViewStates.Gone;
			shareButton.Visibility = ViewStates.Visible;
			Toast.MakeText (this, Resources.GetString (Resource.String.shareFailed), ToastLength.Short).Show ();
		}

		protected override Dialog OnCreateDialog (int id)
		{
			return new DatePickerDialog (this, HandleDateSet, customDate.Year, customDate.Month - 1, customDate.Day);
		}

		void HandleDateSet (object sender, DatePickerDialog.DateSetEventArgs e)
		{
			if (e.Date <= DateTime.Now.Date) {
				Toast.MakeText (this, Resources.GetString (Resource.String.expirationDateError), ToastLength.Short).Show ();
				customDate = DateTime.Today.AddDays (1);
			} else
				customDate = e.Date;

			selectedTime = (int) (customDate - DateTime.Now).TotalMinutes;
			textDate.Text = customDate.ToShortDateString ();
			textDate.Visibility = ViewStates.Visible;
		}
	}
}

