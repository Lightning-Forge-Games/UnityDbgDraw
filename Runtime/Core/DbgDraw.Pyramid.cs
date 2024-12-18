﻿// DbgDraw for Unity. Copyright (c) 2019-2024 Peter Schraut (www.console-dev.de). See LICENSE.md
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
        private static Mesh s_PyramidMesh;

        [Conditional("DBG_DRAW_ENABLED")]
        public static void Pyramid(
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Color color,
            float duration = 0,
            bool depthTest = true)
        {
            MeshJob job;
            if (!TryAllocMeshJob(out job, duration, depthTest, CullMode.Back, true))
            {
                return;
            }

            if (s_PyramidMesh == null)
            {
                s_PyramidMesh = CreatePyramidMesh();
                ReleaseOnDestroy(s_PyramidMesh);
            }

            job.mesh = s_PyramidMesh;
            job.matrix = Matrix4x4.TRS(position, rotation, scale);
            job.color = color;

            job.Submit();
        }

        private static Mesh CreatePyramidMesh()
        {
            Mesh mesh = new();
            mesh.name = "DbgDraw-Pyramid-Mesh";

            var vertices = new List<Vector3>(6 * 3);
            float s = 0.5f;

            // bottom left triangle
            vertices.Add(new Vector3(+s, -s, +s)); // bottom far right
            vertices.Add(new Vector3(-s, -s, +s)); // bottom far left
            vertices.Add(new Vector3(-s, -s, -s)); // bottom near left

            // bottom right triangle
            vertices.Add(new Vector3(-s, -s, -s)); // bottom near left
            vertices.Add(new Vector3(+s, -s, -s)); // bottom near right
            vertices.Add(new Vector3(+s, -s, +s)); // bottom far right

            // front triangle
            vertices.Add(new Vector3(-s, -s, -s)); // bottom near left
            vertices.Add(new Vector3(+0, +s, +0)); // top center
            vertices.Add(new Vector3(+s, -s, -s)); // bottom near right

            // back triangle
            vertices.Add(new Vector3(+s, -s, +s)); // bottom far right
            vertices.Add(new Vector3(+0, +s, +0)); // top center
            vertices.Add(new Vector3(-s, -s, +s)); // bottom far left

            // left triangle
            vertices.Add(new Vector3(-s, -s, +s)); // bottom far left
            vertices.Add(new Vector3(+0, +s, +0)); // top center
            vertices.Add(new Vector3(-s, -s, -s)); // bottom near left

            // right triangle
            vertices.Add(new Vector3(+0.5f, -s, -0.5f)); // bottom near right
            vertices.Add(new Vector3(0, +s, 0)); // top center
            vertices.Add(new Vector3(+0.5f, -s, +0.5f)); // bottom far right

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