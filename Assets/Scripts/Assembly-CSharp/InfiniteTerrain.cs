using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
	public GameObject PlayerObject;

	private Terrain[,] _terrainGrid = new Terrain[3, 3];

	private void Start()
	{
		Terrain component = base.gameObject.GetComponent<Terrain>();
		_terrainGrid[0, 0] = Terrain.CreateTerrainGameObject(component.terrainData).GetComponent<Terrain>();
		_terrainGrid[0, 1] = Terrain.CreateTerrainGameObject(component.terrainData).GetComponent<Terrain>();
		_terrainGrid[0, 2] = Terrain.CreateTerrainGameObject(component.terrainData).GetComponent<Terrain>();
		_terrainGrid[1, 0] = Terrain.CreateTerrainGameObject(component.terrainData).GetComponent<Terrain>();
		_terrainGrid[1, 1] = component;
		_terrainGrid[1, 2] = Terrain.CreateTerrainGameObject(component.terrainData).GetComponent<Terrain>();
		_terrainGrid[2, 0] = Terrain.CreateTerrainGameObject(component.terrainData).GetComponent<Terrain>();
		_terrainGrid[2, 1] = Terrain.CreateTerrainGameObject(component.terrainData).GetComponent<Terrain>();
		_terrainGrid[2, 2] = Terrain.CreateTerrainGameObject(component.terrainData).GetComponent<Terrain>();
		UpdateTerrainPositionsAndNeighbors();
	}

	private void UpdateTerrainPositionsAndNeighbors()
	{
		_terrainGrid[0, 0].transform.position = new Vector3(_terrainGrid[1, 1].transform.position.x - _terrainGrid[1, 1].terrainData.size.x, _terrainGrid[1, 1].transform.position.y, _terrainGrid[1, 1].transform.position.z + _terrainGrid[1, 1].terrainData.size.z);
		_terrainGrid[0, 1].transform.position = new Vector3(_terrainGrid[1, 1].transform.position.x - _terrainGrid[1, 1].terrainData.size.x, _terrainGrid[1, 1].transform.position.y, _terrainGrid[1, 1].transform.position.z);
		_terrainGrid[0, 2].transform.position = new Vector3(_terrainGrid[1, 1].transform.position.x - _terrainGrid[1, 1].terrainData.size.x, _terrainGrid[1, 1].transform.position.y, _terrainGrid[1, 1].transform.position.z - _terrainGrid[1, 1].terrainData.size.z);
		_terrainGrid[1, 0].transform.position = new Vector3(_terrainGrid[1, 1].transform.position.x, _terrainGrid[1, 1].transform.position.y, _terrainGrid[1, 1].transform.position.z + _terrainGrid[1, 1].terrainData.size.z);
		_terrainGrid[1, 2].transform.position = new Vector3(_terrainGrid[1, 1].transform.position.x, _terrainGrid[1, 1].transform.position.y, _terrainGrid[1, 1].transform.position.z - _terrainGrid[1, 1].terrainData.size.z);
		_terrainGrid[2, 0].transform.position = new Vector3(_terrainGrid[1, 1].transform.position.x + _terrainGrid[1, 1].terrainData.size.x, _terrainGrid[1, 1].transform.position.y, _terrainGrid[1, 1].transform.position.z + _terrainGrid[1, 1].terrainData.size.z);
		_terrainGrid[2, 1].transform.position = new Vector3(_terrainGrid[1, 1].transform.position.x + _terrainGrid[1, 1].terrainData.size.x, _terrainGrid[1, 1].transform.position.y, _terrainGrid[1, 1].transform.position.z);
		_terrainGrid[2, 2].transform.position = new Vector3(_terrainGrid[1, 1].transform.position.x + _terrainGrid[1, 1].terrainData.size.x, _terrainGrid[1, 1].transform.position.y, _terrainGrid[1, 1].transform.position.z - _terrainGrid[1, 1].terrainData.size.z);
		_terrainGrid[0, 0].SetNeighbors(null, null, _terrainGrid[1, 0], _terrainGrid[0, 1]);
		_terrainGrid[0, 1].SetNeighbors(null, _terrainGrid[0, 0], _terrainGrid[1, 1], _terrainGrid[0, 2]);
		_terrainGrid[0, 2].SetNeighbors(null, _terrainGrid[0, 1], _terrainGrid[1, 2], null);
		_terrainGrid[1, 0].SetNeighbors(_terrainGrid[0, 0], null, _terrainGrid[2, 0], _terrainGrid[1, 1]);
		_terrainGrid[1, 1].SetNeighbors(_terrainGrid[0, 1], _terrainGrid[1, 0], _terrainGrid[2, 1], _terrainGrid[1, 2]);
		_terrainGrid[1, 2].SetNeighbors(_terrainGrid[0, 2], _terrainGrid[1, 1], _terrainGrid[2, 2], null);
		_terrainGrid[2, 0].SetNeighbors(_terrainGrid[1, 0], null, null, _terrainGrid[2, 1]);
		_terrainGrid[2, 1].SetNeighbors(_terrainGrid[1, 1], _terrainGrid[2, 0], null, _terrainGrid[2, 2]);
		_terrainGrid[2, 2].SetNeighbors(_terrainGrid[1, 2], _terrainGrid[2, 1], null, null);
	}

	private void Update()
	{
		Vector3 vector = new Vector3(PlayerObject.transform.position.x, PlayerObject.transform.position.y, PlayerObject.transform.position.z);
		Terrain terrain = null;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				if (vector.x >= _terrainGrid[i, j].transform.position.x && vector.x <= _terrainGrid[i, j].transform.position.x + _terrainGrid[i, j].terrainData.size.x && vector.z >= _terrainGrid[i, j].transform.position.z && vector.z <= _terrainGrid[i, j].transform.position.z + _terrainGrid[i, j].terrainData.size.z)
				{
					terrain = _terrainGrid[i, j];
					num = 1 - i;
					num2 = 1 - j;
					break;
				}
			}
			if (terrain != null)
			{
				break;
			}
		}
		if (!(terrain != _terrainGrid[1, 1]))
		{
			return;
		}
		Terrain[,] array = new Terrain[3, 3];
		for (int k = 0; k < 3; k++)
		{
			for (int l = 0; l < 3; l++)
			{
				int num3 = k + num;
				if (num3 < 0)
				{
					num3 = 2;
				}
				else if (num3 > 2)
				{
					num3 = 0;
				}
				int num4 = l + num2;
				if (num4 < 0)
				{
					num4 = 2;
				}
				else if (num4 > 2)
				{
					num4 = 0;
				}
				array[num3, num4] = _terrainGrid[k, l];
			}
		}
		_terrainGrid = array;
		UpdateTerrainPositionsAndNeighbors();
	}
}
