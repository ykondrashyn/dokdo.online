using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class RemainEnemy : MonoBehaviour
{
	public Sprite[] flags;

	public Image flag;

	public Text CurrentName;

	public Text RemainTarget;

	public GameObject pop;

	private GameManager GM;

	private void Awake()
	{
		GM = GameManager.Instance;
	}

	private void LateUpdate()
	{
		if (GM.CurrentIsland != null)
		{
			pop.SetActive(true);
			CurrentName.text = GM.IslandName[GM.CurrentIsland.IslandNumber];
			flag.sprite = flags[GM.AmblemNumber(GM.CurrentIsland.IslandNumber)];
			RemainTarget.text = EnemyNumber();
		}
		else
		{
			pop.SetActive(false);
		}
	}

	private string EnemyNumber()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(GM.CurrentIsland.IslandEnemy.Count);
		stringBuilder.Append("/");
		stringBuilder.Append(AllEnemy(GM.CurrentIsland.Tier));
		return stringBuilder.ToString();
	}

	private int AllEnemy(int Tier)
	{
		int result = 1;
		if (GM.CurrentWorld.Equals(WorldType.Dokdo))
		{
			switch (Tier)
			{
			case 0:
				result = 3;
				break;
			case 1:
				result = 3;
				break;
			case 2:
				result = 4;
				break;
			case 3:
				result = 4;
				break;
			case 4:
				result = 5;
				break;
			case 5:
				result = 5;
				break;
			case 6:
				result = 6;
				break;
			}
		}
		else
		{
			result = 10;
		}
		return result;
	}
}
