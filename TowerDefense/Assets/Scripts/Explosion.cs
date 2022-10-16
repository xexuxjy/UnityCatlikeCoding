﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : WarEntity
{
	[SerializeField, Range(0f, 1f)]
	float duration = 0.5f;

	[SerializeField]
	AnimationCurve opacityCurve = default;

	[SerializeField]
	AnimationCurve scaleCurve = default;

	static int colorPropertyID = Shader.PropertyToID("_Color");

	static MaterialPropertyBlock propertyBlock;
	
	float scale;

	public MeshRenderer meshRenderer;

	float age;

	public void Initialize(Vector3 position, float blastRadius, float damage=0)
	{
		if (damage > 0)
		{
			TargetPoint.FillBuffer(position, blastRadius);
			for (int i = 0; i < TargetPoint.BufferedCount; i++)
			{
				TargetPoint.GetBuffered(i).Enemy.ApplyDamage(damage);
			}
		}
		transform.localPosition = position;
		scale = 2f * blastRadius;
		//transform.localScale = Vector3.one * (2f * blastRadius);
	}

	public override bool GameUpdate()
	{
		age += Time.deltaTime;
		if (age >= duration)
		{
			OriginFactory.Reclaim(this);
			return false;
		}

		if (propertyBlock == null)
		{
			propertyBlock = new MaterialPropertyBlock();
		}
		float t = age / duration;
		Color c = Color.clear;
		c.a = opacityCurve.Evaluate(t);
		propertyBlock.SetColor(colorPropertyID, c);
		meshRenderer.SetPropertyBlock(propertyBlock);
		transform.localScale = Vector3.one * (scale * scaleCurve.Evaluate(t));

		return true;
	}

}
