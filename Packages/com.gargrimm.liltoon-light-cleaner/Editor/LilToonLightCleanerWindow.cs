using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LilToonLightCleaner.Editor
{
    public sealed class LilToonLightCleanerWindow : EditorWindow
    {
        private const string RimProperty = "_UseRim";
        private const string BacklightProperty = "_UseBacklight";

        private readonly List<Material> materials = new List<Material>();
        private Vector2 scroll;
        private bool removeRim = true;
        private bool removeBacklight = true;
        private string status = "";

        [MenuItem("Tools/Gargrimm/lilToon Light Cleaner")]
        private static void Open()
        {
            GetWindow<LilToonLightCleanerWindow>("lilToon Cleaner");
        }

        private void OnEnable()
        {
            AddSelection();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Disable lilToon lights", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select or drag materials or GameObjects to collect their renderer materials, including inactive children. " +
                "Changes affect shared material assets with the corresponding properties.",
                MessageType.Info);

            removeRim = EditorGUILayout.ToggleLeft("Disable Rim Light (_UseRim)", removeRim);
            removeBacklight = EditorGUILayout.ToggleLeft("Disable Backlight (_UseBacklight)", removeBacklight);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add selection")) AddSelection();
                if (GUILayout.Button("Clear list"))
                {
                    materials.Clear();
                    status = "";
                }
            }

            DrawDropArea();

            EditorGUILayout.LabelField($"Materials ({materials.Count})", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(100));
            for (int i = materials.Count - 1; i >= 0; i--)
            {
                if (materials[i] == null)
                {
                    materials.RemoveAt(i);
                    continue;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    materials[i] = (Material)EditorGUILayout.ObjectField(materials[i], typeof(Material), false);
                    if (GUILayout.Button("×", GUILayout.Width(24))) materials.RemoveAt(i);
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(materials.Count == 0 || (!removeRim && !removeBacklight)))
            {
                if (GUILayout.Button("Disable on all materials", GUILayout.Height(32))) Apply();
            }

            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);
        }

        private void DrawDropArea()
        {
            Rect area = GUILayoutUtility.GetRect(0, 46, GUILayout.ExpandWidth(true));
            GUI.Box(area, "Drag materials or GameObjects here", EditorStyles.helpBox);

            Event evt = Event.current;
            if (!area.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object item in DragAndDrop.objectReferences)
                        AddObject(item);
                }
                evt.Use();
            }
        }

        private void AddSelection()
        {
            foreach (Object item in Selection.objects)
                AddObject(item);

            Repaint();
        }

        private void AddObject(Object item)
        {
            if (item is Material material)
            {
                AddMaterial(material);
            }
            else if (item is GameObject gameObject)
            {
                foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
                    foreach (Material sharedMaterial in renderer.sharedMaterials)
                        AddMaterial(sharedMaterial);
            }
        }

        private void AddMaterial(Material material)
        {
            if (material != null && !materials.Contains(material)) materials.Add(material);
        }

        private void Apply()
        {
            int changed = 0;
            int skipped = 0;

            foreach (Material material in materials)
            {
                if (material == null)
                {
                    skipped++;
                    continue;
                }

                bool hasTarget = (removeRim && material.HasProperty(RimProperty)) ||
                                 (removeBacklight && material.HasProperty(BacklightProperty));
                if (!hasTarget)
                {
                    skipped++;
                    continue;
                }

                Undo.RecordObject(material, "Disable lilToon Rim Light and Backlight");
                if (removeRim && material.HasProperty(RimProperty)) material.SetFloat(RimProperty, 0f);
                if (removeBacklight && material.HasProperty(BacklightProperty)) material.SetFloat(BacklightProperty, 0f);
                EditorUtility.SetDirty(material);
                changed++;
            }

            AssetDatabase.SaveAssets();
            status = $"Done: {changed} material(s) modified; {skipped} skipped without matching properties.";
        }
    }
}
