using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Tower : GameTileContent
{
    [SerializeField, Range(1.5f, 10.5f)]
    protected float targetingRange = 1.5f;


    const int enemyLayerMask = 1 << 9;

    protected TargetPoint target;

    public abstract TowerType TowerType { get; }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 position = transform.localPosition;
        position.y += 0.01f;
        Gizmos.DrawWireSphere(position, targetingRange);
        //if (target != null)
        //{
        //    Gizmos.DrawLine(position, target.Position);
        //}
    }

    protected bool TrackTarget(ref TargetPoint target)
    {
        if (target == null || !target.Enemy.IsValidTarget)
        {
            return false;
        }

        Vector3 a = transform.localPosition;
        Vector3 b = target.Position;

        a.y = 0;
        b.y = 0;

        if (Vector3.Distance(a, b) > targetingRange +0.125f * target.Enemy.Scale)
        {
            target = null;
            return false;
        }

        return true;
    }


    public override void GameUpdate()
    {
        //if (TrackTarget(ref target) || AcquireTarget(out target))
        //{
        //    Shoot();
        //}
        //else
        //{
        //    laserBeam.localScale = Vector3.zero;
        //}
    }


    void Shoot()
    {
        //Vector3 point = target.Position;
        //turret.LookAt(point);
        //laserBeam.localRotation = turret.localRotation;

        //float d = Vector3.Distance(turret.position, point);
        //laserBeamScale.z = d;
        //laserBeam.localScale = laserBeamScale;
        //laserBeam.localPosition = turret.localPosition + 0.5f * d * laserBeam.forward;

        //target.Enemy.ApplyDamage(damagePerSecond * Time.deltaTime);
    }

    static Collider[] targetsBuffer = new Collider[100];

    protected bool AcquireTarget(out TargetPoint target)
    {

        Vector3 a = transform.localPosition;
        Vector3 b = a;
        b.y += 3;

        if (TargetPoint.FillBuffer(transform.localPosition, targetingRange))
        {
            target = TargetPoint.RandomBuffered;
            return true;
        }

        target = null;
        return false;
    }
}

public enum TowerType
{
    Laser,Mortar
}
