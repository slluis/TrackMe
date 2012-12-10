using System;

namespace TrackMeServer
{
	[DataType("locationshare")]
	public class LocationShareData
	{
		public LocationShareData ()
		{
		}

		[DataMember ("privateId", Key=true)]
		public string PrivateId { get; set; }

		[DataMember ("publicId")]
		public string PublicId { get; set; }

		[DataMember ("longitude")]
		public float Longitude { get; set; }

		[DataMember ("latitude")]
		public float Latitude { get; set; }

		[DataMember ("userId")]
		public string UserId { get; set; }
		
		[DataMember ("userRegId")]
		public string UserRegistrationId { get; set; }
		
		[DataMember ("userName")]
		public string UserName { get; set; }
		
		[DataMember ("userIcon")]
		public byte[] UserIcon { get; set; }

		[DataMember ("timestamp")]
		public DateTime SharedTime { get; set; }

		[DataMember ("expireTime")]
		public DateTime ExpireTime { get; set; }
		
		[DataMember ("expireOnArrival")]
		public bool ExpireOnArrival { get; set; }
	}
}

