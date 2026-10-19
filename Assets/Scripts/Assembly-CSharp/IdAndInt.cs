using System;
using CodeStage.AntiCheat.ObscuredTypes;

[Serializable]
public class IdAndInt
{
	public string ID;

	public ObscuredInt value;

	public IdAndInt(int a)
	{
		value = a;
	}
}
