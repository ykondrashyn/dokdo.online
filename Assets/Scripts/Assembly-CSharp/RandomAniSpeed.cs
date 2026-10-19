using UnityEngine;

public class RandomAniSpeed : MonoBehaviour
{
	public float min;

	public float max;

	private Animator animator;

	private void OnEnable()
	{
		animator = GetComponent<Animator>();
		animator.speed = Random.Range(min, max);
	}
}
