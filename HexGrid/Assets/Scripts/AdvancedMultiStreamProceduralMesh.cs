using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AdvancedMultiStreamProceduralMesh : MonoBehaviour
{

    int vertexAttributeCount = 4;
    int vertexCount = 4;
    int triangleIndexCount = 6;


    private void OnEnable()
    {
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, dimension: 3, stream: 0);
        vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3, stream: 1);
        vertexAttributes[2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4, 2);
        vertexAttributes[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 3);


        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose();


        meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt16);


        NativeArray<float3> positions = meshData.GetVertexData<float3>();
        positions[0] = 0f;
        positions[1] = right();
        positions[2] = up();
        positions[3] = float3(1f, 1f, 0f);

        NativeArray<float3> normals = meshData.GetVertexData<float3>(1);
        normals[0] = normals[1] = normals[2] = normals[3] = back();


        half h0 = half(0f), h1 = half(1f);

        NativeArray<half4> tangents = meshData.GetVertexData<half4>(2);
        tangents[0] = tangents[1] = tangents[2] = tangents[3] = half4(h1, h0, h0, half(-1));

        NativeArray<half2> texCoords = meshData.GetVertexData<half2>(3);
        texCoords[0] = h0;
        texCoords[1] = half2(h1, h0);
        texCoords[2] = half2(h0, h1);
        texCoords[3] = h1;


        NativeArray<ushort> triangleIndices = meshData.GetIndexData<ushort>();
        triangleIndices[0] = 0;
        triangleIndices[1] = 2;
        triangleIndices[2] = 1;
        triangleIndices[3] = 1;
        triangleIndices[4] = 2;
        triangleIndices[5] = 3;

        var bounds = new Bounds(new Vector3(0.5f, 0.5f), new Vector3(1f, 1f));

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount)
        {
            bounds = bounds,
            vertexCount = vertexCount
        }, MeshUpdateFlags.DontRecalculateBounds);


        Mesh mesh = new Mesh() { name = "Procedural Mesh", bounds = bounds };


        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);


        GetComponent<MeshFilter>().mesh = mesh;




    }



}