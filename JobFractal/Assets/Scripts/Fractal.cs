using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

using static Unity.Mathematics.math;
using float4x4 = Unity.Mathematics.float4x4;
using float3 = Unity.Mathematics.float3;

using quaternion = Unity.Mathematics.quaternion;

public class Fractal : MonoBehaviour
{

    [SerializeField, Range(1, 8)]
    int depth = 4;

    [SerializeField]
    Mesh mesh = default;

    [SerializeField]
    Material material = default;

    static readonly int matricesId = Shader.PropertyToID("_Matrices");

    static float3[] directions = {
        new float3(0,1,0), new float3(1,0,0), new float3(-1,0,0), new float3(0,0,1), new float3(0,0,-1)
    };


    static quaternion[] rotations = {
        quaternion.identity,
        quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
    };
    private void OnEnable()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<float4x4>[depth];
        matricesBuffers = new ComputeBuffer[depth];

        int size = 1;
        int stride = 16 * 4;

        for (int i = 0,length=1; i < parts.Length; i++,length*=5)
        {
            parts[i] = new NativeArray<FractalPart>(length,Allocator.Persistent);
            matrices[i] = new NativeArray<float4x4>(length,Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);

            size *= 5;
        }

        FractalPart rootPart = CreatePart(0);
        parts[0][0] = rootPart;
        rootPart.worldRotation = mul(transform.rotation,
                    mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle))
                );
        rootPart.worldPosition = transform.position;

        matrices[0][0] = float4x4.TRS(
            rootPart.worldPosition, rootPart.worldRotation, float3(ObjectScale));

        for (int li = 1; li < parts.Length; li++)
        {
           
            NativeArray<FractalPart> parentParts = parts[li - 1];
            NativeArray <FractalPart> levelParts = parts[li];
            NativeArray <float4x4> levelMatrices = matrices[li];

            for (int fpi = 0; fpi < levelParts.Length; fpi+=5)
            {
                for (int ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi+ci] = CreatePart(ci);
                }
            }
        }


    }

    void OnDisable()
    {
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            matricesBuffers[i].Release();
            parts[i].Dispose();
            matrices[i].Dispose();
        }
        parts = null;
        matrices = null;
        matricesBuffers = null;
    }

    void OnValidate()
    {
        if (parts != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    void Update()
    {
        float spinAngleDelta = 0.125f * PI * Time.deltaTime;

        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle += spinAngleDelta;

        rootPart.worldRotation = mul(transform.rotation,
              mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle))
          );

        parts[0][0] = rootPart;

        float scale = ObjectScale;
        JobHandle jobHandle = default;
        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;

            jobHandle = new UpdateFractalLevelJob
            {
                spinAngleDelta = spinAngleDelta,
                scale = scale,
                parents = parts[li - 1],
                parts = parts[li],
                matrices = matrices[li]
            }.ScheduleParallel(parts[li].Length, 1,jobHandle);


        }
        jobHandle.Complete();
        
        
        var bounds = new Bounds(Vector3.zero, 3f * ObjectScale * Vector3.one);
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);
            propertyBlock.SetBuffer(matricesId, buffer);
            material.SetBuffer(matricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, buffer.count,propertyBlock);
        }
    }

    public float ObjectScale
    { get
        {
            return transform.lossyScale.x;
        }
    }

    FractalPart CreatePart(int childIndex)
    {
        return new FractalPart
        {
            direction = directions[childIndex],
            rotation = rotations[childIndex] //,
                                             //transform = go.transform
        };
    }


    struct FractalPart
    {
        public float3 direction,worldPosition;
        public quaternion rotation,worldRotation;
        public float spinAngle;
    }


    NativeArray<FractalPart>[] parts;
    NativeArray<float4x4>[] matrices;

    ComputeBuffer[] matricesBuffers;
    static MaterialPropertyBlock propertyBlock;

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct UpdateFractalLevelJob : IJobFor 
    {
        public float spinAngleDelta;
        public float scale;
        
        [ReadOnly]
        public NativeArray<FractalPart> parents;
        public NativeArray<FractalPart> parts;
        
        [WriteOnly]
        public NativeArray<float4x4> matrices;

        public void Execute(int i) 
        {
            FractalPart parent = parents[i / 5];
            FractalPart part = parts[i];
            part.spinAngle += spinAngleDelta;
            part.worldRotation = mul(parent.worldRotation,
                            mul(part.rotation, quaternion.RotateY(part.spinAngle))
                        );
            part.worldPosition =
                parent.worldPosition +
                mul(parent.worldRotation, 1.5f * scale * part.direction);

            parts[i] = part;

            matrices[i] = float4x4.TRS(
                part.worldPosition, part.worldRotation, scale * Vector3.one
            );
        }
    }

}