using UnityEngine;

namespace EZObjectPools
{
	[AddComponentMenu("EZ Object Pools/Pooled Object")]
	public class PooledObject : MonoBehaviour
	{
		[HideInInspector]
		public EZObjectPool ParentPool;

		public virtual void Disable()
		{
			base.gameObject.SetActive(false);
		}

		private void OnDisable()
		{
			base.transform.position = Vector3.zero;
			if ((bool)ParentPool)
			{
				ParentPool.AddToAvailableObjects(base.gameObject);
			}
			else
			{
				Debug.LogWarning("PooledObject " + base.gameObject.name + " does not have a parent pool. If this occurred during a scene transition, ignore this. Otherwise reoprt to developer.");
			}
		}
	}
}
