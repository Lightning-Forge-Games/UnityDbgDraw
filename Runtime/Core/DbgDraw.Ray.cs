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
        public static void Ray(
            Vector3 position,
            Vector3 direction,
            Color color,
            float duration = 0,
            bool depthTest = true)
        {
            PrimitiveJob job;
            if (!TryAllocPrimitiveJob(out job, GL.LINES, duration, depthTest, CullMode.Off, true))
            {
                return;
            }

            job.AddVertex(new PrimitiveJob.Vertex { position = position, color = color });
            job.AddVertex(new PrimitiveJob.Vertex { position = position + direction, color = color });

            job.Submit();
        }

        [Conditional("DBG_DRAW_ENABLED")]
        public static void Ray(Ray ray, Color color, float duration = 0, bool depthTest = true)
        {
            Ray(ray.origin, ray.direction * 100000, color, duration, depthTest);
        }
    }
}