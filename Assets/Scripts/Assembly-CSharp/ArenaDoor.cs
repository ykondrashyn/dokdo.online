using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ArenaDoor : MonoBehaviour
{
	public GameObject[] entTrig;

	public Transform[] doors;

	public Transform EnterPos;

	private ShipController SC;

	public GameObject Fade;

	public Transform hud;

	private GameManager GM;

	public GenArena Genarena;

	public GameObject killLabel;

	public void Start()
	{
		GM = GameManager.Instance;
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
	}

	public void Enter()
	{
		StartCoroutine(open());
	}

	private IEnumerator open()
	{
		GM.CurrentArenaKill = 0;
		SC.isArena = true;
		hud.gameObject.SetActive(true);
		killLabel.SetActive(false);
		killLabel.GetComponent<Text>().text = "0 KILL";
		Fade.SetActive(true);
		SC.SetPosMove2(EnterPos);
		hud.DOKill();
		hud.DOScale(Vector3.one * 3f, 0f);
		hud.DOLocalMoveY(-400f, 0f);
		for (int i = 0; i < entTrig.Length; i++)
		{
			entTrig[i].SetActive(false);
		}
		for (int j = 0; j < doors.Length; j++)
		{
			doors[j].DOLocalMoveY(0f, 2f);
		}
		yield return new WaitForSeconds(1f);
		Genarena.init();
		hud.DOScale(Vector3.one, 0.5f);
		hud.DOLocalMoveY(0f, 0.5f);
		yield return new WaitForSeconds(1f);
		killLabel.SetActive(true);
	}

	public void Over()
	{
		for (int i = 0; i < entTrig.Length; i++)
		{
			entTrig[i].SetActive(true);
		}
		for (int j = 0; j < doors.Length; j++)
		{
			doors[j].DOLocalMoveY(-20f, 0f);
		}
	}
}
