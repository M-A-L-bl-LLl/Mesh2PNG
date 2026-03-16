using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tools.Mesh2PNG
{
    internal sealed class Mesh2PNGPreview : IDisposable
    {
        private const float DefaultFov        = 30f;
        private const float FarClip           = 1000f;
        private const float NearClip          = 0.01f;
        private const float AutoFitMultiplier = 3f;
        private const float MinCameraDistance = 0.1f;

        private PreviewRenderUtility _utility;
        private GameObject           _instance;

        private static Material _lineMaterial;

        public bool       IsReady  => _utility != null && _instance != null;
        public GameObject Instance => _instance;

        // Lifecycle

        // Recreates the utility from scratch. Needed when switching objects because
        // AddSingleGO accumulates objects and there is no public way to clear them.
        public void Initialize()
        {
            DestroyInstance();
            _utility?.Cleanup();
            _utility = new PreviewRenderUtility();
            _utility.camera.fieldOfView   = DefaultFov;
            _utility.camera.farClipPlane  = FarClip;
            _utility.camera.nearClipPlane = NearClip;
        }

        public void Dispose()
        {
            DestroyInstance();
            _utility?.Cleanup();
            _utility = null;
        }

        // Object

        public void Spawn(ObjectEntry entry)
        {
            DestroyInstance();
            if (entry?.target == null) return;

            _instance           = UnityEngine.Object.Instantiate(entry.target);
            _instance.hideFlags = HideFlags.HideAndDontSave;
            _utility.AddSingleGO(_instance);

            RestoreHierarchyState(entry.hierarchy);
            AutoFitCamera(entry.camera);
        }

        public void DestroyInstance()
        {
            if (_instance != null) UnityEngine.Object.DestroyImmediate(_instance);
            _instance = null;
        }

        // Camera

        public void UpdateCameraTransform(CameraSettings cam, string selectedPath)
        {
            if (_instance == null || cam == null || _utility == null) return;

            var center = GetFocusBounds(selectedPath).center;
            var rot    = Quaternion.Euler(cam.rotation.x, cam.rotation.y, 0f);
            _utility.camera.transform.position = center + rot * new Vector3(0f, 0f, -cam.distance);
            _utility.camera.transform.LookAt(center);
        }

        public void FitCameraTo(CameraSettings cam, Bounds bounds)
        {
            cam.distance = Mathf.Max(bounds.extents.magnitude * AutoFitMultiplier, MinCameraDistance);
        }

        // null = whole object, "" = root transform
        public Bounds GetFocusBounds(string selectedPath)
        {
            if (selectedPath != null && _instance != null)
            {
                var t = selectedPath == ""
                    ? _instance.transform
                    : _instance.transform.Find(selectedPath);
                if (t != null) return GetBounds(t.gameObject);
            }
            return _instance != null ? GetBounds(_instance) : default;
        }

        // Lighting

        // Call before BeginPreview. Configures lights and Built-in ambient.
        public void ApplyLights(LightingSettings lighting)
        {
            if (lighting == null || _utility == null) return;

            _utility.ambientColor = lighting.ambient; // Built-in reads this before BeginPreview
            if (_utility.lights.Length > 0) lighting.light0.ApplyTo(_utility.lights[0]);
            if (_utility.lights.Length > 1) lighting.light1.ApplyTo(_utility.lights[1]);
        }

        // Call after BeginPreview. Sets ambient for both URP and Built-in RP.
        public void ApplyAmbient(LightingSettings lighting)
        {
            if (lighting == null) return;

            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = lighting.ambient;

            // Built-in RP reads ambientColor from the utility directly
            if (_utility != null)
                _utility.ambientColor = lighting.ambient;
        }

        // Rendering

        public void DrawInRect(Rect captureRect, int width, int height,
            LightingSettings lighting, string selectedPath, bool showBounds = true)
        {
            if (captureRect.width < 1 || captureRect.height < 1) return;

            _utility.camera.aspect = (float)width / Mathf.Max(height, 1);

            ApplyLights(lighting);

            _utility.BeginPreview(captureRect, GUIStyle.none);
            _utility.camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            _utility.camera.clearFlags      = CameraClearFlags.SolidColor;
            ApplyAmbient(lighting);
            _utility.camera.Render();
            if (showBounds) DrawSelectionHighlight(selectedPath);
            _utility.EndAndDrawPreview(captureRect);
        }

        // Render on black, render on white, reconstruct alpha from the difference.
        // Caller owns the returned texture and must DestroyImmediate it.
        public Texture2D CaptureTransparent(int width, int height, LightingSettings lighting)
        {
            var fullRect = new Rect(0, 0, width, height);

            var blackTex = RenderWithBackground(fullRect, width, height, Color.black, lighting);
            var whiteTex = RenderWithBackground(fullRect, width, height, Color.white, lighting);

            var result = CompositeTransparent(blackTex, whiteTex, width, height);

            UnityEngine.Object.DestroyImmediate(blackTex);
            UnityEngine.Object.DestroyImmediate(whiteTex);
            return result;
        }

        // Static utils

        public static Bounds GetBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            var b = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        // Private

        private Texture2D RenderWithBackground(Rect captureRect, int width, int height,
            Color background, LightingSettings lighting)
        {
            _utility.camera.clearFlags      = CameraClearFlags.SolidColor;
            _utility.camera.backgroundColor = background;

            ApplyLights(lighting);
            _utility.BeginPreview(captureRect, GUIStyle.none);
            ApplyAmbient(lighting);
            _utility.camera.Render();

            // ReadPixels reads linear values but EndAndDrawPreview applies gamma on screen.
            // Blit to a sRGB RT first so the saved PNG matches what is shown in the preview.
            var srgbRT = RenderTexture.GetTemporary(
                width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(_utility.camera.targetTexture, srgbRT);

            RenderTexture.active = srgbRT;
            var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            RenderTexture.ReleaseTemporary(srgbRT);
            _utility.EndPreview();
            return tex;
        }

        private static Texture2D CompositeTransparent(Texture2D blackTex, Texture2D whiteTex,
            int width, int height)
        {
            var black  = blackTex.GetPixels();
            var white  = whiteTex.GetPixels();
            var pixels = new Color[black.Length];

            for (var i = 0; i < black.Length; i++)
            {
                var alpha   = 1f - (white[i].r - black[i].r);
                pixels[i]   = alpha > 0f ? black[i] / alpha : Color.clear;
                pixels[i].a = alpha;
            }

            var result = new Texture2D(width, height, TextureFormat.ARGB32, false);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        private void DrawSelectionHighlight(string selectedPath)
        {
            if (selectedPath == null || _instance == null) return;

            var t = selectedPath == ""
                ? _instance.transform
                : _instance.transform.Find(selectedPath);
            if (t == null) return;

            var bounds = GetBounds(t.gameObject);
            if (bounds.size == Vector3.zero) return;

            GetLineMaterial().SetPass(0);

            GL.PushMatrix();
            GL.LoadProjectionMatrix(_utility.camera.projectionMatrix);
            GL.modelview = _utility.camera.worldToCameraMatrix;
            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 0.75f, 0.1f, 0.9f));

            DrawWireBox(bounds.min, bounds.max);

            GL.End();
            GL.PopMatrix();
        }

        private static void DrawWireBox(Vector3 mn, Vector3 mx)
        {
            // bottom
            GlLine(mn.x, mn.y, mn.z,  mx.x, mn.y, mn.z);
            GlLine(mx.x, mn.y, mn.z,  mx.x, mn.y, mx.z);
            GlLine(mx.x, mn.y, mx.z,  mn.x, mn.y, mx.z);
            GlLine(mn.x, mn.y, mx.z,  mn.x, mn.y, mn.z);
            // top
            GlLine(mn.x, mx.y, mn.z,  mx.x, mx.y, mn.z);
            GlLine(mx.x, mx.y, mn.z,  mx.x, mx.y, mx.z);
            GlLine(mx.x, mx.y, mx.z,  mn.x, mx.y, mx.z);
            GlLine(mn.x, mx.y, mx.z,  mn.x, mx.y, mn.z);
            // sides
            GlLine(mn.x, mn.y, mn.z,  mn.x, mx.y, mn.z);
            GlLine(mx.x, mn.y, mn.z,  mx.x, mx.y, mn.z);
            GlLine(mx.x, mn.y, mx.z,  mx.x, mx.y, mx.z);
            GlLine(mn.x, mn.y, mx.z,  mn.x, mx.y, mx.z);
        }

        private static void GlLine(float x0, float y0, float z0, float x1, float y1, float z1)
        {
            GL.Vertex3(x0, y0, z0);
            GL.Vertex3(x1, y1, z1);
        }

        private static Material GetLineMaterial()
        {
            if (_lineMaterial != null) return _lineMaterial;

            var shader    = Shader.Find("Hidden/Internal-Colored");
            _lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _lineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull",     (int)CullMode.Off);
            _lineMaterial.SetInt("_ZWrite",   0);
            _lineMaterial.SetInt("_ZTest",    (int)CompareFunction.Always);
            return _lineMaterial;
        }

        private void RestoreHierarchyState(HierarchyState state)
        {
            if (state == null || _instance == null) return;
            foreach (var path in state.disabledPaths)
            {
                var t = _instance.transform.Find(path);
                if (t != null) t.gameObject.SetActive(false);
            }
        }

        private void AutoFitCamera(CameraSettings cam)
        {
            if (_instance == null || cam == null) return;
            // Skip if the camera was already adjusted for this object.
            if (Mathf.Approximately(cam.distance, new CameraSettings().distance))
                FitCameraTo(cam, GetBounds(_instance));
        }
    }
}
