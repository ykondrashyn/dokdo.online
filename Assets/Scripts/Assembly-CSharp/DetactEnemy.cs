using UnityEngine;

public class DetactEnemy : MonoBehaviour
{
	public string Tag;

	private ShipController SC;

	public bool isEnemy;

	private void OnEnable()
	{
		if (!isEnemy)
		{
			SC = GetComponentInParent<ShipController>();
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.CompareTag(Tag) && !isEnemy && !other.GetComponentInParent<Enemy_Ship>().isDead)
		{
			Transform hEAD = other.GetComponentInParent<Enemy_Ship>().HEAD;
			if (!SC.NearEnemys.Contains(hEAD))
			{
				SC.NearEnemys.Add(hEAD);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag(Tag) && !isEnemy)
		{
			Transform hEAD = other.GetComponentInParent<Enemy_Ship>().HEAD;
			if (SC.NearEnemys.Contains(hEAD))
			{
				SC.NearEnemys.Remove(hEAD);
			}
		}
	}
}
