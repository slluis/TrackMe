using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Views;
using Xamarin.TrackMe.TrackMeWebService;

namespace Xamarin.TrackMe
{
	[Activity (Label = "TrackList")]
	public class TrackListActivity: ListActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			TrackMeApp.TrackedSharesChanged += HandleSharesChanged;
		}
		
		void HandleSharesChanged (object sender, EventArgs e)
		{
			RunOnUiThread (delegate {
				ListAdapter = new TrackingSharesListAdapter ();
			});
		}
		
		protected override void OnResume ()
		{
			base.OnResume ();
			ListAdapter = new TrackingSharesListAdapter ();
		}
		
		protected override void OnPause ()
		{
			base.OnStop ();
			TrackMeApp.TrackedSharesChanged -= HandleSharesChanged;
		}
	}
	
	class TrackingSharesListAdapter: BaseAdapter<TrackedShare>
	{
		TrackedShare[] shares;
		
		public TrackingSharesListAdapter ()
		{
			shares = TrackMeApp.GetTrackedShares ();
		}
		
		#region implemented abstract members of BaseAdapter
		public override long GetItemId (int position)
		{
			return position;
		}
		
		public override View GetView (int position, View convertView, ViewGroup parent)
		{
			var ctx = parent.Context;

			var share = shares [position];
			var row = new LinearLayout (ctx);
			row.SetPadding (15, 15, 15, 15);

			var img = new ImageView (ctx);
			row.AddView (img);
			img.SetImageDrawable (share.IconDrawable);
			LinearLayout.LayoutParams lp = (LinearLayout.LayoutParams)img.LayoutParameters;
			lp.Width = 60;
			lp.Height = 60;

			var t = new TextView (ctx);
			row.AddView (t);
			t.SetTextAppearance (ctx, Android.Resource.Style.TextAppearanceLarge);
			t.Text = share.User.Name;
			lp = (LinearLayout.LayoutParams)t.LayoutParameters;
			lp.Width = LinearLayout.LayoutParams.WrapContent;
			lp.Height = LinearLayout.LayoutParams.WrapContent;
			lp.Weight = 1.0f;

			return row;
		}
		
		public override int Count {
			get {
				return shares.Length;
			}
		}
		#endregion
		#region implemented abstract members of BaseAdapter
		public override TrackedShare this [int position] {
			get {
				return shares [position];
			}
		}
		#endregion
	}
}

