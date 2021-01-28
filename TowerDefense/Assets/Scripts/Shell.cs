using UnityEngine;

public class Shell : WarEntity 
{

	Vector3 launchPoint, targetPoint, launchVelocity;
	float blastRadius;
	float damage;

	float age;


	public void Initialize(Vector3 launchPoint, Vector3 targetPoint, Vector3 launchVelocity,
		float blastRadius, float damage)
	{
		this.launchPoint = launchPoint;
		this.targetPoint = targetPoint;
		this.launchVelocity = launchVelocity;
		this.blastRadius = blastRadius;
		this.damage = damage;
	}

	public override bool GameUpdate()
	{
		Vector3 d = launchVelocity;
		d.y -= 9.81f * age;
		transform.localRotation = Quaternion.LookRotation(d);

		age += Time.deltaTime;
		Vector3 p = launchPoint + launchVelocity * age;
		p.y -= 0.5f * 9.81f * age * age;
		
		if (p.y <= 0f)
		{
			TargetPoint.FillBuffer(targetPoint, blastRadius);
			for (int i = 0; i < TargetPoint.BufferedCount; i++)
			{
				TargetPoint.GetBuffered(i).Enemy.ApplyDamage(damage);
			}

			Game.SpawnExplosion().Initialize(targetPoint, blastRadius, damage);

			OriginFactory.Reclaim(this);
			return false;
		}

		Game.SpawnExplosion().Initialize(p, 0.1f);

		transform.localPosition = p;
		return true;
	}

}