using System.Collections.Generic;
using UnityEngine;

namespace EZObjectPools
{
	[AddComponentMenu("EZ Object Pool/Object Pool")]
	public class EZObjectPool : MonoBehaviour
	{
		private static Dictionary<string, EZObjectPool> SharedPools = new Dictionary<string, EZObjectPool>();

		private static GameObject Marker;

		public GameObject Template;

		public string PoolName;

		public List<GameObject> ObjectList;

		public bool AutoResize;

		public int PoolSize = 100;

		public int MaxPoolSize = 256;

		public bool InstantiateOnAwake;

		public bool Shared;

		private List<GameObject> AvailableObjects;

		public static EZObjectPool CreateObjectPool(GameObject template, string name, int size, bool autoResize, bool instantiateImmediate, bool shared)
		{
			if (Marker == null)
			{
				Marker = new GameObject("EZ Object Pools Container");
				SharedPools.Clear();
			}
			if (shared)
			{
				if (SharedPools.ContainsKey(name))
				{
					return SharedPools[name];
				}
				GameObject gameObject = new GameObject(name);
				EZObjectPool eZObjectPool = gameObject.AddComponent<EZObjectPool>();
				eZObjectPool.InstantiateOnAwake = false;
				eZObjectPool.SetProperties(template, size, name, autoResize);
				SharedPools.Add(name, eZObjectPool);
				if (instantiateImmediate)
				{
					eZObjectPool.InstantiatePool();
				}
				gameObject.transform.SetParent(Marker.transform);
				return eZObjectPool;
			}
			GameObject gameObject2 = new GameObject(name);
			EZObjectPool eZObjectPool2 = gameObject2.AddComponent<EZObjectPool>();
			eZObjectPool2.InstantiateOnAwake = false;
			eZObjectPool2.SetProperties(template, size, name, autoResize);
			if (instantiateImmediate)
			{
				eZObjectPool2.InstantiatePool();
			}
			gameObject2.transform.SetParent(Marker.transform);
			return eZObjectPool2;
		}

		private void Awake()
		{
			if (Marker == null)
			{
				Marker = new GameObject("EZ Object Pools Container");
				SharedPools.Clear();
			}
			if (InstantiateOnAwake)
			{
				ObjectList = new List<GameObject>(PoolSize);
				AvailableObjects = new List<GameObject>(PoolSize);
				InstantiatePool();
			}
			if (Shared)
			{
				SharedPools.Add(PoolName, this);
			}
		}

		public void SetProperties(GameObject objectTemplate, int size, string name, bool autoResize)
		{
			Template = objectTemplate;
			PoolSize = size;
			ObjectList = new List<GameObject>(size);
			AvailableObjects = new List<GameObject>(size);
			PoolName = name;
			AutoResize = autoResize;
			MaxPoolSize = Mathf.Max(size, 256);
		}

		public void InstantiatePool()
		{
			if (Template == null)
			{
				Debug.LogError("EZ Object Pool: " + base.name + ": Template GameObject is null! Make sure you assigned a template either in the inspector or in your scripts.");
				return;
			}
			ClearPool();
			for (int i = 0; i < PoolSize; i++)
			{
				GameObject gameObject = NewActiveObject();
				ObjectList.Add(gameObject);
				gameObject.SetActive(false);
				AddToAvailableObjects(gameObject);
			}
		}

		public bool TryGetNextObject(Vector3 pos, Quaternion rot, out GameObject obj)
		{
			if (ObjectList.Count == 0)
			{
				Debug.LogError("EZ Object Pool " + PoolName + ", the pool has not been instantiated but you are trying to retrieve an object!");
			}
			int index = AvailableObjects.Count - 1;
			if (AvailableObjects.Count > 0)
			{
				if (AvailableObjects[index] == null)
				{
					Debug.LogError("EZObjectPool " + PoolName + " has missing objects in its pool! Are you accidentally destroying any GameObjects retrieved from the pool?");
					obj = null;
					return false;
				}
				AvailableObjects[index].transform.position = pos;
				AvailableObjects[index].transform.rotation = rot;
				AvailableObjects[index].SetActive(true);
				obj = AvailableObjects[index];
				AvailableObjects.RemoveAt(index);
				return true;
			}
			if (AutoResize && ObjectList.Count < EffectiveMaxPoolSize)
			{
				GameObject gameObject = NewActiveObject();
				gameObject.transform.position = pos;
				gameObject.transform.rotation = rot;
				ObjectList.Add(gameObject);
				obj = gameObject;
				return true;
			}
			obj = null;
			return false;
		}

		public void TryGetNextObject(Vector3 pos, Quaternion rot)
		{
			GameObject ignored;
			TryGetNextObject(pos, rot, out ignored);
		}

		public int EffectiveMaxPoolSize
		{
			get
			{
				if (!AutoResize)
				{
					return PoolSize;
				}
				return Mathf.Max(PoolSize, MaxPoolSize > 0 ? MaxPoolSize : 256);
			}
		}

		private GameObject NewActiveObject()
		{
			GameObject gameObject = Object.Instantiate(Template);
			gameObject.transform.SetParent(base.transform);
			PooledObject component = gameObject.GetComponent<PooledObject>();
			if ((bool)component)
			{
				component.ParentPool = this;
			}
			else
			{
				gameObject.AddComponent<PooledObject>().ParentPool = this;
			}
			return gameObject;
		}

		public void ClearPool()
		{
			for (int i = 0; i < ObjectList.Count; i++)
			{
				Object.Destroy(ObjectList[i]);
			}
			ObjectList.Clear();
			AvailableObjects.Clear();
		}

		public void DeletePool(bool deleteActiveObjects)
		{
			for (int i = 0; i < ObjectList.Count; i++)
			{
				if (!ObjectList[i].activeInHierarchy || (ObjectList[i].activeInHierarchy && deleteActiveObjects))
				{
					Object.Destroy(ObjectList[i]);
				}
			}
		}

		public void AddToAvailableObjects(GameObject obj)
		{
			if (obj != null && ObjectList.Contains(obj) &&
				!AvailableObjects.Contains(obj))
			{
				AvailableObjects.Add(obj);
			}
		}

		public int ActiveObjectCount()
		{
			return ObjectList.Count - AvailableObjects.Count;
		}

		public int AvailableObjectCount()
		{
			return AvailableObjects.Count;
		}
	}
}
