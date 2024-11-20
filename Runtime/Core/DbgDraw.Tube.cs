// DbgDraw for Unity. Copyright (c) 2019-2024 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityDbgDraw

using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

#pragma warning disable IDE0018 // Variable declaration can be inlined
#pragma warning disable IDE0017 // Object initialization can be simplified

namespace Oddworm.Framework
{
    public partial class DbgDraw
    {
        private static Mesh s_TubeMesh;

        [Conditional("DBG_DRAW_ENABLED")]
        public static void Tube(
            Vector3 position,
            Quaternion rotation,
            Vector3 size,
            Color color,
            float duration = 0,
            bool depthTest = true)
        {
            MeshJob job;
            if (!TryAllocMeshJob(out job, duration, depthTest, CullMode.Off, true))
            {
                return;
            }

            if (s_TubeMesh == null)
            {
                s_TubeMesh = CreateTubeMesh();
                ReleaseOnDestroy(s_TubeMesh);
            }

            job.mesh = s_TubeMesh;
            job.matrix = Matrix4x4.TRS(position, rotation, size);
            job.color = color;

            job.Submit();
        }

        private static Mesh CreateTubeMesh()
        {
            Mesh mesh = new();
            mesh.name = "DbgDraw-Tube-Mesh";

            var vertices = new List<Vector3>(64 * 4);
            float s = 0.5f;

            // ring around y, full circle at top and bottom
            float step = TAU / 64;
            for (float theta = 0.0f; theta < TAU - step; theta += step)
            {
                float cos0 = Mathf.Cos(theta);
                float cos1 = Mathf.Cos(theta + step);
                float sin0 = Mathf.Sin(theta);
                float sin1 = Mathf.Sin(theta + step);

                vertices.Add(s * new Vector3(cos1, +1, -sin1));
                vertices.Add(s * new Vector3(cos0, +1, -sin0));
                vertices.Add(s * new Vector3(cos0, -1, -sin0));

                vertices.Add(s * new Vector3(cos1, -1, -sin1));
                vertices.Add(s * new Vector3(cos1, +1, -sin1));
                vertices.Add(s * new Vector3(cos0, -1, -sin0));
            }

            int[] indices = new int[vertices.Count];
            for (int n = 0; n < indices.Length; ++n)
            {
                indices[n] = n;
            }

            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}