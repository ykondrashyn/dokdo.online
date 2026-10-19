namespace DG.Tweening
{
	public static class DOTweenAnimationExtensions
	{
		public static bool IsSameOrSubclassOf<T>(this global::UnityEngine.Component t)
		{
			return t is T;
		}
	}
}
