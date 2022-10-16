﻿using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;


namespace ProceduralMeshes.Generators
{
    public struct UVSphere : IMeshGenerator
    {
        public int Resolution { get; set; }

        int ResolutionV => 2 * Resolution;
        int ResolutionU => 4 * Resolution;

        public int VertexCount => (ResolutionU + 1) * (ResolutionV + 1) - 2;

        public int IndexCount => 6 * ResolutionU * (ResolutionV - 1);

        public int JobLength => ResolutionU + 1;

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));

        public void Execute<S>(int u, S streams) where S : struct, IMeshStreams
        {
            if (u == 0)
            {
                ExecuteSeam(streams);
            }
            else
            {
                ExecuteRegular(u, streams);
            }
        }

        public void ExecuteRegular<S>(int u, S streams) where S : struct, IMeshStreams
        {
            int vi = ((ResolutionV + 1) * u) - 2;
            int ti = 2 * (ResolutionV - 1) * (u - 1);

            var vertex = new Vertex();
            vertex.position.y = vertex.normal.y = -1f;
            vertex.tangent.x = cos(2f * PI * (u - 0.5f) / ResolutionU);
            vertex.tangent.z = sin(2f * PI * (u - 0.5f) / ResolutionU);
            vertex.tangent.w = -1f;
            vertex.texCoord0.x = (u - 0.5f) / ResolutionU;
            streams.SetVertex(vi, vertex);

            vertex.position.y = vertex.normal.y = 1f;
            vertex.texCoord0.y = 1f;
            streams.SetVertex(vi + ResolutionV, vertex);

            vi += 1;

            float2 circle;
            circle.x = sin(2f * PI * u / ResolutionU);
            circle.y = cos(2f * PI * u / ResolutionU);
            vertex.tangent.xz = circle.yx;
            circle.y = -circle.y;

            vertex.texCoord0.x = (float)u / ResolutionU;

            int shiftLeft = (u == 1 ? 0 : -1) - ResolutionV;

            streams.SetTriangle(ti, vi + int3(-1, shiftLeft, 0));
            ti += 1;

            for (int v = 1; v < ResolutionV; v++, vi++)
            {
                float circleRadius = sin(PI * v / ResolutionV);

                vertex.position.xz = circle * circleRadius;
                vertex.position.y = -cos(PI * v / ResolutionV);
                vertex.normal = vertex.position;

                vertex.texCoord0.y = (float)v / ResolutionV;
                vertex.texCoord0.x = (float)u / ResolutionU;

                streams.SetVertex(vi, vertex);

                if (v > 1)
                {
                    streams.SetTriangle(ti + 0, vi + int3(shiftLeft - 1, shiftLeft, -1));
                    streams.SetTriangle(ti + 1, vi + int3(-1, shiftLeft, 0));
                    ti += 2;
                }
            }

            streams.SetTriangle(ti, vi + int3(shiftLeft - 1, 0, -1));

        }

        public void ExecuteSeam<S>(S streams) where S : struct, IMeshStreams
        {
            var vertex = new Vertex();
            vertex.tangent.x = 1f;
            vertex.tangent.w = -1f;

            for (int v = 1; v < ResolutionV; v++)
            {
                sincos(
                    PI + PI * v / ResolutionV,
                    out vertex.position.z, out vertex.position.y
                );
                vertex.normal = vertex.position;
                vertex.texCoord0.y = (float)v / ResolutionV;
                streams.SetVertex(v - 1, vertex);
            }
        }

    }
}