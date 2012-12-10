
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

namespace Xamarin.TrackMe
{
	[Activity (Label = "SharesListActivity")]			
	public class SharesListActivity : ListActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			TrackMeApp.SharesChanged += HandleSharesChanged;
		}

		void HandleSharesChanged (object sender, EventArgs e)
		{
			RunOnUiThread (delegate {
				ListAdapter = new SharesListAdapter ();
			});
		}

		protected override void OnResume ()
		{
			base.OnResume ();
			ListAdapter = new SharesListAdapter ();
		}

		protected override void OnPause ()
		{
			base.OnStop ();
			TrackMeApp.SharesChanged -= HandleSharesChanged;
		}
	}

	class SharesListAdapter: BaseAdapter<LocationShare>
	{
		LocationShare[] shares;

		public SharesListAdapter ()
		{
			shares = TrackMeApp.GetShares ();
		}

		#region implemented abstract members of BaseAdapter
		public override long GetItemId (int position)
		{
			return position;
		}

		public override View GetView (int position, View convertView, ViewGroup parent)
		{
			var s = shares [position];
			var t = new TextView (parent.Context);
			var nt = s.Trackers != null ? s.Trackers.Length : 0;
			t.Text = s.SharedTime + " - expires " + s.ExpireTime + " - " + nt + " trackers";

			return t;
		}

		public override int Count {
			get {
				return shares.Length;
			}
		}
		#endregion
		#region implemented abstract members of BaseAdapter
		public override LocationShare this [int position] {
			get {
				return shares [position];
			}
		}
		#endregion
	}
}

