using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoudiniEngineUnity
{
    [CreateAssetMenu(fileName = "ComponentProfile", menuName = "Houdini Engine/Component Profile")]
    public class HEU_ComponentProfile : ScriptableObject
    {
        public enum ExistingComponentMode { KeepExisting, OverwriteSettings }
        public GameObject TemplatePrefab;
        public ExistingComponentMode ExistingComponents = ExistingComponentMode.OverwriteSettings;
        public List<Component> ComponentsToCopy = new List<Component>();

        public static bool IsSupported(Type type)
        {
            return type != null && !typeof(Transform).IsAssignableFrom(type)
                && !typeof(Renderer).IsAssignableFrom(type) && !typeof(MeshFilter).IsAssignableFrom(type)
                && !typeof(Collider).IsAssignableFrom(type) && !typeof(Collider2D).IsAssignableFrom(type)
                && !typeof(Terrain).IsAssignableFrom(type) && !typeof(LODGroup).IsAssignableFrom(type)
                && type.Namespace != "HoudiniEngineUnity" && !type.IsAbstract;
        }

#if UNITY_EDITOR
        private static bool IsUserProperty(string path)
        {
            return path != "m_Name" && path != "m_Script" && path != "m_GameObject" && path != "m_ObjectHideFlags"
                && path != "m_CorrespondingSourceObject" && path != "m_PrefabInstance"
                && path != "m_PrefabAsset" && path != "m_EditorClassIdentifier";
        }

        private static bool DependenciesSupported(Type type, HashSet<Type> visited)
        {
            if (typeof(Transform).IsAssignableFrom(type)) return true;
            if (!visited.Add(type)) return true;
            if (!IsSupported(type)) return false;
            foreach (RequireComponent requirement in type.GetCustomAttributes(typeof(RequireComponent), true))
            {
                foreach (System.Reflection.FieldInfo field in typeof(RequireComponent).GetFields(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    if (field.FieldType != typeof(Type)) continue;
                    Type dependency = field.GetValue(requirement) as Type;
                    if (dependency != null && !DependenciesSupported(dependency, visited)) return false;
                }
            }
            return true;
        }

        public string ValidateProfile()
        {
            if (TemplatePrefab == null || !PrefabUtility.IsPartOfPrefabAsset(TemplatePrefab)
                || TemplatePrefab.transform.parent != null)
                return "Assign a prefab asset root as the template.";
            if (ComponentsToCopy == null || ComponentsToCopy.Count == 0) return "Select at least one component.";
            HashSet<Component> selected = new HashSet<Component>();
            foreach (Component component in ComponentsToCopy)
            {
                if (component == null || component.gameObject != TemplatePrefab)
                    return "Every selected component must belong to the template prefab root.";
                if (!selected.Add(component)) return "The same component is selected more than once.";
                if (!IsSupported(component.GetType()) || !DependenciesSupported(component.GetType(), new HashSet<Type>()))
                    return component.GetType().Name + " is protected or requires a protected component.";
            }
            foreach (Component component in selected)
            {
                SerializedObject source = new SerializedObject(component);
                SerializedProperty property = source.GetIterator();
                while (property.Next(true))
                {
                    if (!IsUserProperty(property.propertyPath) || property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    UnityEngine.Object value = property.objectReferenceValue;
                    if (value == null || value == TemplatePrefab || value == TemplatePrefab.transform) continue;
                    Component linked = value as Component;
                    if (linked != null && selected.Contains(linked)) continue;
                    GameObject linkedGO = value as GameObject;
                    Transform transform = linked != null ? linked.transform : (linkedGO != null ? linkedGO.transform : null);
                    if (transform != null && (transform == TemplatePrefab.transform || transform.IsChildOf(TemplatePrefab.transform)))
                        return component.GetType().Name + "." + property.propertyPath + " references an unselected component or template child.";
                    if (!EditorUtility.IsPersistent(value)) return "Scene references are not supported: " + property.propertyPath;
                }
            }
            return null;
        }
#endif

        public static void Apply(string assetPath, GameObject target, GameObject sourceOutput = null)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(assetPath) || target == null) return;
            HEU_ComponentProfile profile = AssetDatabase.LoadAssetAtPath<HEU_ComponentProfile>(assetPath);
            if (profile == null)
            {
                HEU_Logger.LogWarning("Component profile not found: " + assetPath);
                return;
            }
            string error = profile.ValidateProfile();
            if (error != null)
            {
                HEU_Logger.LogWarning("Invalid component profile " + assetPath + ": " + error);
                return;
            }
            Dictionary<UnityEngine.Object, UnityEngine.Object> map = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
            map[profile.TemplatePrefab] = target;
            map[profile.TemplatePrefab.transform] = target.transform;
            if (sourceOutput != null)
            {
                map[sourceOutput] = target;
                map[sourceOutput.transform] = target.transform;
            }
            Dictionary<Component, Component> copySources = new Dictionary<Component, Component>();
            Dictionary<Type, int> ordinals = new Dictionary<Type, int>();
            HashSet<Component> write = new HashSet<Component>();
            HashSet<Component> original = new HashSet<Component>(target.GetComponents<Component>());
            Dictionary<Component, string> backups = new Dictionary<Component, string>();
            try
            {
                // Allocate all counterparts before copying, so forward and circular references work.
                foreach (Component source in profile.ComponentsToCopy)
                {
                    Type type = source.GetType();
                    int index;
                    ordinals.TryGetValue(type, out index);
                    ordinals[type] = index + 1;
                    Component[] candidates = target.GetComponents(type);
                    Component destination = index < candidates.Length ? candidates[index] : target.AddComponent(type);
                    if (destination == null) throw new InvalidOperationException("Cannot add " + type.FullName);
                    map[source] = destination;
                    copySources[source] = source;
                    if (sourceOutput != null)
                    {
                        Component[] cooked = sourceOutput.GetComponents(type);
                        if (index < cooked.Length)
                        {
                            map[cooked[index]] = destination;
                            copySources[source] = cooked[index];
                        }
                    }
                    if (!original.Contains(destination) || profile.ExistingComponents == ExistingComponentMode.OverwriteSettings)
                    {
                        write.Add(source);
                        if (original.Contains(destination)) backups[destination] = EditorJsonUtility.ToJson(destination);
                    }
                }
                foreach (Component component in profile.ComponentsToCopy)
                {
                    Component destination = (Component)map[component];
                    SerializedObject output = new SerializedObject(destination);
                    if (write.Contains(component))
                    {
                        SerializedObject source = new SerializedObject(copySources[component]);
                        SerializedProperty property = source.GetIterator();
                        // Copy top-level serialized fields including arrays and hidden fields.
                        while (property.Next(false))
                        {
                            if (IsUserProperty(property.propertyPath)) output.CopyFromSerializedProperty(property);
                        }
                    }
                    if (original.Contains(destination) && !backups.ContainsKey(destination))
                        backups[destination] = EditorJsonUtility.ToJson(destination);
                    // Remap all nested references, including references in arrays and hidden fields.
                    SerializedProperty targetProperty = output.GetIterator();
                    while (targetProperty.Next(true))
                    {
                        if (!IsUserProperty(targetProperty.propertyPath)
                            || targetProperty.propertyType != SerializedPropertyType.ObjectReference) continue;
                        UnityEngine.Object replacement;
                        UnityEngine.Object reference = targetProperty.objectReferenceValue;
                        if (reference != null && map.TryGetValue(reference, out replacement))
                            targetProperty.objectReferenceValue = replacement;
                        else if (reference != null && sourceOutput != null && !EditorUtility.IsPersistent(reference))
                        {
                            Component linked = reference as Component;
                            GameObject linkedGO = reference as GameObject;
                            Transform linkedTransform = linked != null ? linked.transform : (linkedGO != null ? linkedGO.transform : null);
                            if (linkedTransform == null || (linkedTransform != target.transform && !linkedTransform.IsChildOf(target.transform)))
                            {
                                HEU_Logger.LogWarning("Component profile bake cleared an unsupported scene reference: "
                                    + component.GetType().Name + "." + targetProperty.propertyPath);
                                targetProperty.objectReferenceValue = null;
                            }
                        }
                    }
                    output.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(destination);
                    if (PrefabUtility.IsPartOfPrefabInstance(destination))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(destination);
                }
            }
            catch (Exception ex)
            {
                foreach (KeyValuePair<Component, string> backup in backups)
                    if (backup.Key != null) EditorJsonUtility.FromJsonOverwrite(backup.Value, backup.Key);
                Component[] current = target.GetComponents<Component>();
                for (int i = current.Length - 1; i >= 0; --i)
                    if (current[i] != null && !original.Contains(current[i])) UnityEngine.Object.DestroyImmediate(current[i]);
                HEU_Logger.LogWarning("Unable to apply component profile " + assetPath + ": " + ex.Message);
            }
#endif
        }
    }
}
