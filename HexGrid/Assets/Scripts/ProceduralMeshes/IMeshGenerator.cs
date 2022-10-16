using UnityEngine;

namespace ProceduralMeshes
{

    public interface IMeshGenerator
    {
        void Execute<S>(int i, S streams) where S : struct, IMeshStreams;
        int VertexCount { get; }

        int IndexCount { get; }

        int JobLength { get; }

        int Resolution { get; set; }

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(1f, 0f, 1f));

    }
}