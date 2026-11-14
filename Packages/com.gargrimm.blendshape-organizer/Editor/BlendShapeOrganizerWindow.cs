using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace BlendShapeOrganizer
{
    /// <summary>A searchable alternative to the built-in blend shape inspector.</summary>
    public sealed class BlendShapeOrganizerWindow : EditorWindow
    {
        private sealed class Group
        {
            public readonly string Name;
            public readonly string Key;
            public readonly List<int> Indices = new List<int>();
            public Group(string key, string name) { Key = key; Name = name; }
        }

        // Accept one or more equals signs on each side, with optional whitespace.
        private static readonly Regex HeaderPattern = new Regex(@"^\s*=+\s*(.*?)\s*=+\s*$", RegexOptions.Compiled);
        private static readonly Dictionary<string, float> copiedWeights = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> expanded = new Dictionary<string, bool>();
        private SkinnedMeshRenderer targetRenderer;
        private Vector2 scroll;
        private string search = string.Empty;
        private bool followSelection = true;

        [MenuItem("Window/Blend Shape Organizer")]
        private static void Open()
        {
            GetWindow<BlendShapeOrganizerWindow>("Blend Shapes").Show();
        }

        [MenuItem("CONTEXT/SkinnedMeshRenderer/Open Blend Shape Organizer")]
        private static void OpenForRenderer(MenuCommand command)
        {
            var window = GetWindow<BlendShapeOrganizerWindow>("Blend Shapes");
            window.targetRenderer = command.context as SkinnedMeshRenderer;
            window.followSelection = false;
            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            Undo.undoRedoPerformed += Repaint;
            if (followSelection) UpdateSelection();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= Repaint;
        }

        private void OnSelectionChanged()
        {
            if (followSelection) UpdateSelection();
            Repaint();
        }

        private void UpdateSelection()
        {
            targetRenderer = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() : null;
        }

        // Reflect changes made by animation, scripts, or the normal inspector.
        private void OnInspectorUpdate() { Repaint(); }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            followSelection = EditorGUILayout.ToggleLeft("Follow selection", followSelection);
            if (EditorGUI.EndChangeCheck() && followSelection) UpdateSelection();

            EditorGUI.BeginChangeCheck();
            var chosen = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Renderer", targetRenderer,
                typeof(SkinnedMeshRenderer), true);
            if (EditorGUI.EndChangeCheck())
            {
                targetRenderer = chosen;
                followSelection = false;
            }

            search = EditorGUILayout.TextField("Search", search);
            if (targetRenderer == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject with a Skinned Mesh Renderer, or assign one above.", MessageType.Info);
                return;
            }

            Mesh mesh = targetRenderer.sharedMesh;
            if (mesh == null || mesh.blendShapeCount == 0)
            {
                EditorGUILayout.HelpBox("This renderer has no mesh with blend shapes.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy weights")) CopyWeights(mesh);
            using (new EditorGUI.DisabledScope(copiedWeights.Count == 0))
            {
                if (GUILayout.Button("Paste matching weights")) PasteWeights(mesh);
            }
            EditorGUILayout.EndHorizontal();
            List<Group> groups = BuildGroups(mesh);
            string filter = search.Trim();
            int visibleCount = 0;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (Group group in groups)
            {
                bool groupMatches = Contains(group.Name, filter);
                var matches = new List<int>();
                foreach (int index in group.Indices)
                {
                    if (filter.Length == 0 || groupMatches || Contains(mesh.GetBlendShapeName(index), filter))
                        matches.Add(index);
                }
                if (matches.Count == 0) continue;
                visibleCount += matches.Count;
                bool isExpanded;
                if (!expanded.TryGetValue(group.Key, out isExpanded)) isExpanded = true;
                isExpanded = EditorGUILayout.Foldout(isExpanded, group.Name + " (" + matches.Count + ")", true);
                expanded[group.Key] = isExpanded;
                if (!isExpanded) continue;
                EditorGUI.indentLevel++;
                foreach (int index in matches)
                {
                    string name = mesh.GetBlendShapeName(index);
                    float weight = targetRenderer.GetBlendShapeWeight(index);
                    EditorGUI.BeginChangeCheck();
                    float next = EditorGUILayout.Slider(name, weight, 0f, 100f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetRenderer, "Change Blend Shape");
                        targetRenderer.SetBlendShapeWeight(index, next);
                        EditorUtility.SetDirty(targetRenderer);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(targetRenderer);
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndScrollView();
            if (visibleCount == 0) EditorGUILayout.HelpBox("No blend shapes match the search.", MessageType.Info);
        }

        private void CopyWeights(Mesh mesh)
        {
            copiedWeights.Clear();
            for (int i = 0; i < mesh.blendShapeCount; i++)
                copiedWeights[mesh.GetBlendShapeName(i)] = targetRenderer.GetBlendShapeWeight(i);
            ShowNotification(new GUIContent("Copied " + copiedWeights.Count + " blend shapes"));
        }

        private void PasteWeights(Mesh mesh)
        {
            int matched = 0;
            bool recorded = false;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                float weight;
                if (!copiedWeights.TryGetValue(mesh.GetBlendShapeName(i), out weight)) continue;
                if (!recorded)
                {
                    Undo.RecordObject(targetRenderer, "Paste Blend Shape Weights");
                    recorded = true;
                }
                targetRenderer.SetBlendShapeWeight(i, weight);
                matched++;
            }
            if (recorded)
            {
                EditorUtility.SetDirty(targetRenderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetRenderer);
            }
            ShowNotification(new GUIContent("Pasted " + matched + " matching blend shapes"));
        }
        private static bool Contains(string value, string filter)
        {
            return value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static List<Group> BuildGroups(Mesh mesh)
        {
            var groups = new List<Group>();
            var byName = new Dictionary<string, Group>(StringComparer.OrdinalIgnoreCase);
            Group current = GetGroup("system:ungrouped", "Ungrouped", groups, byName);
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string name = mesh.GetBlendShapeName(i);
                Match header = HeaderPattern.Match(name);
                if (header.Success && !string.IsNullOrWhiteSpace(header.Groups[1].Value))
                    current = GetGroup("header:" + header.Groups[1].Value.Trim(), header.Groups[1].Value.Trim(), groups, byName);

                // VRChat shapes always have their own group, without changing the current group.
                Group destination = name.StartsWith("vrc.", StringComparison.OrdinalIgnoreCase)
                    ? GetGroup("system:vrc", "VRC", groups, byName) : current;
                destination.Indices.Add(i);
            }
            groups.RemoveAll(group => group.Indices.Count == 0);
            return groups;
        }

        private static Group GetGroup(string key, string name, List<Group> groups, Dictionary<string, Group> byName)
        {
            Group group;
            if (!byName.TryGetValue(key, out group))
            {
                group = new Group(key, name);
                byName.Add(key, group);
                groups.Add(group);
            }
            return group;
        }
    }
}


