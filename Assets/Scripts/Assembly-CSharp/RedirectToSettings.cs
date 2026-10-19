using System;
using UnityEngine;

public class RedirectToSettings : MonoBehaviour
{
	public void openSettingsPage()
	{
		try
		{
			using (AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			{
				using (AndroidJavaObject androidJavaObject = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity"))
				{
					using (AndroidJavaObject androidJavaObject2 = new AndroidJavaObject("android.content.Intent", "com.google.android.gms.settings.ADS_PRIVACY"))
					{
						androidJavaObject.Call("startActivityForResult", androidJavaObject2, 0);
					}
				}
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}
}
