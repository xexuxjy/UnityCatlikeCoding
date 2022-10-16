using UnityEngine;

public abstract class WarEntity : GameBehaviour
{

	WarFactory originFactory;

	public WarFactory OriginFactory
	{
		get => originFactory;
		set
		{
			Debug.Assert(originFactory == null, "Redefined origin factory!");
			originFactory = value;
		}
	}

	public override void Recycle()
	{
		originFactory.Reclaim(this);
	}
}