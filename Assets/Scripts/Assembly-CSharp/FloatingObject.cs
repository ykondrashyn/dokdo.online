using System.Collections.Generic;
using DG.Tweening;
using EZObjectPools;
using UnityEngine;

public class FloatingObject : MonoBehaviour
{
	private Transform myT;

	private Vector3 TempPos;

	private Vector3 tempRot;

	private float Distance;

	private Transform myShip;

	private bool isdone;

	private EZObjectPool fxpool;

	private float DisappearTime = 60f;

	private Vector3 tempPos;

	private float localT;

	private List<Item> items = new List<Item>();

	public bool isAds;

	private ShipController SC;

	private float moveT = 0.5f;

	private void Awake()
	{
		myT = base.transform;
		tempRot = new Vector3(360f, 360f, 0f);
		myShip = GameObject.Find("MyPos").transform;
		fxpool = GameObject.Find("float Effect").GetComponent<EZObjectPool>();
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
	}

	private void Update()
	{
		Distance = Vector3.Distance(myT.position, myShip.position);
		localT -= Time.deltaTime;
		if (localT <= 0f && !isdone)
		{
			isdone = true;
			myT.DOScale(Vector3.zero, 1f).OnComplete(delegate
			{
				base.gameObject.SetActive(false);
			});
		}
		if (Distance > 60f)
		{
			base.gameObject.SetActive(false);
		}
	}

	private void OnEnable()
	{
		tempPos = myT.position;
		tempPos.y = 0f;
		localT = DisappearTime;
		isdone = false;
		myT.DOKill();
		myT.localScale = Vector3.one;
		myT.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5, 0f);
		myT.localRotation = Quaternion.Euler(Vector3.zero);
		TempPos = myT.localPosition;
		TempPos.y = 0f;
		myT.localPosition = TempPos;
		int num = Random.Range(20, 30);
		float duration = Random.Range(1f, 2f);
		myT.DOLocalMoveX(Random.Range(-3f, 3f), 0.5f).SetRelative();
		myT.DOLocalMoveZ(Random.Range(-3f, 3f), 0.5f).SetRelative();
		myT.DOLocalMoveY(Random.Range(1f, 2f), 0.25f).SetEase(Ease.OutCirc);
		myT.DOLocalMoveY(0f, 0.25f).SetEase(Ease.InCirc).SetDelay(0.25f);
		myT.DOLocalMoveY(-0.2f, duration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
			.SetDelay(0.5f);
		myT.DOLocalRotate(tempRot, num, RotateMode.LocalAxisAdd).SetLoops(-1, LoopType.Restart);
	}

	public void SetItem(List<Item> reward)
	{
		items.Clear();
		for (int i = 0; i < reward.Count; i++)
		{
			Item item = new Item();
			item.type = reward[i].type;
			item.value = reward[i].value;
			items.Add(item);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!isdone)
		{
			if (!isAds && other.CompareTag("pick"))
			{
				isdone = true;
				Invoke("Get", 1.5f);
				Invoke("delayEffect", 1.5f);
			}
			else if (other.CompareTag("Player") || other.CompareTag("pet"))
			{
				isdone = true;
				Get();
			}
		}
	}

	private void delayEffect()
	{
		EventManager.TriggerEvent(MyEvent.pickup);
	}

	private void Get()
	{
		fxpool.TryGetNextObject(tempPos, Quaternion.identity);
		myT.DOKill();
		myT.DOLocalRotate(tempRot, 1f, RotateMode.LocalAxisAdd).SetLoops(-1, LoopType.Restart);
		myT.DOMoveX(myShip.position.x, moveT).SetEase(Ease.InCubic);
		myT.DOMoveZ(myShip.position.z, moveT).SetEase(Ease.InCubic);
		myT.DOMoveY(3f, moveT * 0.5f).SetEase(Ease.OutCubic);
		myT.DOMoveY(0f, moveT * 0.5f).SetDelay(moveT * 0.5f).SetEase(Ease.InCubic);
		myT.DOScale(Vector3.zero, moveT).SetEase(Ease.InCirc).OnComplete(delegate
		{
			base.gameObject.SetActive(false);
		});
		if (isAds)
		{
			GameObject.Find("Panel_Ask").GetComponent<Panel_AskAds>().Show();
		}
		else
		{
			SC.GetItem(items.ToArray());
		}
	}
}
