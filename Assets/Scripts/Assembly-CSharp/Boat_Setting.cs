using DG.Tweening;
using UnityEngine;

public class Boat_Setting : MonoBehaviour
{
	public SailSetting[] SettingList;

	public Transform[] Sails1;

	public Transform[] Sails2;

	public Transform[] Sails3;

	public Transform[] Sails4;

	public Transform[] Heads;

	public Transform SailParent1;

	public Transform SailParent2;

	public Transform SailParent3;

	public Transform SailParent4;

	public Transform HeadParent;

	public GameObject[] Bodys;

	public Transform[] HeadPos;

	public Transform[] SailPos1;

	public Transform[] SailPos2;

	public Transform[] SailPos3;

	public Transform[] SailPos4;

	private int b;

	private int h;

	private int s1;

	private int s2;

	private int s3;

	private int s4;

	private Material Amblem;

	private GameManager GM;

	private ShipController SC;

	private Enemy_Ship ES;

	private Assistant AS;

	private void Awake()
	{
		GM = GameManager.Instance;
		if ((bool)GetComponentInParent<ShipController>())
		{
			SC = GetComponentInParent<ShipController>();
		}
		else if ((bool)GetComponentInParent<Enemy_Ship>())
		{
			ES = GetComponentInParent<Enemy_Ship>();
		}
		else if ((bool)GetComponentInParent<Assistant>())
		{
			AS = GetComponentInParent<Assistant>();
		}
	}

	public void init(int Sail, int Head, int Body)
	{
		for (int i = 0; i < Bodys.Length; i++)
		{
			Bodys[i].SetActive(false);
		}
		Bodys[Body].SetActive(true);
		if (b != Body)
		{
			Bodys[Body].transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);
		}
		b = Body;
		for (int j = 0; j < Sails1.Length; j++)
		{
			Sails1[j].SetParent(SailParent1);
		}
		for (int k = 0; k < Sails2.Length; k++)
		{
			Sails2[k].SetParent(SailParent2);
		}
		for (int l = 0; l < Sails3.Length; l++)
		{
			Sails3[l].SetParent(SailParent3);
		}
		for (int m = 0; m < Sails4.Length; m++)
		{
			Sails4[m].SetParent(SailParent4);
		}
		for (int n = 0; n < Heads.Length; n++)
		{
			Heads[n].SetParent(HeadParent);
		}
		Heads[Head].SetParent(HeadPos[Body]);
		Heads[Head].gameObject.SetActive(true);
		Heads[Head].localPosition = Vector3.zero;
		Heads[Head].localScale = Vector3.one;
		if (h != Head)
		{
			Heads[Head].DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
		}
		h = Head;
		if (SC != null)
		{
			Amblem = GM.Sail_Flag_Player[(int)GM.MyAmblemN];
		}
		else if (ES != null)
		{
			ES.HEAD = HeadPos[Body];
			if (ES.isPirate)
			{
				int num = 0;
				do
				{
					num = Random.Range(0, GM.Sail_Flag_Player.Length);
				}
				while (num.Equals(GM.MyAmblemN));
				Amblem = GM.Sail_Flag_Player[num];
				if (Sail >= 49)
				{
					Amblem = GM.Sail_Flag_Player[5];
				}
			}
			else
			{
				int num2 = GM.AmblemNumber(ES.HomeN);
				Amblem = GM.Sail_Flag_Enemy[num2];
			}
		}
		else if (AS != null)
		{
			Amblem = GM.Sail_Flag_Player[(int)GM.MyAmblemN];
		}
		if (SettingList[Sail].pos1 != 0)
		{
			int num3 = SettingList[Sail].pos1 - 1;
			Sails1[num3].SetParent(SailPos1[Body]);
			Sails1[num3].GetComponent<MeshRenderer>().material = Amblem;
			Sails1[num3].gameObject.SetActive(true);
			Sails1[num3].localPosition = Vector3.zero;
			Sails1[num3].localEulerAngles = Vector3.zero;
			Sails1[num3].localScale = Vector3.one;
			if (s1 != SettingList[Sail].pos1)
			{
				Sails1[num3].DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
			}
			s1 = SettingList[Sail].pos1;
		}
		if (SettingList[Sail].pos2 != 0)
		{
			int num4 = SettingList[Sail].pos2 - 1;
			Sails2[num4].SetParent(SailPos2[Body]);
			Sails2[num4].GetComponent<MeshRenderer>().material = Amblem;
			Sails2[num4].gameObject.SetActive(true);
			Sails2[num4].localPosition = Vector3.zero;
			Sails2[num4].localEulerAngles = Vector3.zero;
			Sails2[num4].localScale = Vector3.one;
			if (s2 != SettingList[Sail].pos2)
			{
				Sails2[num4].DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
			}
			s2 = SettingList[Sail].pos2;
		}
		if (SettingList[Sail].pos3 != 0)
		{
			int num5 = SettingList[Sail].pos3 - 1;
			Sails3[num5].GetComponent<MeshRenderer>().material = Amblem;
			Sails3[num5].SetParent(SailPos3[Body]);
			Sails3[num5].gameObject.SetActive(true);
			Sails3[num5].localPosition = Vector3.zero;
			Sails3[num5].localEulerAngles = Vector3.zero;
			Sails3[num5].localScale = Vector3.one;
			if (s3 != SettingList[Sail].pos3)
			{
				Sails3[num5].DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
			}
			s3 = SettingList[Sail].pos3;
		}
		if (SettingList[Sail].pos4 != 0)
		{
			int num6 = SettingList[Sail].pos4 - 1;
			Sails4[num6].GetComponent<MeshRenderer>().material = Amblem;
			Sails4[num6].SetParent(SailPos4[Body]);
			Sails4[num6].gameObject.SetActive(true);
			Sails4[num6].localPosition = Vector3.zero;
			Sails4[num6].localEulerAngles = Vector3.zero;
			Sails4[num6].localScale = Vector3.one;
			if (s4 != SettingList[Sail].pos4)
			{
				Sails4[num6].DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
			}
			s4 = SettingList[Sail].pos4;
		}
	}
}
