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
        public static void WireArc(
            Vector3 position,
            Quaternion rotation,
            Vector3 from,
            float angle,
            float radius,
            Color color,
            float duration = 0,
            bool depthTest = true)
        {
            WireArc(position, rotation, from, 0, angle, radius, radius, color, duration, depthTest);
        }

        [Conditional("DBG_DRAW_ENABLED")]
        public static void WireArc(
            Vector3 position,
            Quaternion rotation,
            Vector3 from,
            float fromAngle,
            float toAngle,
            float innerRadius,
            float outerRadius,
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

            job.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            job.useMatrix = true;
            job.useVertexColor = true;

            if (fromAngle != 0)
            {
                fromAngle %= 360;
            }

            if (toAngle != 0)
            {
                toAngle %= 360;
            }

            if (fromAngle > toAngle)
            {
                float temp = fromAngle;
                fromAngle = toAngle;
                toAngle = temp;
            }

            innerRadius = Mathf.Max(0, innerRadius);
            outerRadius = Mathf.Max(0, outerRadius);
            if (innerRadius > outerRadius)
            {
                float temp = innerRadius;
                innerRadius = outerRadius;
                outerRadius = temp;
            }

            const int circleSegments = 24;
            Vector4 rotationRightVector = job.matrix.GetColumn(0);
            Vector4 rotationUpVector = job.matrix.GetColumn(1);
            float startingTheta = Vector3.SignedAngle(rotationRightVector, from, rotationUpVector) *
                Mathf.Deg2Rad;
            float theta = startingTheta + fromAngle * Mathf.Deg2Rad; // start at this angle
            float thetaTarget = startingTheta + toAngle * Mathf.Deg2Rad; // end at this angle
            float thetaStep = (thetaTarget - theta) / circleSegments; // make a step of this size
            bool hasInnerRadius = Mathf.Abs(innerRadius - outerRadius) - 0.001f > 0;

            // from center
            if (hasInnerRadius)
            {
                job.AddVertex(
                    new PrimitiveJob.Vertex
                    {
                        position = new Vector3(Mathf.Cos(theta), 0, -Mathf.Sin(theta)) * innerRadius,
                        color = color
                    });
            }

            // outer ring
            for (int n = 0; n <= circleSegments; ++n)
            {
                job.AddVertex(
                    new PrimitiveJob.Vertex
                    {
                        position = new Vector3(Mathf.Cos(theta), 0, -Mathf.Sin(theta)) * outerRadius,
                        color = color
                    });
                theta += thetaStep;
            }

            if (hasInnerRadius)
            {
                // to center
                theta -= thetaStep;
                job.AddVertex(
                    new PrimitiveJob.Vertex
                    {
                        position = new Vector3(Mathf.Cos(theta), 0, -Mathf.Sin(theta)) * innerRadius,
                        color = color
                    });

                // inner ring
                for (int n = 0; n <= circleSegments; ++n)
                {
                    job.AddVertex(
                        new PrimitiveJob.Vertex
                        {
                            position = new Vector3(Mathf.Cos(theta), 0, -Mathf.Sin(theta)) * innerRadius,
                            color = color
                        });
                    theta -= thetaStep;
                }
            }

            job.Submit();
        }
    }
}