using System;
using Dokdo;
using Dokdo.Web;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CloudSave : MonoBehaviour
{
	public GameObject OffTarget;

	public Texture2D Screenshot;

	public string TempData;

	private GameManager GM;

	public void Save()
	{
		try
		{
			DataToString();
			string saveData = TempData;
#if UNITY_WEBGL && !UNITY_EDITOR
			BrowserClipboardRuntime.Copy(saveData,
				delegate
				{
					ShowMessage("COPY SAVE", "Save copied to clipboard.");
				},
				delegate(string reason)
				{
					BrowserClipboardRuntime.ShowManualCopy(saveData);
					ShowMessage("COPY SAVE",
						"Clipboard unavailable. Use the browser copy panel.");
				});
#else
			GUIUtility.systemCopyBuffer = TempData;
			ShowMessage("COPY SAVE", "Save copied to clipboard.");
#endif
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			ShowMessage("COPY SAVE", "Failed");
		}
		finally
		{
			TempData = string.Empty;
		}
	}

	public void AskSave()
	{
		MNPopup popup = new MNPopup(
			"COPY SAVE", "Copy the current save to the clipboard?");
		popup.AddAction("No", delegate { });
		popup.AddAction("Yes", Save);
		popup.Show();
	}

	public void Load()
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		BrowserClipboardRuntime.Paste(ConfirmLoad,
			delegate(string reason)
			{
				BrowserClipboardRuntime.RequestManualPaste(ConfirmLoad);
				ShowMessage("PASTE SAVE",
					"Clipboard unavailable. Use the browser paste panel.");
			});
#else
		string clipboard = GUIUtility.systemCopyBuffer;
		ConfirmLoad(clipboard);
#endif
	}

	private void ConfirmLoad(string clipboard)
	{
		if (string.IsNullOrWhiteSpace(clipboard))
		{
			ShowMessage("PASTE SAVE", "Clipboard is empty.");
			return;
		}

		MNPopup popup = new MNPopup(
			"PASTE SAVE", "Replace local progress with the clipboard save?");
		popup.AddAction("No", delegate { });
		popup.AddAction("Yes", delegate { StringToData(clipboard); });
		popup.Show();
	}

	public void DataToString()
	{
		TempData = GM.ExportSave();
	}

	public void StringToData(string data)
	{
		if (!GM.ImportSave(data, out string error))
		{
			ShowMessage("PASTE SAVE", error);
			return;
		}

		if (LocalizeManager.Instance != null)
		{
			LocalizeManager.Instance.init();
		}
		TempData = string.Empty;

		MNPopup popup = new MNPopup("PASTE SAVE", "COMPLETE");
		popup.AddAction("Ok", delegate
		{
			SceneManager.LoadScene(SceneIds.Intro);
		});
		popup.Show();
	}

	private static void ShowMessage(string title, string message)
	{
		MNPopup popup = new MNPopup(title, message);
		popup.AddAction("Ok", delegate { });
		popup.Show();
	}

	private void Awake()
	{
		GM = GameManager.Instance;
	}
}
