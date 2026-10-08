using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoudiniEngineUnity
{
    public partial class HEU_ComponentProfile
    {
        [Serializable]
        public class AttributeBinding
        {
            public Component Component;
            public string PropertyPath;
            public string Attribute = "unity_cp_";
            public bool EulerDegrees;
            // Array values are a flat Houdini tuple on ONE source element, not all primitives.
            public int ArrayElementSize = 1;
        }
        public List<AttributeBinding> AttributeBindings = new List<AttributeBinding>();

#if UNITY_EDITOR
        public static bool IsBindingProperty(SerializedProperty property)
        {
            if (property == null || !IsUserProperty(property.propertyPath) || !property.editable) return false;
            if (property.isArray && property.propertyType != SerializedPropertyType.String) return true;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float: case SerializedPropertyType.String:
                case SerializedPropertyType.Color: case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.LayerMask: case SerializedPropertyType.Enum:
                case SerializedPropertyType.Vector2: case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4: case SerializedPropertyType.Rect:
                case SerializedPropertyType.Character: case SerializedPropertyType.Bounds:
                case SerializedPropertyType.Quaternion: case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int: case SerializedPropertyType.RectInt:
                case SerializedPropertyType.BoundsInt: return true;
                default: return false;
            }
        }

        private static void BindingWarning(AttributeBinding binding, string reason)
        {
            HEU_Logger.LogWarning("Component binding '" + binding.Attribute + "' -> '" + binding.PropertyPath + "': " + reason);
        }

        private static void ApplyAttributeBindings(HEU_ComponentProfile profile,
            Dictionary<UnityEngine.Object, UnityEngine.Object> map, Func<string, HEU_OutputAttribute> resolve)
        {
            if (resolve == null || profile.AttributeBindings == null) return;
            foreach (AttributeBinding binding in profile.AttributeBindings)
            {
                if (binding == null) continue;
                try
                {
                    if (string.IsNullOrEmpty(binding.Attribute) || !binding.Attribute.StartsWith("unity_cp_", StringComparison.Ordinal)
                        || binding.Attribute.Length <= "unity_cp_".Length)
                    { BindingWarning(binding, "attribute names must start with unity_cp_ and have a suffix."); continue; }
                    UnityEngine.Object destination;
                    if (binding.Component == null || !map.TryGetValue(binding.Component, out destination))
                    { BindingWarning(binding, "select a component included in Components to Copy."); continue; }
                    HEU_OutputAttribute value = resolve(binding.Attribute);
                    if (value == null || value._count == 0) continue;
                    SerializedObject output = new SerializedObject(destination);
                    SerializedProperty property = output.FindProperty(binding.PropertyPath ?? "");
                    if (!IsBindingProperty(property))
                    { BindingWarning(binding, "property is missing, read-only or unsupported."); continue; }
                    WarnConflictingTuples(binding, value);
                    if (property.isArray && property.propertyType != SerializedPropertyType.String)
                    {
                        int width = binding.ArrayElementSize;
                        if (width < 1 || value._tupleSize % width != 0)
                            throw new ArgumentException("tuple size must be divisible by Array Element Size.");
                        property.arraySize = value._tupleSize / width;
                        for (int i = 0; i < property.arraySize; ++i)
                            SetBindingValue(property.GetArrayElementAtIndex(i), value, i * width, width, binding.EulerDegrees);
                    }
                    else SetBindingValue(property, value, 0, value._tupleSize, binding.EulerDegrees);
                    // A failed element conversion discards the whole staged binding, including array resizing.
                    output.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(destination);
                    if (PrefabUtility.IsPartOfPrefabInstance(destination)) PrefabUtility.RecordPrefabInstancePropertyModifications(destination);
                }
                catch (Exception ex) { BindingWarning(binding, ex.Message + " Binding skipped."); }
            }
        }

        private static void WarnConflictingTuples(AttributeBinding binding, HEU_OutputAttribute value)
        {
            for (int i = value._tupleSize; i < value._count * value._tupleSize; ++i)
            {
                int first = i % value._tupleSize;
                bool differs = value._stringValues != null ? value._stringValues[i] != value._stringValues[first]
                    : value._intValues != null ? value._intValues[i] != value._intValues[first]
                    : value._floatValues != null && value._floatValues[i] != value._floatValues[first];
                if (differs) { BindingWarning(binding, "conflicting values within one output; using the first source element."); return; }
            }
        }

        private static float BindingNumber(HEU_OutputAttribute value, int i)
        {
            float number;
            if (value._floatValues != null) number = value._floatValues[i];
            else if (value._intValues != null) number = value._intValues[i];
            else throw new ArgumentException("requires a numeric attribute.");
            if (float.IsNaN(number) || float.IsInfinity(number)) throw new ArgumentException("non-finite numeric value.");
            return number;
        }
        private static int BindingInt(HEU_OutputAttribute value, int i)
        {
            if (value._intValues == null) throw new ArgumentException("requires an integer attribute.");
            return value._intValues[i];
        }
        private static string BindingString(HEU_OutputAttribute value, int i)
        {
            if (value._stringValues == null) throw new ArgumentException("requires a string attribute.");
            return value._stringValues[i] ?? "";
        }
        private static void RequireWidth(int actual, int expected)
        {
            if (actual != expected) throw new ArgumentException("expected tuple width " + expected + ", got " + actual + ".");
        }
        private static Type BoundFieldType(SerializedProperty property)
        {
            Type type = property.serializedObject.targetObject.GetType();
            string[] tokens = property.propertyPath.Split('.');
            for (int i = 0; i < tokens.Length; ++i)
            {
                if (tokens[i] == "Array" && i + 1 < tokens.Length && tokens[i + 1].StartsWith("data[", StringComparison.Ordinal))
                {
                    type = type.IsArray ? type.GetElementType() : (type.IsGenericType ? type.GetGenericArguments()[0] : null);
                    ++i;
                    if (type == null) return null;
                    continue;
                }
                System.Reflection.FieldInfo field = null;
                for (Type owner = type; owner != null && field == null; owner = owner.BaseType)
                    field = owner.GetField(tokens[i], System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field == null) return null;
                type = field.FieldType;
            }
            return type;
        }

        private static void SetBindingValue(SerializedProperty p, HEU_OutputAttribute a, int offset, int width, bool euler)
        {
            if (!IsBindingProperty(p) || p.isArray && p.propertyType != SerializedPropertyType.String)
                throw new ArgumentException("unsupported array element or property type.");
            Func<int, float> f = i => BindingNumber(a, offset + i);
            Func<int, int> n = i => BindingInt(a, offset + i);
            switch (p.propertyType)
            {
                case SerializedPropertyType.Float: RequireWidth(width, 1); p.doubleValue = f(0); break;
                case SerializedPropertyType.Integer:
                    RequireWidth(width, 1); p.longValue = n(0); break;
                case SerializedPropertyType.LayerMask:
                    RequireWidth(width, 1); p.intValue = n(0); break;
                case SerializedPropertyType.Character:
                    RequireWidth(width, 1); string character = BindingString(a, offset);
                    if (character.Length != 1) throw new ArgumentException("character requires exactly one character.");
                    p.intValue = character[0]; break;
                case SerializedPropertyType.Boolean:
                    RequireWidth(width, 1); int boolean = n(0);
                    if (boolean != 0 && boolean != 1) throw new ArgumentException("boolean must be integer 0 or 1.");
                    p.boolValue = boolean == 1; break;
                case SerializedPropertyType.String: RequireWidth(width, 1); p.stringValue = BindingString(a, offset); break;
                case SerializedPropertyType.Enum:
                    RequireWidth(width, 1);
                    if (a._stringValues != null)
                    {
                        int index = Array.IndexOf(p.enumNames, BindingString(a, offset));
                        if (index < 0) throw new ArgumentException("unknown enum name.");
                        p.enumValueIndex = index;
                    }
                    else p.intValue = n(0);
                    break;
                case SerializedPropertyType.Vector2: RequireWidth(width, 2); p.vector2Value = new Vector2(f(0), f(1)); break;
                case SerializedPropertyType.Vector3: RequireWidth(width, 3); p.vector3Value = new Vector3(f(0), f(1), f(2)); break;
                case SerializedPropertyType.Vector4: RequireWidth(width, 4); p.vector4Value = new Vector4(f(0), f(1), f(2), f(3)); break;
                case SerializedPropertyType.Vector2Int: RequireWidth(width, 2); p.vector2IntValue = new Vector2Int(n(0), n(1)); break;
                case SerializedPropertyType.Vector3Int: RequireWidth(width, 3); p.vector3IntValue = new Vector3Int(n(0), n(1), n(2)); break;
                case SerializedPropertyType.Color:
                    if (width != 3 && width != 4) throw new ArgumentException("color requires RGB or RGBA.");
                    p.colorValue = new Color(f(0), f(1), f(2), width == 4 ? f(3) : 1f); break;
                case SerializedPropertyType.Quaternion:
                    RequireWidth(width, euler ? 3 : 4);
                    if (euler) p.quaternionValue = Quaternion.Euler(f(0), f(1), f(2));
                    else
                    {
                        Quaternion q = new Quaternion(f(0), f(1), f(2), f(3));
                        float magnitude = Mathf.Sqrt(q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w);
                        if (float.IsInfinity(magnitude) || magnitude < 0.000001f) throw new ArgumentException("zero quaternion is invalid.");
                        p.quaternionValue = new Quaternion(q.x/magnitude, q.y/magnitude, q.z/magnitude, q.w/magnitude);
                    }
                    break;
                case SerializedPropertyType.Rect: RequireWidth(width, 4); p.rectValue = new Rect(f(0), f(1), f(2), f(3)); break;
                case SerializedPropertyType.RectInt: RequireWidth(width, 4); p.rectIntValue = new RectInt(n(0), n(1), n(2), n(3)); break;
                case SerializedPropertyType.Bounds: RequireWidth(width, 6); p.boundsValue = new Bounds(new Vector3(f(0), f(1), f(2)), new Vector3(f(3), f(4), f(5))); break;
                case SerializedPropertyType.BoundsInt: RequireWidth(width, 6); p.boundsIntValue = new BoundsInt(new Vector3Int(n(0), n(1), n(2)), new Vector3Int(n(3), n(4), n(5))); break;
                case SerializedPropertyType.ObjectReference:
                    RequireWidth(width, 1); string path = BindingString(a, offset);
                    if (path.Length == 0) { p.objectReferenceValue = null; break; }
                    // Serialized PPtr type identifies the asset class even when the template value is null.
                    string typeName = p.type;
                    int start = typeName.IndexOf('<'), end = typeName.LastIndexOf('>');
                    if (start < 0 || end <= start) throw new ArgumentException("cannot resolve asset reference type.");
                    typeName = typeName.Substring(start + 1, end - start - 1).TrimStart('$');
                    Type assetType = BoundFieldType(p) ?? HEU_GeneralUtility.GetSystemTypeByName(typeName)
                        ?? HEU_GeneralUtility.GetSystemTypeByName("UnityEngine." + typeName);
                    if (assetType == null || !typeof(UnityEngine.Object).IsAssignableFrom(assetType))
                        throw new ArgumentException("cannot resolve asset reference type " + typeName + ".");
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, assetType);
                    if (asset == null) throw new ArgumentException("asset missing or wrong type: " + path);
                    p.objectReferenceValue = asset; break;
                default: throw new ArgumentException("unsupported serialized type: " + p.propertyType);
            }
        }
#endif
    }
}
