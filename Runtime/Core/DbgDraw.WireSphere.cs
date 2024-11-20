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
        private static Mesh s_WireSphereMesh;

        [Conditional("DBG_DRAW_ENABLED")]
        public static void WireSphere(
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

            if (s_WireSphereMesh == null)
            {
                s_WireSphereMesh = CreateWireSphereMesh();
                ReleaseOnDestroy(s_WireSphereMesh);
            }

            job.mesh = s_WireSphereMesh;
            job.matrix = Matrix4x4.TRS(position, rotation, size);
            job.color = color;

            job.Submit();
        }

        private static Mesh CreateWireSphereMesh()
        {
            Mesh mesh = new();
            mesh.name = "DbgDraw-WireSphere-Mesh";

            var vertices = new List<Vector3>(64 * 3);
            float step = TAU / 64;
            float s = 0.5f;

            for (float theta = 0.0f; theta < TAU; theta += step)
            {
                float cos0 = Mathf.Cos(theta);
                float cos1 = Mathf.Cos(theta + step);
                float sin0 = Mathf.Sin(theta);
                float sin1 = Mathf.Sin(theta + step);

                // ring around x
                vertices.Add(s * new Vector3(0, cos0, -sin0));
                vertices.Add(s * new Vector3(0, cos1, -sin1));

                // ring around y
                vertices.Add(s * new Vector3(cos0, 0, -sin0));
                vertices.Add(s * new Vector3(cos1, 0, -sin1));

                // ring around z
                vertices.Add(s * new Vector3(cos0, -sin0, 0));
                vertices.Add(s * new Vector3(cos1, -sin1, 0));
            }

            int[] indices = new int[vertices.Count];
            for (int n = 0; n < indices.Length; ++n)
            {
                indices[n] = n;
            }

            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);

            return mesh;
        }
    }
}