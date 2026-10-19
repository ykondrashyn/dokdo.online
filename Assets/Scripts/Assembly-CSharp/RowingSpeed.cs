using UnityEngine;

public class RowingSpeed : MonoBehaviour
{
	public Animator[] anim;

	private Rigidbody rigid;

	public GameObject Show;

	public bool isEnemy;

	public bool isShow;

	private Enemy_Ship ES;

	private void OnEnable()
	{
		rigid = GetComponentInParent<Rigidbody>();
		isShow = false;
		ES = GetComponentInParent<Enemy_Ship>();
	}

	private void FixedUpdate()
	{
		if (!isShow)
		{
			if (!isEnemy)
			{
				if ((int)GameManager.Instance.Level_Sail >= 46)
				{
					Show.SetActive(true);
					isShow = true;
				}
				else
				{
					Show.SetActive(false);
				}
			}
			else if (ES.Lv_Sail >= 46)
			{
				Show.SetActive(true);
				isShow = true;
			}
			else
			{
				Show.SetActive(false);
			}
		}
		if (isShow)
		{
			for (int i = 0; i < anim.Length; i++)
			{
				anim[i].speed = Mathf.Clamp(rigid.velocity.magnitude * 0.2f, 0f, 1.5f);
			}
		}
	}
}
