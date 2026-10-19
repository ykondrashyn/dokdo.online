using UnityEngine;

public class IslandLine : MonoBehaviour
{
	public GameObject target;

	public IslandChecker IC;

	private ShipController SC;

	private GameManager GM;

	private void Awake()
	{
		GM = GameManager.Instance;
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
	}

	private void OnEnable()
	{
	}

	private void Update()
	{
		if (SC.isParking || !GM.IslandDone[IC.IslandNumber])
		{
			target.SetActive(false);
		}
		else
		{
			target.SetActive(true);
		}
	}
}
