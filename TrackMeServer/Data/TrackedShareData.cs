using System;

namespace TrackMeServer
{
	public class TrackedShareData: LocationShareData
	{
		[DataMember ("trackerId", Key=true)]
		public string TrackerId { get; set; }
	}
}

