using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
	public int myScore;

	public int EnemyScore;

	private Text label;

	private void Start()
	{
		label = GetComponent<Text>();
	}

	public void AddScore(bool isEnemy)
	{
		if (!isEnemy)
		{
			myScore++;
		}
		else
		{
			EnemyScore++;
		}
		label.text = myScore + " : " + EnemyScore;
		if (myScore >= 30 || EnemyScore >= 30)
		{
			myScore = 0;
			EnemyScore = 0;
			label.text = myScore + " : " + EnemyScore;
		}
	}
}
