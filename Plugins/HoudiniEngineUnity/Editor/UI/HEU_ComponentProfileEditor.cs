using System.Collections.Generic;
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
            if (EditorGUI.EndChangeCheck())
            {
                components.ClearArray();
                serializedObject.FindProperty("AttributeBindings").ClearArray();
            }
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
            DrawAttributeBindings(components);
            serializedObject.ApplyModifiedProperties();
            string error = ((HEU_ComponentProfile)target).ValidateProfile();
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
            EditorGUILayout.HelpBox("Only root components are copied. Select all referenced root components. Template child references are not supported. Generated geometry components are protected.", MessageType.Info);
        }

        private void DrawAttributeBindings(SerializedProperty components)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Attribute Bindings", EditorStyles.boldLabel);
            SerializedProperty bindings = serializedObject.FindProperty("AttributeBindings");
            List<Component> sources = new List<Component>();
            List<string> labels = new List<string> { "Select component" };
            for (int i = 0; i < components.arraySize; ++i)
            {
                Component component = components.GetArrayElementAtIndex(i).objectReferenceValue as Component;
                if (component == null) continue;
                sources.Add(component);
                labels.Add(component.GetType().FullName + " [" + (i + 1) + "]");
            }
            for (int i = 0; i < bindings.arraySize; ++i)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    SerializedProperty binding = bindings.GetArrayElementAtIndex(i);
                    SerializedProperty source = binding.FindPropertyRelative("Component");
                    SerializedProperty path = binding.FindPropertyRelative("PropertyPath");
                    int oldIndex = sources.IndexOf(source.objectReferenceValue as Component) + 1;
                    int index = EditorGUILayout.Popup("Component", oldIndex, labels.ToArray());
                    if (index != oldIndex)
                    {
                        source.objectReferenceValue = index > 0 ? sources[index - 1] : null;
                        path.stringValue = "";
                    }
                    Component component = source.objectReferenceValue as Component;
                    List<string> paths = new List<string> { "" };
                    List<string> properties = new List<string> { string.IsNullOrEmpty(path.stringValue) ? "Select property" : "Missing: " + path.stringValue };
                    SerializedObject template = component != null ? new SerializedObject(component) : null;
                    if (template != null)
                    {
                        SerializedProperty property = template.GetIterator();
                        bool enterChildren = true;
                        while (property.Next(enterChildren))
                        {
                            enterChildren = property.propertyType == SerializedPropertyType.Generic && !property.isArray;
                            if (!HEU_ComponentProfile.IsBindingProperty(property)) continue;
                            paths.Add(property.propertyPath);
                            properties.Add(property.displayName + " — " + property.propertyPath + " (" + property.type + ")");
                        }
                    }
                    int oldProperty = System.Math.Max(0, paths.IndexOf(path.stringValue));
                    int chosen = EditorGUILayout.Popup("Property", oldProperty, properties.ToArray());
                    if (chosen != oldProperty) path.stringValue = paths[chosen];
                    SerializedProperty attribute = binding.FindPropertyRelative("Attribute");
                    EditorGUILayout.PropertyField(attribute, new GUIContent("Houdini Attribute"));
                    if (!attribute.stringValue.StartsWith("unity_cp_", System.StringComparison.Ordinal) || attribute.stringValue.Length <= 9)
                        EditorGUILayout.HelpBox("Use unity_cp_ followed by a name, e.g. unity_cp_mass. Root-only names can use unity_cp_root_.", MessageType.Warning);
                    SerializedProperty selected = template != null && !string.IsNullOrEmpty(path.stringValue) ? template.FindProperty(path.stringValue) : null;
                    bool array = selected != null && selected.isArray && selected.propertyType != SerializedPropertyType.String;
                    if (array)
                    {
                        EditorGUILayout.PropertyField(binding.FindPropertyRelative("ArrayElementSize"), new GUIContent("Array Element Size", "Values per element in the flat Houdini tuple: 1 for scalars, 3 for Vector3, etc."));
                        EditorGUILayout.HelpBox("The first source element's tuple becomes the list. HAPI variable-length array attributes are not supported.", MessageType.Info);
                    }
                    if (array || selected != null && selected.propertyType == SerializedPropertyType.Quaternion)
                        EditorGUILayout.PropertyField(binding.FindPropertyRelative("EulerDegrees"), new GUIContent("Quaternion From Euler Degrees"));
                    if (GUILayout.Button("Remove Binding"))
                    {
                        bindings.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }
            if (GUILayout.Button("Add Attribute Binding"))
            {
                int index = bindings.arraySize++;
                SerializedProperty binding = bindings.GetArrayElementAtIndex(index);
                binding.FindPropertyRelative("Component").objectReferenceValue = sources.Count > 0 ? sources[0] : null;
                binding.FindPropertyRelative("PropertyPath").stringValue = "";
                binding.FindPropertyRelative("Attribute").stringValue = "unity_cp_";
                binding.FindPropertyRelative("ArrayElementSize").intValue = 1;
                binding.FindPropertyRelative("EulerDegrees").boolValue = false;
            }
        }
    }
}
