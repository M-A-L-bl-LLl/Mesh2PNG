using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tools.Mesh2PNG
{
    [Serializable]
    internal sealed class CameraSettings
    {
        public Vector2 rotation = new(15f, -30f);
        public float   distance = 3f;

        public void Reset() { rotation = new(15f, -30f); distance = 3f; }
    }

    [Serializable]
    internal sealed class LightEntry
    {
        public bool    enabled   = true;
        public Color   color     = Color.white;
        public float   intensity = 1f;
        public Vector2 rotation  = new(50f, -30f);

        // SetCustomLighting ignores light.enabled, zeroing intensity is the workaround.
        public void ApplyTo(Light light)
        {
            light.enabled   = enabled;
            light.color     = color;
            light.intensity = enabled ? intensity : 0f;
            light.transform.rotation = Quaternion.Euler(rotation.x, rotation.y, 0f);
        }
    }

    [Serializable]
    internal sealed class LightingSettings
    {
        public Color      ambient = new(0.15f, 0.15f, 0.15f, 1f);
        public LightEntry light0  = new() { intensity = 1f,   rotation = new(50f,  -30f) };
        public LightEntry light1  = new() { intensity = 0.5f, rotation = new(-30f,  60f),
                                            color = new Color(0.8f, 0.85f, 1f) };
    }

    [Serializable]
    internal sealed class HierarchyState
    {
        // Relative transform paths of disabled children, e.g. "Armature/Body".
        public List<string> disabledPaths = new();

        public bool IsDisabled(string path) => disabledPaths.Contains(path);

        public void Set(string path, bool disabled)
        {
            if (disabled  && !disabledPaths.Contains(path)) disabledPaths.Add(path);
            if (!disabled) disabledPaths.Remove(path);
        }
    }

    // All per-object data in one place instead of parallel lists.
    [Serializable]
    internal sealed class ObjectEntry
    {
        public GameObject       target               = null;
        public CameraSettings   camera               = new();
        public LightingSettings lighting             = new();
        public HierarchyState   hierarchy            = new();
        public string           selectedChildPath    = null;  // null = none, "" = root
        public float            savedCameraDistance  = -1f;  // whole-object distance before child focus
    }
}
