﻿// DbgDraw for Unity. Copyright (c) 2019-2024 Peter Schraut (www.console-dev.de). See LICENSE.md
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
        [Conditional("DBG_DRAW_ENABLED")]
        public static void WireQuad(
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Color color,
            float duration = 0,
            bool depthTest = true)
        {
            PrimitiveJob job;
            if (!TryAllocPrimitiveJob(
                out job,
                GL.LINE_STRIP,
                duration,
                depthTest,
                CullMode.Off,
                true))
            {
                return;
            }

            job.matrix = Matrix4x4.TRS(position, rotation, scale);
            job.useMatrix = true;

            float s = 1.0f * 0.5f;
            job.AddVertex(new PrimitiveJob.Vertex { position = new Vector3(-s, 0, -s), color = color });
            job.AddVertex(new PrimitiveJob.Vertex { position = new Vector3(-s, 0, +s), color = color });
            job.AddVertex(new PrimitiveJob.Vertex { position = new Vector3(+s, 0, +s), color = color });
            job.AddVertex(new PrimitiveJob.Vertex { position = new Vector3(+s, 0, -s), color = color });
            job.AddVertex(new PrimitiveJob.Vertex { position = new Vector3(-s, 0, -s), color = color });

            job.Submit();
        }
    }
}