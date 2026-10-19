using UnityEngine;

public class GameCenter : MonoBehaviour
{
	public void OpenLeader()
	{
		ShowUnavailable();
	}

	public void OpenAchieve()
	{
		ShowUnavailable();
	}

	private static void ShowUnavailable()
	{
		MNPopup popup = new MNPopup("SYSTEM", "Online services unavailable.");
		popup.AddAction("Ok", delegate { });
		popup.Show();
	}
}
