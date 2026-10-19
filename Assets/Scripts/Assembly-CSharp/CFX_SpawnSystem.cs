using System.Collections.Generic;
using UnityEngine;

public class CFX_SpawnSystem : MonoBehaviour
{
	private static CFX_SpawnSystem instance;

	public GameObject[] objectsToPreload = new GameObject[0];

	public int[] objectsToPreloadTimes = new int[0];

	public bool hideObjectsInHierarchy;

	public bool spawnAsChildren = true;

	public bool onlyGetInactiveObjects;

	public bool instantiateIfNeeded;

	private bool allObjectsLoaded;

	private Dictionary<int, List<GameObject>> instantiatedObjects = new Dictionary<int, List<GameObject>>();

	private Dictionary<int, int> poolCursors = new Dictionary<int, int>();

	public static bool AllObjectsLoaded
	{
		get
		{
			return instance.allObjectsLoaded;
		}
	}

	public static GameObject GetNextObject(GameObject sourceObj, bool activateObject = true)
	{
		int instanceID = sourceObj.GetInstanceID();
		if (!instance.poolCursors.ContainsKey(instanceID))
		{
			Debug.LogError("[CFX_SpawnSystem.GetNextObject()] Object hasn't been preloaded: " + sourceObj.name + " (ID:" + instanceID + ")\n", instance);
			return null;
		}
		int num = instance.poolCursors[instanceID];
		GameObject gameObject = null;
		if (instance.onlyGetInactiveObjects)
		{
			int num2 = num;
			while (true)
			{
				gameObject = instance.instantiatedObjects[instanceID][num];
				instance.increasePoolCursor(instanceID);
				num = instance.poolCursors[instanceID];
				if (gameObject != null && !gameObject.activeSelf)
				{
					break;
				}
				if (num == num2)
				{
					if (instance.instantiateIfNeeded)
					{
						Debug.Log("[CFX_SpawnSystem.GetNextObject()] A new instance has been created for \"" + sourceObj.name + "\" because no active instance were found in the pool.\n", instance);
						PreloadObject(sourceObj);
						List<GameObject> list = instance.instantiatedObjects[instanceID];
						gameObject = list[list.Count - 1];
						break;
					}
					Debug.LogWarning("[CFX_SpawnSystem.GetNextObject()] There are no active instances available in the pool for \"" + sourceObj.name + "\"\nYou may need to increase the preloaded object count for this prefab?", instance);
					return null;
				}
			}
		}
		else
		{
			gameObject = instance.instantiatedObjects[instanceID][num];
			instance.increasePoolCursor(instanceID);
		}
		if (activateObject && gameObject != null)
		{
			gameObject.SetActive(true);
		}
		return gameObject;
	}

	public static void PreloadObject(GameObject sourceObj, int poolSize = 1)
	{
		instance.addObjectToPool(sourceObj, poolSize);
	}

	public static void UnloadObjects(GameObject sourceObj)
	{
		instance.removeObjectsFromPool(sourceObj);
	}

	private void addObjectToPool(GameObject sourceObject, int number)
	{
		int instanceID = sourceObject.GetInstanceID();
		if (!instantiatedObjects.ContainsKey(instanceID))
		{
			instantiatedObjects.Add(instanceID, new List<GameObject>());
			poolCursors.Add(instanceID, 0);
		}
		for (int i = 0; i < number; i++)
		{
			GameObject gameObject = Object.Instantiate(sourceObject);
			gameObject.SetActive(false);
			CFX_AutoDestructShuriken[] componentsInChildren = gameObject.GetComponentsInChildren<CFX_AutoDestructShuriken>(true);
			CFX_AutoDestructShuriken[] array = componentsInChildren;
			foreach (CFX_AutoDestructShuriken cFX_AutoDestructShuriken in array)
			{
				cFX_AutoDestructShuriken.OnlyDeactivate = true;
			}
			CFX_LightIntensityFade[] componentsInChildren2 = gameObject.GetComponentsInChildren<CFX_LightIntensityFade>(true);
			CFX_LightIntensityFade[] array2 = componentsInChildren2;
			foreach (CFX_LightIntensityFade cFX_LightIntensityFade in array2)
			{
				cFX_LightIntensityFade.autodestruct = false;
			}
			instantiatedObjects[instanceID].Add(gameObject);
			if (hideObjectsInHierarchy)
			{
				gameObject.hideFlags = HideFlags.HideInHierarchy;
			}
			if (spawnAsChildren)
			{
				gameObject.transform.parent = base.transform;
			}
		}
	}

	private void removeObjectsFromPool(GameObject sourceObject)
	{
		int instanceID = sourceObject.GetInstanceID();
		if (!instantiatedObjects.ContainsKey(instanceID))
		{
			Debug.LogWarning("[CFX_SpawnSystem.removeObjectsFromPool()] There aren't any preloaded object for: " + sourceObject.name + " (ID:" + instanceID + ")\n", base.gameObject);
			return;
		}
		for (int num = instantiatedObjects[instanceID].Count - 1; num >= 0; num--)
		{
			GameObject obj = instantiatedObjects[instanceID][num];
			instantiatedObjects[instanceID].RemoveAt(num);
			Object.Destroy(obj);
		}
		instantiatedObjects.Remove(instanceID);
		poolCursors.Remove(instanceID);
	}

	private void increasePoolCursor(int uniqueId)
	{
		instance.poolCursors[uniqueId]++;
		if (instance.poolCursors[uniqueId] >= instance.instantiatedObjects[uniqueId].Count)
		{
			instance.poolCursors[uniqueId] = 0;
		}
	}

	private void Awake()
	{
		if (instance != null)
		{
			Debug.LogWarning("CFX_SpawnSystem: There should only be one instance of CFX_SpawnSystem per Scene!\n", base.gameObject);
		}
		instance = this;
	}

	private void Start()
	{
		allObjectsLoaded = false;
		for (int i = 0; i < objectsToPreload.Length; i++)
		{
			PreloadObject(objectsToPreload[i], objectsToPreloadTimes[i]);
		}
		allObjectsLoaded = true;
	}
}
