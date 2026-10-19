using EZObjectPools;
using UnityEngine;

[AddComponentMenu("EZ Object Pool/Pooled Objects/Particle System Disable")]
public class ParticleSystemDisable : PooledObject
{
	public ParticleSystem Particles;

	private void Awake()
	{
		if (Particles == null)
		{
			Particles = GetComponentInChildren<ParticleSystem>();
		}
	}

	private void OnEnable()
	{
		if (Particles == null)
		{
			Debug.LogError("ParticleSystemDisable " + base.gameObject.name + " could not find any particle systems!");
		}
	}

	private void Update()
	{
		if (!Particles.IsAlive())
		{
			base.transform.parent = ParentPool.transform;
			base.gameObject.SetActive(false);
		}
	}
}
