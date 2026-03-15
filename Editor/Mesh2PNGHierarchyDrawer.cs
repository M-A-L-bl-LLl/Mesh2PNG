using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tools.Mesh2PNG
{
    // Draws the object hierarchy tree and returns the updated selected path.
    // null = nothing selected, "" = root, "Armature/Body" = child path.
    internal static class Mesh2PNGHierarchyDrawer
    {
        private const float IndentWidth = 14f;
        private const float ColCenter   =  7f;
        private const float LineWidth   =  1f;

        public static string DrawNodes(Transform root, HierarchyState state, string selectedPath)
        {
            var ctx = new DrawContext { Root = root, State = state, SelectedPath = selectedPath };
            DrawNode(ctx, root, depth: 0, activeColumns: new bool[32], isLast: true);
            return ctx.SelectedPath;
        }

        // Mutable traversal context shared across the recursive calls.
        private sealed class DrawContext
        {
            public Transform      Root;
            public HierarchyState State;
            public string         SelectedPath;
        }

        private static void DrawNode(DrawContext ctx, Transform node,
            int depth, bool[] activeColumns, bool isLast)
        {
            var isRoot     = node == ctx.Root;
            var path       = isRoot ? "" : GetTransformPath(ctx.Root, node);
            var isSelected = ctx.SelectedPath == path;

            var rowRect = EditorGUILayout.BeginHorizontal();

            if (isSelected && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rowRect, new Color(1f, 0.75f, 0.1f, 0.15f));

            if (!isRoot && Event.current.type == EventType.Repaint)
                DrawTreeLines(rowRect, depth, activeColumns, isLast);

            GUILayout.Space(depth * IndentWidth);

            if (!isRoot)
            {
                var isActive = node.gameObject.activeSelf;
                EditorGUI.BeginChangeCheck();
                var newActive = EditorGUILayout.Toggle(isActive, GUILayout.Width(16f));
                if (EditorGUI.EndChangeCheck())
                {
                    node.gameObject.SetActive(newActive);
                    ctx.State?.Set(path, !newActive);
                }
            }
            else
            {
                GUILayout.Space(20f); // align label with children that have a toggle
            }

            var labelStyle = node.gameObject.activeSelf
                ? EditorStyles.label
                : new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } };
            EditorGUILayout.LabelField(node.name, labelStyle);

            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                ctx.SelectedPath = isSelected ? null : path; // click again to deselect
                Event.current.Use();
            }

            for (var i = 0; i < node.childCount; i++)
            {
                var childIsLast = i == node.childCount - 1;
                var childCols   = (bool[])activeColumns.Clone();
                childCols[depth] = !childIsLast; // stay active as long as siblings follow
                DrawNode(ctx, node.GetChild(i), depth + 1, childCols, childIsLast);
            }
        }

        private static void DrawTreeLines(Rect rowRect, int depth, bool[] activeColumns, bool isLast)
        {
            var lineColor = new Color(0.55f, 0.55f, 0.55f, 0.45f);
            var cy        = rowRect.y + rowRect.height * 0.5f;

            // Vertical pass-throughs for ancestor columns that still have siblings below.
            for (var i = 0; i < depth - 1; i++)
            {
                if (!activeColumns[i]) continue;
                var x = rowRect.x + i * IndentWidth + ColCenter;
                EditorGUI.DrawRect(new Rect(x, rowRect.y, LineWidth, rowRect.height), lineColor);
            }

            var cx = rowRect.x + (depth - 1) * IndentWidth + ColCenter;

            // Top half, always drawn (comes down from parent).
            EditorGUI.DrawRect(new Rect(cx, rowRect.y, LineWidth, rowRect.height * 0.5f), lineColor);

            // Bottom half, only when more siblings follow.
            if (!isLast)
                EditorGUI.DrawRect(new Rect(cx, cy, LineWidth, rowRect.height * 0.5f), lineColor);

            // Horizontal elbow.
            EditorGUI.DrawRect(new Rect(cx, cy - LineWidth * 0.5f, IndentWidth, LineWidth), lineColor);
        }

        private static string GetTransformPath(Transform root, Transform child)
        {
            var parts   = new Stack<string>();
            var current = child;
            while (current != root)
            {
                parts.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", parts);
        }
    }
}
