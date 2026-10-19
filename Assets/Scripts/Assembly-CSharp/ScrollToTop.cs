using UnityEngine;
using UnityEngine.UI;

public class ScrollToTop : MonoBehaviour
{
	private Scrollbar scrollbar;

	private void Awake()
	{
		scrollbar = GetComponent<Scrollbar>();
	}

	public void repositionScrollbar()
	{
		scrollbar.value = 1f;
		scrollbar.size = 0.08560523f;
	}
}
