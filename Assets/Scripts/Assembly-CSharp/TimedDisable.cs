using EZObjectPools;
using UnityEngine;

[AddComponentMenu("EZ Object Pools/Pooled Objects/Timed Disable")]
public class TimedDisable : PooledObject
{
	private float timer;

	public float DisableTime;

	private void OnEnable()
	{
		timer = 0f;
	}

	private void Update()
	{
		timer += Time.deltaTime;
		if (timer > DisableTime)
		{
			base.transform.parent = ParentPool.transform;
			base.gameObject.SetActive(false);
		}
	}
}
