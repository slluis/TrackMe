
using System;
using System.Web;
using System.Web.UI;

namespace TrackMeServer
{
	public class Target : System.Web.IHttpHandler
	{
		public virtual bool IsReusable {
			get {
				return false;
			}
		}
		
		public virtual void ProcessRequest (HttpContext context)
		{
			context.Response.ContentType = "target/xamarin.trackme";
			context.Response.Write (context.Request.QueryString ["id"]);
			context.Response.End ();
		}
	}
}

