﻿using static Unity.Mathematics.math;


namespace ProceduralMeshes.Generators
{
    public struct SquareGrid : IMeshGenerator
    {
        public int Resolution { get; set; }

        public int VertexCount => 4 * Resolution * Resolution;

        public int IndexCount => 6 * Resolution * Resolution;

        public int JobLength => Resolution;


        public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
        {
            int vi = 4 * Resolution * z;
            int ti = 2 * Resolution * z;

            //int z = i / Resolution;

            //Debug.Log("I == "+i);


            //int x = i - Resolution * z;

            for (int x = 0; x < Resolution; ++x, vi += 4, ti += 2)
            {
                float tileSize = 1.0f;

                //var coordinates = float4(x, x + tileSize, z, z + tileSize) / Resolution - 0.5f; ;
                var xCoordinates = float2(x, x + tileSize) / Resolution - 0.5f;
                var zCoordinates = float2(z, z + tileSize) / Resolution - 0.5f;



                var vertex = new Vertex();
                vertex.position.x = xCoordinates.x;
                vertex.position.z = zCoordinates.x;
                vertex.normal.y = 1f;
                vertex.tangent.xw = float2(1f, -1f);

                streams.SetVertex(vi + 0, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.texCoord0 = float2(1f, 0f);
                streams.SetVertex(vi + 1, vertex);

                vertex.position.x = xCoordinates.x;
                vertex.position.z = zCoordinates.y;
                vertex.texCoord0 = float2(0f, 1f);
                streams.SetVertex(vi + 2, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.texCoord0 = 1f;
                streams.SetVertex(vi + 3, vertex);

                streams.SetTriangle(ti + 0, vi + int3(0, 2, 1));
                streams.SetTriangle(ti + 1, vi + int3(1, 2, 3));
            }
        }
    }
}