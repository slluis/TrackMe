using System;
using System.Collections.Generic;
using Android.Content;

namespace Xamarin.TrackMe
{
	public class AppNotification
	{
		public static Dictionary<string,Type> notificationTypes = new Dictionary<string, Type> ();

		static AppNotification ()
		{
			notificationTypes ["TargetChanged"] = typeof(TargetChangedNotification);
			notificationTypes ["TrackerAdded"] = typeof(TrackerAddedNotification);
		}

		public static AppNotification ReadNotification (Intent intent)
		{
			var t = intent.GetStringExtra ("Type");
			if (t == null)
				t = intent.GetStringExtra ("event");
			if (t == null)
				return new UnknownNotification ();

			Type type;
			if (!notificationTypes.TryGetValue (t, out type))
				return new UnknownNotification ();

			var not = (AppNotification)Activator.CreateInstance (type);

			foreach (var p in not.GetType ().GetProperties (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)) {
				var arg = (IntentArg) Attribute.GetCustomAttribute (p, typeof(IntentArg));
				if (arg != null) {
					object val = GetValue (intent, arg.Name ?? p.Name, p.PropertyType);
					p.SetValue (not, val, null);
				}
			}
			
			foreach (var p in not.GetType ().GetFields (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)) {
				var arg = (IntentArg) Attribute.GetCustomAttribute (p, typeof(IntentArg));
				if (arg != null) {
					object val = GetValue (intent, arg.Name ?? p.Name, p.FieldType);
					p.SetValue (not, val);
				}
			}
			return not;
		}

		public static void WriteNotification (Intent intent, AppNotification not)
		{
			foreach (var p in not.GetType ().GetProperties (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)) {
				var arg = (IntentArg) Attribute.GetCustomAttribute (p, typeof(IntentArg));
				if (arg != null)
					SetValue (intent, arg.Name ?? p.Name, p.GetValue (not, null));
			}
			
			foreach (var p in not.GetType ().GetFields (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)) {
				var arg = (IntentArg) Attribute.GetCustomAttribute (p, typeof(IntentArg));
				if (arg != null)
					SetValue (intent, arg.Name ?? p.Name, p.GetValue (not));
			}
		}

		static object GetValue (Intent intent, string name, Type type)
		{
			switch (Type.GetTypeCode (type)) {
			case TypeCode.String:
				return intent.GetStringExtra (name);
			case TypeCode.Single:
				return intent.GetFloatExtra (name, 0);
			case TypeCode.Double:
				return intent.GetDoubleExtra (name, 0);
			case TypeCode.Int32:
				return intent.GetIntExtra (name, 0);
			default:
				throw new Exception ("Type not supported");
			}
		}

		static void SetValue (Intent intent, string name, object value)
		{
			if (value == null) {
				intent.PutExtra (name, (string)null);
			}
			switch (Type.GetTypeCode (value.GetType ())) {
			case TypeCode.String:
				intent.PutExtra (name, (string)value);
				break;
			case TypeCode.Single:
				intent.PutExtra (name, (float)value);
				break;
			case TypeCode.Double:
				intent.PutExtra (name, (double)value);
				break;
			case TypeCode.Int32:
				intent.PutExtra (name, (int)value);
				break;
			default:
				throw new Exception ("Type not supported");
			}
		}
	}

	[AttributeUsage (AttributeTargets.Property | AttributeTargets.Field)]
	public class IntentArg: Attribute
	{
		public IntentArg ()
		{
		}

		public IntentArg (string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}

	class UnknownNotification: AppNotification
	{
	}

	class TrackerAddedNotification: AppNotification
	{
		[IntentArg ("shareId")]
		public string ShareId { get; set; }

		[IntentArg ("trackerId")]
		public string TrackerId { get; set; }
	}
	
	class TargetChangedNotification: AppNotification
	{
		[IntentArg ("trackedShareId")]
		public string ShareId { get; set; }

		[IntentArg ("latitude")]
		public float Latitude { get; set; }

		[IntentArg ("longitude")]
		public float Longitude { get; set; }
	}
}

