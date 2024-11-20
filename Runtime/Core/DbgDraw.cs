// DbgDraw for Unity. Copyright (c) 2019-2021 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityDbgDraw

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#pragma warning disable IDE0018 // Variable declaration can be inlined
#pragma warning disable IDE0017 // Object initialization can be simplified
#pragma warning disable IDE1006 // Naming rule vilation

namespace Oddworm.Framework
{
    public partial class DbgDraw
    {
        /// <summary>
        /// Gets whether DbgDraw is supported. Returns true in development mode builds and if the development mode setting is ticked editor, false otherwise.
        /// </summary>
        private static bool IsSupported
        {
            get
            {
#if DBG_DRAW_ENABLED && !DBG_DRAW_DISABLED
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Gets and sets whether DbgDraw is enabled.
        /// </summary>
        public static bool IsEnabled = true;

        private static DbgDrawBehaviour s_Instance;

        private const float TAU = 2 * Mathf.PI;
        private static Color s_XAxisColor = new(219f / 255, 62f / 255, 29f / 255, .93f);
        private static Color s_YAxisColor = new(154f / 255, 243f / 255, 72f / 255, .93f);
        private static Color s_ZAxisColor = new(58f / 255, 122f / 255, 248f / 255, .93f);

        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");
        private static readonly int SRCBLEND_ID = Shader.PropertyToID("_SrcBlend");
        private static readonly int DSTBLEND_ID = Shader.PropertyToID("_DstBlend");
        private static readonly int CULL_ID = Shader.PropertyToID("_Cull");
        private static readonly int ZWRITE_ID = Shader.PropertyToID("_ZWrite");
        private static readonly int ZTEST_ID = Shader.PropertyToID("_ZTest");

        private static bool IsEnabledAndSupported => IsEnabled && IsSupported;
        private static bool IsEnabledAndPlaying => IsEnabledAndSupported && Application.isPlaying;

        private static bool TryGetLineBatch(out LineBatchJob batch, bool depthTest, CullMode cullMode)
        {
            if (!IsEnabledAndPlaying)
            {
                batch = new LineBatchJob();
                return false;
            }

            batch = instance.GetLineBatch(depthTest, cullMode);
            return true;
        }

        private static bool TryAllocPrimitiveJob(out PrimitiveJob job, int primitiveType, float duration, bool depthTest, CullMode cullMode, bool shaded)
        {
            if (!IsEnabledAndPlaying)
            {
                job = new PrimitiveJob();
                return false;
            }

            job = instance.AllocPrimitiveJob(primitiveType, duration, depthTest, cullMode, shaded);
            return true;
        }

        private static bool TryAllocMeshJob(out MeshJob job, float duration, bool depthTest, CullMode cullMode, bool shaded)
        {
            //shaded = true;
            if (!IsEnabledAndPlaying)
            {
                job = new MeshJob();
                return false;
            }

            job = instance.AllocMeshJob(duration, depthTest, cullMode, shaded);
            return true;
        }

        private static Mesh CreateMesh(PrimitiveType type)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Mesh mesh = Object.Instantiate<Mesh>(go.GetComponent<MeshFilter>().sharedMesh);
            Object.Destroy(go);
            return mesh;
        }

        private static void ReleaseOnDestroy(Mesh mesh)
        {
            instance.ReleaseOnDestroy(mesh);
        }

        private struct LineBatchJob
        {
            public struct Line
            {
                public Vector3 a;
                public Vector3 b;
                public Color32 color;
                public float remainingDuration;
            }

            public List<Line> list;
            public Material material;

            public void AddLine(Vector3 a, Vector3 b, Color32 color, float duration)
            {
                Line line = new();
                line.a = a;
                line.b = b;
                line.color = color;
                line.remainingDuration = duration;
                list.Add(line);
            }

            public void Submit()
            {
            }
        }

        private struct PrimitiveJob
        {
            public struct Vertex
            {
                public Vector3 position;
                public Color32 color;
            }

            public int primitiveType; // GL.LINES, GL.TRIANGLES, and so on
            public List<Vertex> list;
            public float remainingDuration;
            public bool depthTest;
            public Matrix4x4 matrix;
            public bool useMatrix;
            public bool useVertexColor;
            public Material material;

            private readonly List<PrimitiveJob> m_Owner;

            public PrimitiveJob(List<PrimitiveJob> owner)
                : this()
            {
                m_Owner = owner;
            }

            public void AddVertex(Vertex vertex)
            {
                list.Add(vertex);
            }

            public void Submit()
            {
                m_Owner.Add(this);
            }
        }

        private struct MeshJob
        {
            public float remainingDuration;
            public bool depthTest;
            public Matrix4x4 matrix;
            public Material material;
            public Mesh mesh;
            public Color color;

            private readonly List<MeshJob> m_Owner;

            public MeshJob(List<MeshJob> owner)
                : this()
            {
                m_Owner = owner;
            }

            public void Submit()
            {
                m_Owner.Add(this);
            }
        }

        [DefaultExecutionOrder(int.MinValue)]
        private class PreDbgDrawBehaviour : MonoBehaviour
        {
            [System.NonSerialized]
            public DbgDrawBehaviour debugDrawBehaviour;

            private void Update()
            {
                if (debugDrawBehaviour != null)
                {
                    UnityEngine.Profiling.Profiler.BeginSample("DbgDraw.RemoveDeadJobs");
                    debugDrawBehaviour.RemoveDeadJobs();
                    UnityEngine.Profiling.Profiler.EndSample();
                }
            }
        }

        [DefaultExecutionOrder(int.MaxValue)]
        private class DbgDrawBehaviour : MonoBehaviour
        {
            private List<PrimitiveJob> m_PrimitiveJobs = new(64);

            private List<List<PrimitiveJob.Vertex>> m_VertexListCache = new(64);

            private List<MeshJob> m_MeshJobs = new(64);
            private List<Object> m_ReleaseOnDestroy = new();

            private Material[,]
                m_ColoredMaterials = null; // index array via Material[ZTest,CullMode]

            private Material[,]
                m_ShadedMaterials = null; // index array via Material[ZTest,CullMode]

            private LineBatchJob[,] m_LineBatch = null;

#if UNITY_EDITOR
            private int m_EditorPauseFrame = -1;
#endif

            private void Awake()
            {
                m_ColoredMaterials =
                    CreateMaterialArray(
                        "Hidden/Internal-Colored"); // Added to "Always Included Shaders" in the Graphics settings by Unity
                m_ShadedMaterials =
                    CreateMaterialArray(
                        "Hidden/DbgDraw-Shaded"); // Added to "Always Included Shaders" in the Graphics settings by DbgDrawBuildProcessor

                m_LineBatch = new LineBatchJob[2, 3];
                for (int y = 0; y < m_LineBatch.GetLength(0); ++y)
                {
                    for (int x = 0; x < m_LineBatch.GetLength(1); ++x)
                    {
                        m_LineBatch[y, x].list = new List<LineBatchJob.Line>();
                        m_LineBatch[y, x].material = m_ColoredMaterials[y, x];
                    }
                }

                gameObject.AddComponent<PreDbgDrawBehaviour>().debugDrawBehaviour = this;
                RenderPipelineManager.endContextRendering +=
                    OnRenderPipelineManagerEndCameraRendering;
            }

            private void OnRenderPipelineManagerEndCameraRendering(ScriptableRenderContext context, List<Camera> cameras)
            {
                if (!isActiveAndEnabled)
                {
                    return;
                }

                foreach (Camera cam in cameras)
                {
                    UnityEngine.Profiling.Profiler.BeginSample("DbgDraw.Render", cam);
                    Render(cam);
                    UnityEngine.Profiling.Profiler.EndSample();
                }
            }

            private Material[,] CreateMaterialArray(string shaderName)
            {
                var materials = new Material[2, 3];

                // depth test off
                materials[0, (int)CullMode.Off] = CreateMaterial(shaderName, CullMode.Off, false);
                materials[0, (int)CullMode.Front] =
                    CreateMaterial(shaderName, CullMode.Front, false);
                materials[0, (int)CullMode.Back] = CreateMaterial(shaderName, CullMode.Back, false);

                // depth test on
                materials[1, (int)CullMode.Off] = CreateMaterial(shaderName, CullMode.Off, true);
                materials[1, (int)CullMode.Front] =
                    CreateMaterial(shaderName, CullMode.Front, true);
                materials[1, (int)CullMode.Back] = CreateMaterial(shaderName, CullMode.Back, true);

                return materials;
            }

            private Material CreateMaterial(string shaderName, CullMode cullMode, bool depthTest)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    // This should not occur, but if it does, we try to find a fallback shader
                    Debug.LogError(
                        $"{nameof(DbgDraw)}: Cannot find shader '{shaderName}'. {nameof(DbgDraw)} will not work correctly.");
                    foreach (string fallback in new[] { "Hidden/Internal-Colored", "Unlit/Color" })
                    {
                        shader = Shader.Find(shaderName);
                        if (shader != null)
                        {
                            break;
                        }
                    }
                }

                Material material = new(shader);

                material.SetColor(COLOR_ID, Color.white);
                material.SetInt(SRCBLEND_ID, (int)BlendMode.SrcAlpha);
                material.SetInt(DSTBLEND_ID, (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt(CULL_ID, (int)cullMode);
                material.SetInt(ZWRITE_ID, 0);
                if (!depthTest)
                {
                    material.SetInt(ZTEST_ID, 0);
                }

                ReleaseOnDestroy(material);
                return material;
            }

            public void ReleaseOnDestroy(Object obj)
            {
                m_ReleaseOnDestroy.Add(obj);
            }

            private void OnDestroy()
            {
                for (int n = 0; n < m_ReleaseOnDestroy.Count; ++n)
                {
                    if (m_ReleaseOnDestroy[n] != null)
                    {
                        Destroy(m_ReleaseOnDestroy[n]);
                    }

                    m_ReleaseOnDestroy[n] = null;
                }

                m_ReleaseOnDestroy.Clear();

                m_ReleaseOnDestroy = null;
                m_PrimitiveJobs = null;
                m_VertexListCache = null;
                m_MeshJobs = null;
                m_ShadedMaterials = null;
                m_ColoredMaterials = null;
                m_LineBatch = null;
                RenderPipelineManager.endContextRendering -= OnRenderPipelineManagerEndCameraRendering;
            }

            private void OnRenderObject()
            {
                // TODO(BDG): remove? is this triggered in URP?
                Camera camera = Camera.current;
                UnityEngine.Profiling.Profiler.BeginSample("DbgDraw.Render", camera);
                Render(camera);
                UnityEngine.Profiling.Profiler.EndSample();
            }

            private void Render(Camera camera)
            {
                if (!IsEnabledAndSupported || camera == null)
                {
                    return;
                }

                // Render only in game or scene view. If we don't to this, we also render
                // stuff in the frame debugger mesh preview window for example.
                bool validCamera = (camera.cameraType == CameraType.Game && camera.CompareTag("MainCamera"))
                    || camera.cameraType == CameraType.SceneView;

                if (!validCamera)
                {
                    return;
                }

                // If you pause and then unpause the Unity editor, Time.unscaledDeltaTime gets
                // advanced the amount of time you paused the editor. In this case, any jobs with a
                // 'duration' often just vanish. Therefore we detect when the editor gets unpaused
                // and for this frame, we ignore the deltatime!
                float deltaTime = Time.unscaledDeltaTime;

#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPaused)
                {
                    m_EditorPauseFrame = Time.frameCount;
                }

                if (!UnityEditor.EditorApplication.isPaused && m_EditorPauseFrame == Time.frameCount - 1)
                {
                    deltaTime = 0;
                }
#endif

                UnityEngine.Profiling.Profiler.BeginSample("DbgDraw.DrawMeshJobs");
                DrawMeshJobs(deltaTime);
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("DbgDraw.DrawPrimitiveJobs");
                DrawPrimitiveJobs(deltaTime);
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("DbgDraw.DrawLines");
                DrawLines(deltaTime);
                UnityEngine.Profiling.Profiler.EndSample();
            }

            private void ResetMaterialColors()
            {
                ResetMaterialColors(m_ColoredMaterials);
                ResetMaterialColors(m_ShadedMaterials);
            }

            private void ResetMaterialColors(Material[,] materials)
            {
                for (int y = 0; y < materials.GetLength(0); y++)
                {
                    for (int x = 0; x < materials.GetLength(1); x++)
                    {
                        if (materials[y, x] != null)
                        {
                            materials[y, x].color = Color.white;
                        }
                    }
                }
            }

            private void DrawPrimitiveJobs(float deltaTime)
            {
                ResetMaterialColors();
                Material material = null;

                GL.PushMatrix();

                for (int k = 0; k < m_PrimitiveJobs.Count; ++k)
                {
                    PrimitiveJob job = m_PrimitiveJobs[k];

                    if (job.useMatrix)
                    {
                        GL.PushMatrix();
                        GL.MultMatrix(job.matrix);
                    }

                    if (job.material != material)
                    {
                        material = job.material;
                        material.color = Color.white;
                        material.SetPass(0);
                    }

                    GL.Begin(job.primitiveType);

                    for (int n = 0; n < job.list.Count; ++n)
                    {
                        if (job.useVertexColor || n == 0)
                        {
                            GL.Color(job.list[n].color);
                        }

                        GL.Vertex(job.list[n].position);
                    }

                    GL.End();

                    if (job.useMatrix)
                    {
                        GL.PopMatrix();
                    }

                    job.remainingDuration -= deltaTime;
                    m_PrimitiveJobs[k] = job;
                }

                GL.PopMatrix();
            }

            private void DrawMeshJobs(float deltaTime)
            {
                ResetMaterialColors();
                Material material = null;

                for (int k = 0; k < m_MeshJobs.Count; ++k)
                {
                    MeshJob job = m_MeshJobs[k];

                    if (job.material != material || material.color != job.color)
                    {
                        material = job.material;
                        material.color = job.color;
                        material.SetPass(0);
                    }

                    Graphics.DrawMeshNow(job.mesh, job.matrix);

                    job.remainingDuration -= deltaTime;
                    m_MeshJobs[k] = job;
                }
            }

            private void DrawLines(float deltaTime)
            {
                ResetMaterialColors();
                GL.PushMatrix();

                for (int y = 0; y < m_LineBatch.GetLength(0); ++y)
                {
                    for (int x = 0; x < m_LineBatch.GetLength(1); ++x)
                    {
                        List<LineBatchJob.Line> list = m_LineBatch[y, x].list;
                        if (list.Count == 0)
                        {
                            continue;
                        }

                        Material material = m_LineBatch[y, x].material;
                        if (material == null)
                        {
                            continue;
                        }

                        material.color = Color.white;
                        material.SetPass(0);

                        GL.Begin(GL.LINES);

                        for (int n = 0; n < list.Count; ++n)
                        {
                            LineBatchJob.Line line = list[n];

                            GL.Color(line.color);
                            GL.Vertex(line.a);
                            GL.Vertex(line.b);

                            line.remainingDuration -= deltaTime;
                            list[n] = line;
                        }

                        GL.End();
                    }
                }

                GL.PopMatrix();
            }

            private List<PrimitiveJob.Vertex> GetCachedVertexList()
            {
                if (m_VertexListCache.Count == 0)
                {
                    return new List<PrimitiveJob.Vertex>(32);
                }

                List<PrimitiveJob.Vertex> list = m_VertexListCache[m_VertexListCache.Count - 1];
                m_VertexListCache.RemoveAt(m_VertexListCache.Count - 1);
                return list;
            }

            public void RemoveDeadJobs()
            {
                for (int n = m_PrimitiveJobs.Count - 1; n >= 0; --n)
                {
                    PrimitiveJob job = m_PrimitiveJobs[n];
                    if (job.remainingDuration <= 0)
                    {
                        if (job.list != null)
                        {
                            job.list.Clear();
                            m_VertexListCache.Add(job.list);
                        }

                        m_PrimitiveJobs.RemoveAt(n);
                    }
                }

                for (int y = 0; y < m_LineBatch.GetLength(0); ++y)
                {
                    for (int x = 0; x < m_LineBatch.GetLength(1); ++x)
                    {
                        List<LineBatchJob.Line> list = m_LineBatch[y, x].list;
                        for (int n = list.Count - 1; n >= 0; --n)
                        {
                            LineBatchJob.Line line = list[n];
                            if (line.remainingDuration <= 0)
                            {
                                list.RemoveAt(n);
                            }
                        }
                    }
                }

                for (int n = m_MeshJobs.Count - 1; n >= 0; --n)
                {
                    MeshJob job = m_MeshJobs[n];
                    if (job.remainingDuration <= 0)
                    {
                        m_MeshJobs.RemoveAt(n);
                    }
                }
            }

            public LineBatchJob GetLineBatch(bool depthTest, CullMode cullMode)
            {
                LineBatchJob batch = m_LineBatch[depthTest ? 1 : 0, (int)cullMode];
                return batch;
            }

            public PrimitiveJob AllocPrimitiveJob(int primitiveType, float duration, bool depthTest,
                CullMode cullMode, bool shaded)
            {
                PrimitiveJob job = new(m_PrimitiveJobs);
                job.primitiveType = primitiveType;
                job.remainingDuration = duration;
                job.depthTest = depthTest;

                if (shaded)
                {
                    job.material = m_ShadedMaterials[depthTest ? 1 : 0, (int)cullMode];
                }
                else
                {
                    job.material = m_ColoredMaterials[depthTest ? 1 : 0, (int)cullMode];
                }

                job.list = GetCachedVertexList();
                return job;
            }

            public MeshJob AllocMeshJob(float duration, bool depthTest, CullMode cullMode,
                bool shaded)
            {
                MeshJob job = new(m_MeshJobs);
                job.remainingDuration = duration;
                job.depthTest = depthTest;
                job.color = Color.white;

                if (shaded)
                {
                    job.material = m_ShadedMaterials[depthTest ? 1 : 0, (int)cullMode];
                }
                else
                {
                    job.material = m_ColoredMaterials[depthTest ? 1 : 0, (int)cullMode];
                }

                return job;
            }
        }

        private static DbgDrawBehaviour instance
        {
            get
            {
                if (s_Instance != null)
                {
                    return s_Instance;
                }

                // NOTE(BDG): for reloads?
#if UNITY_EDITOR
    #if UNITY_2023_1_OR_NEWER
                s_Instance = Object.FindFirstObjectByType<DbgDrawBehaviour>();
    #else
                s_Instance = Object.FindObjectOfType<DbgDrawBehaviour>();
    #endif
#endif
                if (s_Instance == null)
                {
                    GameObject go = new("DbgDraw");
                    s_Instance = go.AddComponent<DbgDrawBehaviour>();
                    s_Instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                }

                return s_Instance;
            }
        }
    }
}