using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tools.Mesh2PNG
{
    public sealed class Mesh2PNGWindow : EditorWindow
    {
        private const float LeftPanelWidth   = 230f;
        private const float MinPreviewHeight = 200f;

        // Menu

        [MenuItem("Tools/Mesh2PNG")]
        public static void Open()
        {
            var w = GetWindow<Mesh2PNGWindow>();
            w.titleContent = new GUIContent("Mesh2PNG", Mesh2PNGIcon.Get());
            w.minSize      = new Vector2(560f, 440f);
        }

        // Serialized state

        [SerializeField] private List<ObjectEntry> _entries         = new();
        [SerializeField] private int               _width           = 512;
        [SerializeField] private int               _height          = 512;
        [SerializeField] private string            _outputFolder    = "Assets";
        [SerializeField] private bool              _outputFoldout   = true;
        [SerializeField] private bool              _lightingFoldout = true;
        [SerializeField] private bool              _cameraFoldout   = true;
        [SerializeField] private bool              _showBounds      = true;

        // Runtime state

        private int  _selectedIndex = -1;

        private bool    _isDragging;
        private Vector2 _listScrollPos;
        private Vector2 _hierarchyScrollPos;
        private Vector2 _settingsScrollPos;

        private Mesh2PNGPreview _preview;

        // Shortcuts

        private bool             HasSelection  => (uint)_selectedIndex < (uint)_entries.Count;
        private ObjectEntry      SelectedEntry => HasSelection ? _entries[_selectedIndex] : null;
        private CameraSettings   Cam           => SelectedEntry?.camera;
        private LightingSettings Lighting      => SelectedEntry?.lighting;

        // Lifecycle

        private void OnEnable()
        {
            titleContent = new GUIContent("Mesh2PNG", Mesh2PNGIcon.Get());
            _preview = new Mesh2PNGPreview();
            _preview.Initialize();
            if (HasSelection) _preview.Spawn(_entries[_selectedIndex]);
        }

        private void OnDisable()
        {
            _preview?.Dispose();
            _preview = null;
        }

        // Object management

        private void SelectObject(int index)
        {
            _selectedIndex = index;
            _preview.Initialize(); // fresh utility, AddSingleGO has no remove
            if (HasSelection) _preview.Spawn(_entries[index]);
            Repaint();
        }

        private void AddEntry()
        {
            _entries.Add(new ObjectEntry());
            SelectObject(_entries.Count - 1);
        }

        private void RemoveEntryAt(int index)
        {
            _entries.RemoveAt(index);
            if (_entries.Count == 0)
            {
                _selectedIndex = -1;
                _preview.DestroyInstance();
            }
            else
            {
                SelectObject(Mathf.Clamp(_selectedIndex, 0, _entries.Count - 1));
            }
        }

        private void Navigate(int direction)
        {
            if (_entries.Count == 0) return;
            var next = (_selectedIndex + direction + _entries.Count) % _entries.Count;
            SelectObject(next);
            _listScrollPos.y = next * EditorGUIUtility.singleLineHeight;
        }

        // GUI

        private void OnGUI()
        {
            DrawUpdateBanner();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLeftPanel();
                DrawRightPanel();
            }
        }

        private void DrawUpdateBanner()
        {
            if (Mesh2PNGUpdater.IsChecking)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label("  Checking for updates…", EditorStyles.miniLabel,
                        GUILayout.ExpandWidth(true));
                }
                EditorGUILayout.Space(2f);
            }
            else if (Mesh2PNGUpdater.UpdateAvailable || Mesh2PNGUpdater.IsInstalling)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var label = Mesh2PNGUpdater.IsInstalling
                        ? $"  Installing update…"
                        : $"  Update available:  v{Mesh2PNGUpdater.CurrentVersion}  →  v{Mesh2PNGUpdater.LatestVersion}";

                    GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

                    EditorGUI.BeginDisabledGroup(Mesh2PNGUpdater.IsInstalling);
                    if (GUILayout.Button("Install", GUILayout.Width(60), GUILayout.Height(18)))
                        Mesh2PNGUpdater.InstallLatest();
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.Space(2f);

                if (Mesh2PNGUpdater.IsInstalling) Repaint();
            }
            else if (!string.IsNullOrEmpty(Mesh2PNGUpdater.CheckError))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(
                        $"  Update check failed: {Mesh2PNGUpdater.CheckError}",
                        EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                }
                EditorGUILayout.Space(2f);
            }
            else if (!string.IsNullOrEmpty(Mesh2PNGUpdater.LatestVersion))
            {
                var elapsed = (System.DateTime.UtcNow - Mesh2PNGUpdater.CheckCompletedAt).TotalSeconds;
                if (elapsed < 5.0)
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label(
                            $"  Mesh2PNG v{Mesh2PNGUpdater.CurrentVersion} — up to date ✓",
                            EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                    }
                    EditorGUILayout.Space(2f);
                    Repaint();
                }
            }
        }

        // Left panel

        private void DrawLeftPanel()
        {
            using (new EditorGUILayout.VerticalScope(
                EditorStyles.helpBox,
                GUILayout.Width(LeftPanelWidth),
                GUILayout.ExpandHeight(true)))
            {
                DrawObjectList();
                DrawHierarchyPanel();
            }
        }

        private void DrawObjectList()
        {
            EditorGUILayout.LabelField("Objects", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            _listScrollPos = EditorGUILayout.BeginScrollView(_listScrollPos, GUILayout.MaxHeight(150f));
            for (var i = 0; i < _entries.Count; i++)
                DrawObjectRow(i);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("+ Add Object"))
                AddEntry();
        }

        private void DrawObjectRow(int i)
        {
            var isSelected = i == _selectedIndex;
            var rowRect    = EditorGUILayout.BeginHorizontal();

            if (isSelected)
                EditorGUI.DrawRect(rowRect, new Color(0.3f, 0.55f, 0.9f, 0.25f));

            var entry = _entries[i];
            EditorGUI.BeginChangeCheck();
            entry.target = (GameObject)EditorGUILayout.ObjectField(entry.target, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck() && isSelected)
                SelectObject(i);

            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                EditorGUILayout.EndHorizontal();
                RemoveEntryAt(i);
                GUIUtility.ExitGUI();
                return;
            }

            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                SelectObject(i);
                Event.current.Use();
            }
        }

        private void DrawHierarchyPanel()
        {
            var instance = _preview?.Instance;
            if (instance == null || instance.transform.childCount == 0) return;

            EditorGUILayout.Space(4f);
            EditorGUI.DrawRect(
                GUILayoutUtility.GetRect(LeftPanelWidth, 1f),
                new Color(1f, 1f, 1f, 0.1f));
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Hierarchy", EditorStyles.boldLabel);

            _hierarchyScrollPos = EditorGUILayout.BeginScrollView(
                _hierarchyScrollPos, GUILayout.ExpandHeight(true));

            var entry   = SelectedEntry;
            var newPath = Mesh2PNGHierarchyDrawer.DrawNodes(
                instance.transform, entry?.hierarchy, entry?.selectedChildPath);

            EditorGUILayout.EndScrollView();

            if (entry != null && newPath != entry.selectedChildPath)
            {
                if (newPath != null)
                {
                    // Save the whole-object distance only on first child selection.
                    if (entry.selectedChildPath == null)
                        entry.savedCameraDistance = Cam?.distance ?? -1f;

                    entry.selectedChildPath = newPath;
                    if (Cam != null)
                        _preview.FitCameraTo(Cam, _preview.GetFocusBounds(newPath));
                }
                else
                {
                    // Restore the distance saved before child was selected.
                    if (Cam != null && entry.savedCameraDistance > 0f)
                        Cam.distance = entry.savedCameraDistance;
                    entry.savedCameraDistance = -1f;
                    entry.selectedChildPath   = null;
                }
                Repaint();
            }
        }

        // Right panel

        private void DrawRightPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                DrawNavigationBar();

                var previewRect = GUILayoutUtility.GetRect(
                    GUIContent.none, GUIStyle.none,
                    GUILayout.ExpandWidth(true),
                    GUILayout.ExpandHeight(true),
                    GUILayout.MinHeight(MinPreviewHeight));

                DrawPreview(previewRect);
                DrawSettingsPanel();
            }
        }

        private void DrawNavigationBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var hasObjects = _entries.Count > 0;
                EditorGUI.BeginDisabledGroup(!hasObjects);

                if (GUILayout.Button("‹", EditorStyles.toolbarButton, GUILayout.Width(28)))
                    Navigate(-1);

                GUILayout.FlexibleSpace();

                var label = hasObjects && HasSelection
                    ? $"{(SelectedEntry.target != null ? SelectedEntry.target.name : "None")}  ({_selectedIndex + 1} / {_entries.Count})"
                    : "No objects";
                GUILayout.Label(label, EditorStyles.toolbarButton);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("›", EditorStyles.toolbarButton, GUILayout.Width(28)))
                    Navigate(+1);

                EditorGUI.EndDisabledGroup();

                _showBounds = GUILayout.Toggle(
                    _showBounds, "Bounds", EditorStyles.toolbarButton, GUILayout.Width(52));

                EditorGUI.BeginDisabledGroup(Mesh2PNGUpdater.IsChecking);
                var checkLabel = Mesh2PNGUpdater.IsChecking ? "…" : "↻";
                if (GUILayout.Button(new GUIContent(checkLabel, "Check for updates"),
                        EditorStyles.toolbarButton, GUILayout.Width(22)))
                    Mesh2PNGUpdater.CheckForUpdates();
                EditorGUI.EndDisabledGroup();
            }
        }

        // Preview

        private void DrawPreview(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            if (!_preview.IsReady)
            {
                GUI.Label(rect, "← Add an object and select it",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 });
                return;
            }

            var captureRect = ComputeCaptureRect(rect);

            var childPath = SelectedEntry?.selectedChildPath;
            HandlePreviewInput(captureRect);
            _preview.UpdateCameraTransform(Cam, childPath);
            _preview.DrawInRect(captureRect, _width, _height, Lighting, childPath, _showBounds);

            DrawCaptureOverlay(rect, captureRect);

            GUI.Label(
                new Rect(captureRect.x, captureRect.yMax - 18f, captureRect.width, 18f),
                $"{_width} × {_height}  |  Drag to rotate  |  Scroll to zoom",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel));
        }

        private Rect ComputeCaptureRect(Rect previewRect)
        {
            var targetAspect = (float)_width / Mathf.Max(_height, 1);
            var areaAspect   = previewRect.width / Mathf.Max(previewRect.height, 1f);

            float w, h;
            if (targetAspect > areaAspect) { w = previewRect.width;  h = w / targetAspect; }
            else                           { h = previewRect.height; w = h * targetAspect; }

            return new Rect(
                previewRect.x + (previewRect.width  - w) * 0.5f,
                previewRect.y + (previewRect.height - h) * 0.5f,
                w, h);
        }

        private static void DrawCaptureOverlay(Rect previewRect, Rect captureRect)
        {
            var dimColor    = new Color(0f, 0f, 0f, 0.55f);
            var borderColor = new Color(1f, 1f, 1f, 0.60f);
            const float b   = 1f;

            EditorGUI.DrawRect(new Rect(previewRect.x,    previewRect.y, captureRect.x       - previewRect.x,    previewRect.height), dimColor);
            EditorGUI.DrawRect(new Rect(captureRect.xMax, previewRect.y, previewRect.xMax     - captureRect.xMax, previewRect.height), dimColor);
            EditorGUI.DrawRect(new Rect(captureRect.x,    previewRect.y, captureRect.width,    captureRect.y      - previewRect.y),    dimColor);
            EditorGUI.DrawRect(new Rect(captureRect.x,    captureRect.yMax, captureRect.width, previewRect.yMax   - captureRect.yMax), dimColor);

            EditorGUI.DrawRect(new Rect(captureRect.x,        captureRect.y,        captureRect.width, b), borderColor);
            EditorGUI.DrawRect(new Rect(captureRect.x,        captureRect.yMax - b, captureRect.width, b), borderColor);
            EditorGUI.DrawRect(new Rect(captureRect.x,        captureRect.y,        b, captureRect.height), borderColor);
            EditorGUI.DrawRect(new Rect(captureRect.xMax - b, captureRect.y,        b, captureRect.height), borderColor);
        }

        private void HandlePreviewInput(Rect rect)
        {
            if (Cam == null) return;
            var e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDown when rect.Contains(e.mousePosition):
                    _isDragging = true;
                    e.Use();
                    break;
                case EventType.MouseUp:
                    _isDragging = false;
                    break;
                case EventType.MouseDrag when _isDragging:
                    Cam.rotation.x -= e.delta.y * 0.4f;
                    Cam.rotation.y += e.delta.x * 0.4f;
                    Repaint();
                    e.Use();
                    break;
                case EventType.ScrollWheel when rect.Contains(e.mousePosition):
                    Cam.distance = Mathf.Clamp(Cam.distance * (1f + e.delta.y * 0.05f), 0.1f, 500f);
                    Repaint();
                    e.Use();
                    break;
            }
        }

        // Settings

        private void DrawSettingsPanel()
        {
            _settingsScrollPos = EditorGUILayout.BeginScrollView(
                _settingsScrollPos, GUILayout.ExpandWidth(true));

            DrawOutputSection();
            DrawLightingSection();
            if (_entries.Count > 1 && Cam != null)
                DrawCameraSection();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Capture",     GUILayout.Height(28))) CaptureSelected();
                if (GUILayout.Button("Capture All", GUILayout.Height(28))) CaptureAll();
            }
            EditorGUILayout.Space(6f);
        }

        private void DrawOutputSection()
        {
            _outputFoldout = EditorGUILayout.Foldout(_outputFoldout, "Output", true, EditorStyles.foldoutHeader);
            if (!_outputFoldout) return;

            using (new EditorGUI.IndentLevelScope())
            {
                _width  = Mathf.Max(1, EditorGUILayout.IntField("Width",  _width));
                _height = Mathf.Max(1, EditorGUILayout.IntField("Height", _height));

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Folder");
                    EditorGUILayout.LabelField(
                        _outputFolder.Replace('\\', '/'),
                        EditorStyles.textField,
                        GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("…", GUILayout.Width(26)))
                    {
                        var chosen = EditorUtility.OpenFolderPanel("Output folder", _outputFolder, "");
                        if (!string.IsNullOrEmpty(chosen)) _outputFolder = chosen;
                    }
                }
            }
            EditorGUILayout.Space(2f);
        }

        private void DrawLightingSection()
        {
            var l = Lighting;
            if (l == null) return;

            _lightingFoldout = EditorGUILayout.Foldout(
                _lightingFoldout, "Lighting", true, EditorStyles.foldoutHeader);
            if (!_lightingFoldout) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginChangeCheck();

                l.ambient = EditorGUILayout.ColorField("Ambient", l.ambient);
                EditorGUILayout.Space(2f);
                DrawLightEntry("Light 1", l.light0);
                EditorGUILayout.Space(2f);
                DrawLightEntry("Light 2", l.light1);

                if (EditorGUI.EndChangeCheck()) Repaint();
            }
            EditorGUILayout.Space(2f);
        }

        private static void DrawLightEntry(string label, LightEntry entry)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                entry.enabled   = EditorGUILayout.Toggle("Enabled",     entry.enabled);
                entry.color     = EditorGUILayout.ColorField("Color",   entry.color);
                entry.intensity = EditorGUILayout.Slider("Intensity",   entry.intensity, 0f, 5f);
                var rot = entry.rotation;
                rot.x   = EditorGUILayout.Slider("Rotation X", rot.x, -180f, 180f);
                rot.y   = EditorGUILayout.Slider("Rotation Y", rot.y, -180f, 180f);
                entry.rotation = rot;
            }
        }

        private void DrawCameraSection()
        {
            _cameraFoldout = EditorGUILayout.Foldout(
                _cameraFoldout, "Camera", true, EditorStyles.foldoutHeader);
            if (!_cameraFoldout) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginChangeCheck();
                var rot = Cam.rotation;
                rot.x = EditorGUILayout.FloatField("Rotation X", rot.x);
                rot.y = EditorGUILayout.FloatField("Rotation Y", rot.y);
                if (EditorGUI.EndChangeCheck()) { Cam.rotation = rot; Repaint(); }

                EditorGUI.BeginChangeCheck();
                var dist = EditorGUILayout.FloatField("Distance", Cam.distance);
                if (EditorGUI.EndChangeCheck()) { Cam.distance = Mathf.Max(0.1f, dist); Repaint(); }

                if (GUILayout.Button("Reset Camera"))
                {
                    Cam.Reset();
                    if (_preview.Instance != null)
                        _preview.FitCameraTo(Cam, Mesh2PNGPreview.GetBounds(_preview.Instance));
                    Repaint();
                }
            }
            EditorGUILayout.Space(2f);
        }

        // Capture

        private void CaptureSelected()
        {
            if (!HasSelection || SelectedEntry.target == null)
            {
                Debug.LogWarning("[Mesh2PNG] No object selected.");
                return;
            }
            CaptureAt(_selectedIndex);
        }

        private void CaptureAll()
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].target == null) continue;
                SelectObject(i);
                CaptureAt(i);
            }
        }

        private void CaptureAt(int index)
        {
            var entry = _entries[index];
            if (!_preview.IsReady) _preview.Spawn(entry);
            _preview.UpdateCameraTransform(entry.camera, entry.selectedChildPath);

            var tex  = _preview.CaptureTransparent(_width, _height, entry.lighting);
            var path = Path.Combine(_outputFolder, entry.target.name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            DestroyImmediate(tex);

            Debug.Log($"[Mesh2PNG] Saved: {path}");
            AssetDatabase.Refresh();
        }
    }
}
