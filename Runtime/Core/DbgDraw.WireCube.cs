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
        private static Mesh s_WireCubeMesh;

        [Conditional("DBG_DRAW_ENABLED")]
        public static void WireCube(
            Vector3 position,
            Quaternion rotation,
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

            if (s_WireCubeMesh == null)
            {
                s_WireCubeMesh = CreateWireCubeMesh();
                ReleaseOnDestroy(s_WireCubeMesh);
            }

            job.mesh = s_WireCubeMesh;
            job.matrix = Matrix4x4.TRS(position, rotation, scale);
            job.color = color;

            job.Submit();
        }

        private static Mesh CreateWireCubeMesh()
        {
            Mesh mesh = new();
            mesh.name = "DbgDraw-WireCube-Mesh";

            var vertices = new List<Vector3>(24);

            float s = 1.0f * 0.5f;
            vertices.Add(new Vector3(-s, -s, -s)); // bottom near left
            vertices.Add(new Vector3(-s, -s, +s)); // bottom far left
            vertices.Add(new Vector3(-s, -s, +s)); // bottom far left
            vertices.Add(new Vector3(+s, -s, +s)); // bottom far right
            vertices.Add(new Vector3(+s, -s, +s)); // bottom far right
            vertices.Add(new Vector3(+s, -s, -s)); // bottom near right
            vertices.Add(new Vector3(+s, -s, -s)); // bottom near right
            vertices.Add(new Vector3(-s, -s, -s)); // bottom near left

            vertices.Add(new Vector3(-s, +s, -s)); // top near left
            vertices.Add(new Vector3(-s, +s, +s)); // top far left
            vertices.Add(new Vector3(-s, +s, +s)); // top far left
            vertices.Add(new Vector3(+s, +s, +s)); // top far right
            vertices.Add(new Vector3(+s, +s, +s)); // top far right
            vertices.Add(new Vector3(+s, +s, -s)); // top near right
            vertices.Add(new Vector3(+s, +s, -s)); // top near right
            vertices.Add(new Vector3(-s, +s, -s)); // top near left

            vertices.Add(new Vector3(+s, +s, +s)); // top far right
            vertices.Add(new Vector3(+s, -s, +s)); // bottom far right
            vertices.Add(new Vector3(+s, +s, -s)); // top near right
            vertices.Add(new Vector3(+s, -s, -s)); // bottom near right

            vertices.Add(new Vector3(-s, +s, +s)); // top far left
            vertices.Add(new Vector3(-s, -s, +s)); // bottom far left
            vertices.Add(new Vector3(-s, +s, -s)); // top near left
            vertices.Add(new Vector3(-s, -s, -s)); // bottom near left

            mesh.SetVertices(vertices);

            int[] indices = new int[vertices.Count];
            for (int n = 0; n < indices.Length; ++n)
            {
                indices[n] = n;
            }

            mesh.SetIndices(indices, MeshTopology.Lines, 0);

            return mesh;
        }
    }
}