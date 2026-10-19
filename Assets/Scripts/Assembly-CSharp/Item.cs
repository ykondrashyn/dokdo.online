using System;
using CodeStage.AntiCheat.ObscuredTypes;

[Serializable]
public class Item
{
	public string name;

	public itemType type;

	public ObscuredInt value;

	public Item(itemType t, int v)
	{
		type = t;
		value = v;
	}

	public Item()
	{
	}
}
