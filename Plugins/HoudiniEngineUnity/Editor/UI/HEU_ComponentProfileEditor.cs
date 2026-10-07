using UnityEditor;
using UnityEngine;

namespace HoudiniEngineUnity
{
    [CustomEditor(typeof(HEU_ComponentProfile))]
    public class HEU_ComponentProfileEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty template = serializedObject.FindProperty("TemplatePrefab");
            SerializedProperty components = serializedObject.FindProperty("ComponentsToCopy");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(template, new GUIContent("Template Prefab"));
            if (EditorGUI.EndChangeCheck()) components.ClearArray();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ExistingComponents"), new GUIContent("Existing Components"));
            GameObject prefab = template.objectReferenceValue as GameObject;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Components to Copy", EditorStyles.boldLabel);
            if (prefab != null && PrefabUtility.IsPartOfPrefabAsset(prefab) && prefab.transform.parent == null)
            {
                foreach (Component component in prefab.GetComponents<Component>())
                {
                    if (component == null) continue;
                    bool supported = HEU_ComponentProfile.IsSupported(component.GetType());
                    int found = -1;
                    for (int i = 0; i < components.arraySize; ++i)
                        if (components.GetArrayElementAtIndex(i).objectReferenceValue == component) found = i;
                    using (new EditorGUI.DisabledScope(!supported))
                    {
                        bool enabled = EditorGUILayout.ToggleLeft(component.GetType().FullName + (supported ? "" : " (protected)"), found >= 0);
                        if (enabled && found < 0)
                        {
                            int index = components.arraySize++;
                            components.GetArrayElementAtIndex(index).objectReferenceValue = component;
                        }
                        else if (!enabled && found >= 0)
                        {
                            components.GetArrayElementAtIndex(found).objectReferenceValue = null;
                            components.DeleteArrayElementAtIndex(found);
                        }
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
            string error = ((HEU_ComponentProfile)target).ValidateProfile();
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
            EditorGUILayout.HelpBox("Only root components are copied. Select all referenced root components. Template child references are not supported. Generated geometry components are protected.", MessageType.Info);
        }
    }
}
