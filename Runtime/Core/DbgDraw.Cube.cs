// DbgDraw for Unity. Copyright (c) 2019-2024 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityDbgDraw

using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

#pragma warning disable IDE0018 // Variable declaration can be inlined
#pragma warning disable IDE0017 // Object initialization can be simplified

namespace Oddworm.Framework
{
    public partial class DbgDraw
    {
        private static Mesh s_CubeMesh;

        [Conditional("DBG_DRAW_ENABLED")]
        public static void Cube(
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

            if (s_CubeMesh == null)
            {
                s_CubeMesh = CreateCubeMesh();
                ReleaseOnDestroy(s_CubeMesh);
            }

            job.mesh = s_CubeMesh;
            job.matrix = Matrix4x4.TRS(position, rotation, scale);
            job.color = color;

            job.Submit();
        }

        private static Mesh CreateCubeMesh()
        {
            Mesh mesh = CreateMesh(PrimitiveType.Cube);
            mesh.name = "DbgDraw-Cube-Mesh";
            return mesh;
        }
    }
}