﻿using UnityEngine;

public class Fractal : MonoBehaviour
{

    [SerializeField, Range(1, 8)]
    int depth = 4;

    [SerializeField]
    Mesh mesh = default;

    [SerializeField]
    Material material = default;

    static readonly int matricesId = Shader.PropertyToID("_Matrices");

    static Vector3[] directions = {
        Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
    };

    static Quaternion[] rotations = {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
    };

    private void OnEnable()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
        parts = new FractalPart[depth][];
        matrices = new Matrix4x4[depth][];
        matricesBuffers = new ComputeBuffer[depth];

        int size = 1;
        int stride = 16 * 4;

        for (int i = 0,length=1; i < parts.Length; i++,length*=5)
        {
            parts[i] = new FractalPart[length];
            matrices[i] = new Matrix4x4[length];
            matricesBuffers[i] = new ComputeBuffer(length, stride);

            size *= 5;
        }

        FractalPart rootPart = CreatePart(0);
        parts[0][0] = rootPart;
        rootPart.worldRotation =
                    transform.rotation *
                    (rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f));
        rootPart.worldPosition = transform.position;

        matrices[0][0] = Matrix4x4.TRS(
            rootPart.worldPosition, rootPart.worldRotation, ObjectScale*Vector3.one);

        for (int li = 1; li < parts.Length; li++)
        {
           
            FractalPart[] parentParts = parts[li - 1];
            FractalPart[] levelParts = parts[li];
            Matrix4x4[] levelMatrices = matrices[li];

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
        float spinAngleDelta = 22.5f * Time.deltaTime;

        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle += spinAngleDelta;

        rootPart.worldRotation = rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f);
        parts[0][0] = rootPart;

        float scale = ObjectScale;

        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;

            FractalPart[] levelParts = parts[li];
            FractalPart[] parentParts = parts[li - 1];
            Matrix4x4[] levelMatrices = matrices[li];

            for (int fpi = 0; fpi < levelParts.Length; fpi++)
            {
                FractalPart parent = parentParts[fpi/5];
                FractalPart part = levelParts[fpi];
                
                part.spinAngle += spinAngleDelta;

                part.worldRotation = parent.worldRotation * (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));

                part.worldPosition =
                    parent.worldPosition +
                    parent.worldRotation * (1.5f * scale * part.direction); 
                    levelParts[fpi] = part;

                levelMatrices[fpi] = Matrix4x4.TRS(
                    part.worldPosition, part.worldRotation, scale * Vector3.one
                );
            }
        }
        
        
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
        public Vector3 direction,worldPosition;
        public Quaternion rotation,worldRotation;
        public float spinAngle;
    }


    FractalPart[][] parts;
    Matrix4x4[][] matrices;

    ComputeBuffer[] matricesBuffers;
    static MaterialPropertyBlock propertyBlock;


}