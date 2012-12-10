using System;

namespace TrackMeServer
{
	[DataType ("tracker")]
	public class TrackerData
	{
		public TrackerData ()
		{
		}

		[DataMember ("trackerId", Key=true)]
		public string TrackerId { get; set; }

		[DataMember ("sharePrivateId")]
		public string SharePrivateId { get; set; }

		[DataMember ("userId")]
		public string UserId { get; set; }

		[DataMember ("userRegId")]
		public string UserRegistrationId { get; set; }

		[DataMember ("userName")]
		public string UserName { get; set; }

		[DataMember ("userIcon")]
		public byte[] UserIcon { get; set; }
	}
}

