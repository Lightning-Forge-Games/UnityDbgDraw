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
        private static Mesh s_WireHemisphereMesh;

        [Conditional("DBG_DRAW_ENABLED")]
        public static void WireHemisphere(
            Vector3 position,
            Quaternion rotation,
            Vector3 size,
            Color color,
            float duration = 0,
            bool depthTest = true)
        {
            MeshJob job;
            if (!TryAllocMeshJob(out job, duration, depthTest, CullMode.Back, true))
            {
                return;
            }

            if (s_WireHemisphereMesh == null)
            {
                s_WireHemisphereMesh = CreateWireHemisphereMesh();
                ReleaseOnDestroy(s_WireHemisphereMesh);
            }

            job.mesh = s_WireHemisphereMesh;
            job.matrix = Matrix4x4.TRS(position, rotation, size);
            job.color = color;

            job.Submit();
        }

        private static Mesh CreateWireHemisphereMesh()
        {
            Mesh mesh = new();
            mesh.name = "DbgDraw-WireHemisphere-Mesh";

            var vertices = new List<Vector3>(64 * 3);
            float s = 0.5f;

            // ring around y, full circle
            float step = TAU / 64;
            for (float theta = 0.0f; theta < TAU; theta += step)
            {
                float cos0 = Mathf.Cos(theta);
                float cos1 = Mathf.Cos(theta + step);
                float sin0 = Mathf.Sin(theta);
                float sin1 = Mathf.Sin(theta + step);

                vertices.Add(s * new Vector3(cos0, 0, -sin0));
                vertices.Add(s * new Vector3(cos1, 0, -sin1));
            }


            // sides
            float stept = TAU / 4;
            for (float t = 0.0f; t < TAU; t += stept)
            {
                Quaternion yrot = Quaternion.AngleAxis(Mathf.Rad2Deg * t, Vector3.up);

                // ring around x, half circle
                for (float theta = -Mathf.PI; theta < 0; theta += step)
                {
                    Quaternion xrot0 = Quaternion.AngleAxis(Mathf.Rad2Deg * theta, Vector3.right);
                    Quaternion xrot1 = Quaternion.AngleAxis(
                        Mathf.Rad2Deg * (theta + step),
                        Vector3.right);

                    vertices.Add(yrot * xrot0 * Vector3.forward * s);
                    vertices.Add(yrot * xrot1 * Vector3.forward * s);
                }
            }

#if false
        // ring around x, half circle
        for (var theta = 0.0f - Mathf.PI * 0.5f; theta < Mathf.PI - Mathf.PI * 0.5f; theta += step)
        {
            var cos0 = Mathf.Cos(theta);
            var cos1 = Mathf.Cos(theta + step);
            var sin0 = Mathf.Sin(theta);
            var sin1 = Mathf.Sin(theta + step);

            vertices.Add(s * new Vector3(0, cos0, -sin0));
            vertices.Add(s * new Vector3(0, cos1, -sin1));
        }

        // ring around z, half circle
        for (var theta = 0.0f - Mathf.PI; theta < 0; theta += step)
        {
            var cos0 = Mathf.Cos(theta);
            var cos1 = Mathf.Cos(theta + step);
            var sin0 = Mathf.Sin(theta);
            var sin1 = Mathf.Sin(theta + step);

            vertices.Add(s * new Vector3(cos0, -sin0, 0));
            vertices.Add(s * new Vector3(cos1, -sin1, 0));
        }
#endif

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