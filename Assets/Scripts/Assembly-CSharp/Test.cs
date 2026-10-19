using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
	private GameManager GM;

	public Text lv;

	private Transform lvt;

	private void Start()
	{
		GM = GameManager.Instance;
		lvt = lv.transform;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			up();
		}
		if (Input.GetKeyDown(KeyCode.D))
		{
			reset();
		}
	}

	private void reset()
	{
		GM.Level_Body = 1;
		GM.Level_Sail = 1;
		GM.Level_Steering = 1;
		GM.Seagull_Lv[0] = 1;
		lv.text = "Lv." + GM.Level_Sail;
		lvt.DOKill();
		lvt.localScale = Vector3.one;
		lvt.DOScale(Vector3.one * 1.2f, 0.3f).From().SetEase(Ease.InBack);
		EventManager.TriggerEvent(MyEvent.Upgrade);
		EventManager.TriggerEvent(MyEvent.AssiUpdate);
	}

	private void up()
	{
		Debug.Log("LvUP");
		if ((int)GM.Level_Body * 5 <= (int)GM.Level_Sail)
		{
			if ((int)GM.Level_Body < 10)
			{
				GameManager gM = GM;
				gM.Level_Body = (int)gM.Level_Body + 1;
			}
		}
		else
		{
			GameManager gM2 = GM;
			gM2.Level_Sail = (int)gM2.Level_Sail + 1;
			GameManager gM3 = GM;
			gM3.Level_Steering = (int)gM3.Level_Steering + 1;
			ObscuredInt[] seagull_Lv = GM.Seagull_Lv;
			int num = 0;
			seagull_Lv[num] = (int)seagull_Lv[num] + 1;
		}
		lv.text = "Lv." + GM.Level_Sail;
		lvt.DOKill();
		lvt.localScale = Vector3.one;
		lvt.DOScale(Vector3.one * 1.2f, 0.3f).From().SetEase(Ease.InBack);
		GM.popSound();
		EventManager.TriggerEvent(MyEvent.Upgrade);
		EventManager.TriggerEvent(MyEvent.AssiUpdate);
	}
}
