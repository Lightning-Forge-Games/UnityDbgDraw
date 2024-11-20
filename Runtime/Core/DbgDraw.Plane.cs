// DbgDraw for Unity. Copyright (c) 2019-2022 Peter Schraut (www.console-dev.de). See LICENSE.md
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
        private static Mesh s_PlaneMesh;

        [Conditional("DBG_DRAW_ENABLED")]
        public static void Plane(
            Plane plane,
            Vector3 position,
            Vector3 scale,
            Color color,
            float duration = 0,
            bool depthTest = true)
        {
            MeshJob job;
            if (!TryAllocMeshJob(out job, duration, depthTest, CullMode.Off, true))
            {
                return;
            }

            if (s_PlaneMesh == null)
            {
                s_PlaneMesh = CreatePlaneMesh();
                ReleaseOnDestroy(s_PlaneMesh);
            }

            job.mesh = s_PlaneMesh;
            job.matrix = Matrix4x4.TRS(position, Quaternion.LookRotation(plane.normal), scale);
            job.color = color;

            job.Submit();
        }

        private static Mesh CreatePlaneMesh()
        {
            Mesh mesh = new();
            mesh.name = "DbgDraw-Plane-Mesh";

            var vertices = new List<Vector3>(4 * 3);
            float s = 0.5f;

            // quad
            vertices.Add(new Vector3(-s, -s, 0));
            vertices.Add(new Vector3(-s, +s, 0));
            vertices.Add(new Vector3(+s, +s, 0));

            vertices.Add(new Vector3(+s, +s, 0));
            vertices.Add(new Vector3(+s, -s, 0));
            vertices.Add(new Vector3(-s, -s, 0));

            // "arrrow"
            s = 0.01f;
            vertices.Add(new Vector3(0, -s, 0));
            vertices.Add(new Vector3(0, 0, 0.25f));
            vertices.Add(new Vector3(0, +s, 0));

            vertices.Add(new Vector3(-s, 0, 0));
            vertices.Add(new Vector3(0, 0, 0.25f));
            vertices.Add(new Vector3(+s, 0, 0));


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