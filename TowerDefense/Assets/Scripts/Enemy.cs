using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class Enemy : GameBehaviour
{
    [SerializeField]
    Transform model = default;

    [SerializeField]
    EnemyAnimationConfig animationConfig = default;

    EnemyAnimator animator;

    

    float Health { get; set; }



    EnemyFactory originFactory;

    GameTile tileFrom, tileTo;
    Vector3 positionFrom, positionTo;
    float progress;
    float progressFactor;

    Direction direction;
    DirectionChange directionChange;
    float directionAngleFrom, directionAngleTo;

    float pathOffset;

    public float Scale { get; private set; }


    float speed;

    void Awake()
    {
        animator.Configure(
            model.GetChild(0).gameObject.AddComponent<Animator>(),
            animationConfig
        );


    }

    public void Initialize(float scale, float pathOffset, float speed, float health)
    {
        model.localScale = new Vector3(scale, scale, scale);
        this.pathOffset = pathOffset;
        this.speed = speed;
        Scale = scale;
        Health = health;
        
        animator.PlayIntro();
        targetPointCollider.enabled = false;

    }

    Collider targetPointCollider;

    public Collider TargetPointCollider
    {
        set
        {
            Debug.Assert(targetPointCollider == null, "Redefined collider!");
            targetPointCollider = value;
        }
    }

    public EnemyFactory OriginFactory
    {
        get => originFactory;
        set
        {
            Debug.Assert(originFactory == null, "Redefined origin factory!");
            originFactory = value;
        }
    }


    public override bool GameUpdate()
    {
        progress += Time.deltaTime * progressFactor;

        animator.GameUpdate();


        if (animator.CurrentClip == EnemyAnimator.Clip.Intro)
        {
            if (!animator.IsDone)
            {
                return true;
            }
            animator.PlayMove(speed / Scale);
            targetPointCollider.enabled = true;
        }
        else if (animator.CurrentClip >= EnemyAnimator.Clip.Outro)
        {
            if (animator.IsDone)
            {
                Recycle();
                return false;
            }
            targetPointCollider.enabled = false;
            return true;
        }

        while (progress >= 1f)
        {
            //tileFrom = tileTo;
            //tileTo = tileTo.NextTileOnPath;
            if (tileTo == null)
            {
                Game.EnemyReachedDestination();
                animator.PlayOutro();
                targetPointCollider.enabled = false;
                return true;
            }
            progress = (progress - 1f) / progressFactor;
            PrepareNextState();
            progress *= progressFactor;
        }
        if (directionChange == DirectionChange.None)
        {
            transform.localPosition =
                Vector3.LerpUnclamped(positionFrom, positionTo, progress);
        }
        else
        {
            float angle = Mathf.LerpUnclamped(
                directionAngleFrom, directionAngleTo, progress
            );
            transform.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        if (Health <= 0f)
        {
            animator.PlayDying();
            return true;
        }

        return true;
    }

    public bool IsValidTarget => animator.CurrentClip == EnemyAnimator.Clip.Move;

    public void SpawnOn(GameTile tile)
    {
        Debug.Assert(tile.NextTileOnPath != null, "Nowhere to go!", this);
        tileFrom = tile;
        tileTo = tile.NextTileOnPath;
        progress = 0f;
        PrepareIntro();
    }


    void PrepareNextState()
    {
        tileFrom = tileTo;
        tileTo = tileTo.NextTileOnPath;
        positionFrom = positionTo;

        if (tileTo == null)
        {
            PrepareOutro();
            return;
        }

        positionTo = tileFrom.ExitPoint;
        directionChange = direction.GetDirectionChangeTo(tileFrom.PathDirection);
        direction = tileFrom.PathDirection;
        directionAngleFrom = directionAngleTo;

        switch (directionChange)
        {
            case DirectionChange.None: PrepareForward(); break;
            case DirectionChange.TurnRight: PrepareTurnRight(); break;
            case DirectionChange.TurnLeft: PrepareTurnLeft(); break;
            default: PrepareTurnAround(); break;
        }


    }
    void PrepareForward()
    {
        transform.localRotation = direction.GetRotation();
        directionAngleTo = direction.GetAngle();
        model.localPosition = new Vector3(pathOffset, 0f);
        progressFactor = speed;
    }
    void PrepareTurnRight()
    {
        directionAngleTo = directionAngleFrom + 90f;
        model.localPosition = new Vector3(pathOffset - 0.5f, 0f);
        transform.localPosition = positionFrom + direction.GetHalfVector();
        progressFactor = speed / (Mathf.PI * 0.5f * (0.5f - pathOffset));
    }


    void PrepareTurnLeft()
    {
        directionAngleTo = directionAngleFrom - 90f;
        model.localPosition = new Vector3(pathOffset + 0.5f, 0);
        transform.localPosition = positionFrom + direction.GetHalfVector();
        progressFactor = speed / (Mathf.PI * 0.5f * (0.5f + pathOffset));
    }

    void PrepareTurnAround()
    {
        directionAngleTo = directionAngleFrom + (pathOffset < 0f ? 180f : -180f);
        model.localPosition = new Vector3(pathOffset, 0);
        transform.localPosition = positionFrom;
        progressFactor = speed / (Mathf.PI * Mathf.Max(Mathf.Abs(pathOffset), 0.2f));
    }
    void PrepareIntro()
    {
        positionFrom = tileFrom.transform.localPosition;
        transform.localPosition = positionFrom;

        positionTo = tileFrom.ExitPoint;
        direction = tileFrom.PathDirection;
        directionChange = DirectionChange.None;
        directionAngleFrom = directionAngleTo = direction.GetAngle();
        transform.localRotation = direction.GetRotation();

        progressFactor = 2f * speed;
    }

    void PrepareOutro()
    {
        positionTo = tileFrom.transform.localPosition;
        directionChange = DirectionChange.None;
        directionAngleTo = direction.GetAngle();
        model.localPosition = new Vector3(pathOffset, 0f);
        transform.localRotation = direction.GetRotation();
        progressFactor = 2f * speed;
    }

    public void ApplyDamage(float damage)
    {
        Debug.Assert(damage >= 0f, "Negative damage applied.");
        Health -= damage;
    }

    public override void Recycle()
    {
        animator.Stop();
        OriginFactory.Reclaim(this);
    }

    void OnDestroy()
    {
        animator.Destroy();
    }

}

public enum EnemyType
{
    Small, Medium, Large
}




[System.Serializable]
public struct EnemyAnimator
{
    public enum Clip { Move, Intro, Outro, Dying }

    PlayableGraph graph;

    AnimationMixerPlayable mixer;

    Clip previousClip;

    float transitionProgress;

    const float transitionSpeed = 5f;


    public void Configure(Animator animator, EnemyAnimationConfig config) 
    {
        graph = PlayableGraph.Create();
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        mixer = AnimationMixerPlayable.Create(graph, Enum.GetNames(typeof(Clip)).Length);

        var clip = AnimationClipPlayable.Create(graph, config.Move);
        mixer.ConnectInput((int)Clip.Move, clip, 0);
        clip.Pause();


        clip = AnimationClipPlayable.Create(graph, config.Intro);
        mixer.ConnectInput((int)Clip.Intro, clip, 0);
        clip.SetDuration(config.Intro.length);

        clip = AnimationClipPlayable.Create(graph, config.Outro);
        mixer.ConnectInput((int)Clip.Outro, clip, 0);
        clip.SetDuration(config.Outro.length);
        clip.Pause();

        clip = AnimationClipPlayable.Create(graph, config.Dying);
        clip.SetDuration(config.Dying.length);
        clip.Pause();
        mixer.ConnectInput((int)Clip.Dying, clip, 0);

        var output = AnimationPlayableOutput.Create(graph, "Enemy", animator);
        output.SetSourcePlayable(mixer);

    }

    public Clip CurrentClip { get; private set; }

    public void PlayMove(float speed) 
    {
        SetWeight(CurrentClip, 0f);
        SetWeight(Clip.Move, 1f);
        var clip = GetPlayable(Clip.Move);
        BeginTransition(Clip.Move);
        clip.SetSpeed(speed);
        clip.Play();
        CurrentClip = Clip.Move;
    }

    Playable GetPlayable(Clip clip)
    {
        return mixer.GetInput((int)clip);
    }


    public void PlayIntro()
    {
        SetWeight(Clip.Intro, 1f);
        CurrentClip = Clip.Intro;
        graph.Play();
        transitionProgress = -1f;
    }
    public void PlayOutro()
    {
        BeginTransition(Clip.Outro);
    }

    public void PlayDying()
    {
        BeginTransition(Clip.Dying);
    }


    void SetWeight(Clip clip, float weight)
    {
        mixer.SetInputWeight((int)clip, weight);
    }

    void BeginTransition(Clip nextClip)
    {
        previousClip = CurrentClip;
        CurrentClip = nextClip;
        transitionProgress = 0f;
        GetPlayable(nextClip).Play();
    }

    public void GameUpdate()
    {
        transitionProgress += Time.deltaTime * transitionSpeed;
        if (transitionProgress >= 0f)
        {
            if (transitionProgress >= 1f)
            {
                transitionProgress = -1f;
                SetWeight(CurrentClip, 1f);
                SetWeight(previousClip, 0f);
                GetPlayable(previousClip).Pause();
            }
            else
            {
                SetWeight(CurrentClip, transitionProgress);
                SetWeight(previousClip, 1f - transitionProgress);
            }
        }
    }

    public void Stop() 
    {
        graph.Stop();
    }

    public void Destroy()
    {
        graph.Destroy();
    }

    public bool IsDone => GetPlayable(CurrentClip).IsDone();


}