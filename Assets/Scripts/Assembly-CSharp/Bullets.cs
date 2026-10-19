using EZObjectPools;
using UnityEngine;

public class Bullets : MonoBehaviour
{
	private Transform myT;

	private bool isdone;

	private EZObjectPool fxpool;

	private EZObjectPool boompool;

	private Rigidbody rigid;

	private MeshRenderer mesh;

	public bool isEnemy;

	private float localT;

	public int atk;

	private void Awake()
	{
		myT = base.transform;
		fxpool = GameObject.Find("drop Effect").GetComponent<EZObjectPool>();
		boompool = GameObject.Find("boom Effect").GetComponent<EZObjectPool>();
		rigid = GetComponent<Rigidbody>();
		mesh = GetComponent<MeshRenderer>();
	}

	private void OnEnable()
	{
		mesh.enabled = true;
		rigid.useGravity = true;
		isdone = false;
		localT = 0.3f;
	}

	private void Update()
	{
		localT -= Time.deltaTime;
		if (myT.position.y <= 0f && !isdone && localT <= 0f)
		{
			isdone = true;
			OnWater();
		}
	}

	private void OnWater()
	{
		Vector3 position = myT.position;
		position.y = 0f;
		rigid.useGravity = false;
		rigid.velocity = Vector3.zero;
		fxpool.TryGetNextObject(position, Quaternion.identity);
		mesh.enabled = false;
		Invoke("setOff", 4f);
	}

	private void setOff()
	{
		base.gameObject.SetActive(false);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (isEnemy)
		{
			if (other.CompareTag("Player") && !isdone)
			{
				isdone = true;
				Disappear();
				other.GetComponentInParent<ShipController>().Hit(atk);
			}
		}
		else if (other.CompareTag("Enemy") && !isdone)
		{
			isdone = true;
			other.GetComponentInParent<Enemy_Ship>().Hit(atk);
			Disappear();
		}
	}

	private void Disappear()
	{
		rigid.useGravity = false;
		rigid.velocity = Vector3.zero;
		boompool.TryGetNextObject(myT.position, Quaternion.identity);
		mesh.enabled = false;
		Invoke("setOff", 4f);
	}
}
