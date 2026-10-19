using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Island_AssiMenu : MonoBehaviour
{
	public IslandMenu IM;

	public Text CurrentAssiN;

	public Text CurrentSeagullN;

	public Text CurrentNessyN;

	private GameManager GM;

	public GameObject pickobj;

	private UnityAction AssInit;

	private void Awake()
	{
		GM = GameManager.Instance;
		AssInit = init;
	}

	private void OnEnable()
	{
		IM.CamToAssi(true);
		EventManager.StartListening(MyEvent.AssiUpdate, AssInit);
		init();
	}

	private void OnDisable()
	{
		IM.CamToAssi(false);
		EventManager.StopListening(MyEvent.AssiUpdate, AssInit);
	}

	private void init()
	{
		int num = 0;
		for (int i = 0; i < GM.AssistantShip_Lv.Length; i++)
		{
			if ((int)GM.AssistantShip_Lv[i] > 0)
			{
				num++;
			}
		}
		CurrentAssiN.text = num + "/" + 3;
		int num2 = 0;
		for (int j = 0; j < GM.Seagull_Lv.Length; j++)
		{
			if ((int)GM.Seagull_Lv[j] > 0)
			{
				num2++;
			}
		}
		CurrentSeagullN.text = num2 + "/" + 2;
		int num3 = 0;
		if ((int)GM.Nessy_Lv > 0)
		{
			num3 = 1;
		}
		CurrentNessyN.text = num3 + "/" + 1;
		pickobj.SetActive(GM.pickboat);
	}
}
