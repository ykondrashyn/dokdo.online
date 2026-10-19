using UnityEngine;

public class CFX_InspectorHelp : MonoBehaviour
{
	public bool Locked;

	public string Title;

	public string HelpText;

	public int MsgType;

	[ContextMenu("Unlock editing")]
	private void Unlock()
	{
		Locked = false;
	}
}
