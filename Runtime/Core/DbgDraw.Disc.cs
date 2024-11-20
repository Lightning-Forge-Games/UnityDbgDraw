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
        private static Mesh s_DiscMesh;

        [Conditional("DBG_DRAW_ENABLED")]
        public static void Disc(
            Vector3 position,
            Quaternion rotation,
            float radius,
            Color color,
            float duration = 0,
            bool depthTest = true)
        {
            MeshJob job;
            if (!TryAllocMeshJob(out job, duration, depthTest, CullMode.Off, true))
            {
                return;
            }

            if (s_DiscMesh == null)
            {
                s_DiscMesh = CreateDiscMesh();
                ReleaseOnDestroy(s_DiscMesh);
            }

            job.mesh = s_DiscMesh;
            job.matrix = Matrix4x4.TRS(position, rotation, Vector3.one * radius);
            job.color = color;

            job.Submit();
        }

        private static Mesh CreateDiscMesh()
        {
            Mesh mesh = new();
            mesh.name = "DbgDraw-Disc-Mesh";

            var vertices = new List<Vector3>(64 * 3);
            float step = TAU / 64;

            for (float theta = step; theta < TAU; theta += step)
            {
                float cos0 = Mathf.Cos(theta - step);
                float cos1 = Mathf.Cos(theta);
                float sin0 = Mathf.Sin(theta - step);
                float sin1 = Mathf.Sin(theta);

                vertices.Add(Vector3.zero);
                vertices.Add(new Vector3(cos0, 0, -sin0));
                vertices.Add(new Vector3(cos1, 0, -sin1));
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