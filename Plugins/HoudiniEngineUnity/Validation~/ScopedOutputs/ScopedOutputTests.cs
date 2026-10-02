using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HoudiniEngineUnity;

public class ScopedOutputTests
{
    private class IntSession : HEU_SessionBase
    {
        public Dictionary<string, int[]> Values = new Dictionary<string, int[]>();
        private string Key(string name, HAPI_AttributeOwner owner) { return owner + ":" + name; }
        public void Add(string name, HAPI_AttributeOwner owner, params int[] values) { Values[Key(name, owner)] = values; }
        public override bool GetAttributeInfo(int node, int part, string name, HAPI_AttributeOwner owner, ref HAPI_AttributeInfo info)
        {
            int[] values;
            info = new HAPI_AttributeInfo();
            if (!Values.TryGetValue(Key(name, owner), out values)) return true;
            info.exists = true; info.owner = owner; info.count = values.Length; info.tupleSize = 1;
            info.storage = HAPI_StorageType.HAPI_STORAGETYPE_INT;
            return true;
        }
        public override bool GetAttributeIntData(int node, int part, string name, ref HAPI_AttributeInfo info, int[] data, int start, int length)
        {
            Array.Copy(Values[Key(name, info.owner)], start, data, 0, length);
            return true;
        }
    }

    [TestCase(false, HAPI_AttributeOwner.HAPI_ATTROWNER_POINT)]
    [TestCase(false, HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL)]
    [TestCase(true, HAPI_AttributeOwner.HAPI_ATTROWNER_POINT)]
    [TestCase(true, HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM)]
    [TestCase(true, HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL)]
    public void PreserveInstanceFlagsSurvivesRepeatedPass(bool packed, HAPI_AttributeOwner owner)
    {
        var root = new GameObject("custom_name_suffix");
        var child = new GameObject("mesh"); child.transform.SetParent(root.transform, false);
        try
        {
            root.tag = "Player"; root.layer = 2;
            child.tag = "MainCamera";
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(root, UnityEditor.StaticEditorFlags.BatchingStatic);
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(child, UnityEditor.StaticEditorFlags.OccluderStatic);
            var session = new IntSession();
            session.Add(HEU_Defines.UNITY_USE_INSTANCE_FLAGS_ATTR, owner, 1);
            session.Add(HEU_PluginSettings.UnityStaticAttributeName, owner, 0);
            var reader = new HEU_OutputAttributeReader(session, 0, 0);
            var scope = new HEU_OutputAttributeScope { _gameObject = root, _isInstance = true,
                _isPackedInstance = packed, _instanceCount = 1, _points = new[] { 0 }, _primitives = new[] { 0 } };
            reader.ApplyFlags(scope); reader.ApplyFlags(scope);
            Assert.AreEqual(UnityEditor.StaticEditorFlags.BatchingStatic, UnityEditor.GameObjectUtility.GetStaticEditorFlags(root));
            Assert.AreEqual(UnityEditor.StaticEditorFlags.OccluderStatic, UnityEditor.GameObjectUtility.GetStaticEditorFlags(child));
            Assert.AreEqual("Player", root.tag); Assert.AreEqual(2, root.layer);
            Assert.AreEqual("MainCamera", child.tag);
            Assert.AreEqual("custom_name_suffix", root.name);
            Assert.AreSame(root.transform, child.transform.parent);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    [Test]
    public void PointZeroOverridesPreserveDetailDefaultAndMeshIgnoresInstanceSwitch()
    {
        var session = new IntSession();
        session.Add(HEU_Defines.UNITY_USE_INSTANCE_FLAGS_ATTR, HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL, 1);
        session.Add(HEU_Defines.UNITY_USE_INSTANCE_FLAGS_ATTR, HAPI_AttributeOwner.HAPI_ATTROWNER_POINT, 0, 1);
        var reader = new HEU_OutputAttributeReader(session, 0, 0);
        Assert.IsFalse(reader.UseInstanceFlags(new HEU_OutputAttributeScope { _isInstance = true, _points = new[] { 0 } }));
        Assert.IsTrue(reader.UseInstanceFlags(new HEU_OutputAttributeScope { _isInstance = true, _points = new[] { 1 } }));
        Assert.IsFalse(reader.UseInstanceFlags(new HEU_OutputAttributeScope { _points = new[] { 1 } }));
    }

    [Test]
    public void BakeFlagCopyPreservesPartialStaticMask()
    {
        var source = new GameObject("source"); var target = new GameObject("target");
        try
        {
            source.tag = "Player"; source.layer = 2;
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(source, UnityEditor.StaticEditorFlags.BatchingStatic);
            HEU_OutputAttributeReader.CopyOutputFlags(source, target);
            Assert.AreEqual(UnityEditor.StaticEditorFlags.BatchingStatic, UnityEditor.GameObjectUtility.GetStaticEditorFlags(target));
            Assert.AreEqual(source.tag, target.tag); Assert.AreEqual(source.layer, target.layer);
        }
        finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(target); }
    }

    [Test]
    public void PrimitiveValueOverridesDetailAndUsesOwnIndex()
    {
        var session = new IntSession();
        session.Add("property", HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM, 7, 9);
        session.Add("property", HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL, 22);
        var reader = new HEU_OutputAttributeReader(session, 0, 0);
        Assert.AreEqual(9, reader.IntValue(new HEU_OutputAttributeScope { _primitives = new[] { 1 } }, "property"));
    }

    [Test]
    public void PointInstancesHaveDifferentFlags()
    {
        var session = new IntSession();
        session.Add("flag", HAPI_AttributeOwner.HAPI_ATTROWNER_POINT, 0, 1);
        var reader = new HEU_OutputAttributeReader(session, 0, 0);
        Assert.AreEqual(0, reader.IntValue(new HEU_OutputAttributeScope { _isInstance = true, _points = new[] { 0 } }, "flag"));
        Assert.AreEqual(1, reader.IntValue(new HEU_OutputAttributeScope { _isInstance = true, _points = new[] { 1 } }, "flag"));
    }

    [Test]
    public void DetailDefaultBroadcastsToEveryInstance()
    {
        var session = new IntSession();
        session.Add("flag", HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL, 1);
        var reader = new HEU_OutputAttributeReader(session, 0, 0);
        Assert.AreEqual(1, reader.IntValue(new HEU_OutputAttributeScope { _isInstance = true, _points = new[] { 42 } }, "flag"));
    }

    [Test]
    public void MixedPropertyWarnsAndUsesFirstSelectedPrimitive()
    {
        var session = new IntSession();
        session.Add("flag", HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM, 9, 1, 0);
        var reader = new HEU_OutputAttributeReader(session, 0, 0);
        var scope = new HEU_OutputAttributeScope { _primitives = new[] { 1, 2 }, _path = "A/mesh" };
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Conflicting flag.*A/mesh"));
        Assert.AreEqual(1, reader.IntValue(scope, "flag"));
    }

    [Test]
    public void SelectedStoredAttributesExcludeOtherOutputs()
    {
        var session = new IntSession();
        session.Add("id", HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM, 10, 20, 30);
        var reader = new HEU_OutputAttributeReader(session, 0, 0);
        var selected = reader.Select(new HEU_OutputAttributeScope { _primitives = new[] { 0, 2 } }, "id");
        Assert.AreEqual(2, selected._count);
        CollectionAssert.AreEqual(new[] { 10, 30 }, selected._intValues);
    }

    [Test]
    public void TupleSelectionPreservesComponentsAndOrder()
    {
        CollectionAssert.AreEqual(new[] { 7f, 8f, 9f, 1f, 2f, 3f },
            HEU_OutputAttributeReader.Slice(new[] { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f }, new[] { 2, 0 }, 3));
    }

    [Test]
    public void MeshScopeCollectsOnlyOwnPrimitivesVerticesAndPoints()
    {
        var cache = new HEU_GenerateGeoCache { _faceCounts = new[] { 3, 3 }, _vertexList = new[] { 0, 1, 2, 2, 3, 4 } };
        var scope = HEU_OutputAttributeScope.ForMesh(null, cache, new[] { "A", "B" }, "B");
        CollectionAssert.AreEqual(new[] { 1 }, scope._primitives);
        CollectionAssert.AreEqual(new[] { 3, 4, 5 }, scope._vertices);
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, scope._points);
    }

    [Test]
    public void MeshFlagsDoNotOverwriteDescendantOutput()
    {
        var root = new GameObject("parent");
        var child = new GameObject("child"); child.transform.SetParent(root.transform, false);
        try
        {
            var session = new IntSession();
            session.Add(HEU_PluginSettings.UnityStaticAttributeName, HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM, 1, 0);
            var reader = new HEU_OutputAttributeReader(session, 0, 0);
            reader.ApplyFlags(new HEU_OutputAttributeScope { _gameObject = child, _primitives = new[] { 1 } });
            reader.ApplyFlags(new HEU_OutputAttributeScope { _gameObject = root, _primitives = new[] { 0 } });
            Assert.IsTrue(root.isStatic); Assert.IsFalse(child.isStatic);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    [Test]
    public void PathCollisionGroupsAreSeparateComponents()
    {
        var go = new GameObject("collision_only");
        try
        {
            var cache = new HEU_GenerateGeoCache { _isUnityPathCache = true };
            var output = new HEU_GeneratedOutputData { _gameObject = go };
            cache._colliderInfos.Add(new HEU_GenerateGeoCache.HEU_ColliderInfo {
                _colliderType = HEU_GenerateGeoCache.HEU_ColliderInfo.ColliderType.BOX, _colliderSize = Vector3.one });
            cache._colliderInfos.Add(new HEU_GenerateGeoCache.HEU_ColliderInfo {
                _colliderType = HEU_GenerateGeoCache.HEU_ColliderInfo.ColliderType.BOX, _colliderSize = Vector3.one * 2, _isTrigger = true });
            HEU_GenerateGeoCache.UpdateColliders(cache, output);
            var colliders = go.GetComponents<BoxCollider>();
            Assert.AreEqual(2, colliders.Length);
            Assert.IsFalse(colliders[0].isTrigger); Assert.IsTrue(colliders[1].isTrigger);
            HEU_GenerateGeoCache.UpdateColliders(cache, output);
            Assert.AreEqual(2, go.GetComponents<BoxCollider>().Length);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    [Test]
    public void PathCacheKeepsCollisionMembershipAndOriginalOffsets()
    {
        var cache = new HEU_GenerateGeoCache { _faceCounts = new[] { 3, 3 }, _vertexList = new[] { 0, 1, 2, 3, 4, 5 } };
        cache._groupSplitVertexIndices.Add("collision_geo", new[] { 0, 1, 2, 3, 4, 5 });
        cache._groupSplitFaceIndices.Add("collision_geo", new List<int> { 0, 1 });
        cache._groupVertexOffsets.Add("collision_geo", new List<int> { 0, 3 });
        var split = cache.CreateUnityPathCache(new[] { "A", "B" }, "B");
        CollectionAssert.AreEqual(new[] { 1 }, split._groupSplitFaceIndices["collision_geo"]);
        CollectionAssert.AreEqual(new[] { 3 }, split._groupVertexOffsets["collision_geo"]);
        CollectionAssert.AreEqual(new[] { -1, -1, -1, 3, 4, 5 }, split._groupSplitVertexIndices["collision_geo"]);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, cache._groupSplitVertexIndices["collision_geo"]);
    }

    [Test]
    public void CollisionOnlyVisibilityHandlesAllColliderTypes()
    {
        var go = new GameObject("collision_only");
        var part = ScriptableObject.CreateInstance<HEU_PartData>();
        try
        {
            var box = go.AddComponent<BoxCollider>();
            var sphere = go.AddComponent<SphereCollider>();
            part.SetGameObject(go);
            typeof(HEU_PartData).GetField("_hasUnityPathMeshes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(part, true);
            part.SetVisiblity(false);
            part.CalculateColliderState();
            Assert.IsFalse(box.enabled); Assert.IsFalse(sphere.enabled);
            part.SetVisiblity(true);
            part.CalculateColliderState();
            Assert.IsTrue(box.enabled); Assert.IsTrue(sphere.enabled);
        }
        finally { UnityEngine.Object.DestroyImmediate(part); UnityEngine.Object.DestroyImmediate(go); }
    }

    [TestCase("Source", "Default_Instance1", "Oak", null, "Oak")]
    [TestCase("Source", "Default_Instance1", null, "_large", "Source_large")]
    [TestCase("Source", "Default_Instance1", "Oak", "_large", "Oak_large")]
    [TestCase("Source", "Default_Instance1", "", "", "Default_Instance1")]
    public void PackedAndPrefabNamingHaveIdenticalRules(string source, string fallback, string name, string suffix, string expected)
    {
        Assert.AreEqual(expected, HEU_PartData.ResolveUnityInstanceName(source, fallback, name, suffix));
    }

    [Test]
    public void PackedPropertiesFallBackToPrimitiveOwner()
    {
        var session = new IntSession();
        session.Add("flag", HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM, 5, 8);
        session.Add("flag", HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL, 99);
        var scope = new HEU_OutputAttributeScope { _isInstance = true, _isPackedInstance = true,
            _instanceCount = 2, _points = new[] { 1 }, _primitives = new[] { 1 } };
        Assert.AreEqual(8, new HEU_OutputAttributeReader(session, 0, 0).IntValue(scope, "flag"));
        session.Add("flag", HAPI_AttributeOwner.HAPI_ATTROWNER_POINT, 10, 11);
        Assert.AreEqual(11, new HEU_OutputAttributeReader(session, 0, 0).IntValue(scope, "flag"));
    }

    [Test]
    public void PackedMismatchedCountFallsBackWithoutGuessingIndices()
    {
        var session = new IntSession();
        session.Add("flag", HAPI_AttributeOwner.HAPI_ATTROWNER_POINT, 7);
        session.Add("flag", HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM, 5, 8);
        var scope = new HEU_OutputAttributeScope { _isInstance = true, _isPackedInstance = true,
            _instanceCount = 2, _points = new[] { 1 }, _primitives = new[] { 1 } };
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("does not match the packed transform count"));
        Assert.AreEqual(8, new HEU_OutputAttributeReader(session, 0, 0).IntValue(scope, "flag"));
    }

    [Test]
    public void PackedCustomNamesAreCleanedWithoutDestroyingSharedMesh()
    {
        var root = new GameObject("output");
        var source = new GameObject("source");
        var mesh = new Mesh();
        var part = ScriptableObject.CreateInstance<HEU_PartData>();
        try
        {
            source.AddComponent<MeshFilter>().sharedMesh = mesh;
            var copy = UnityEngine.Object.Instantiate(source, root.transform);
            copy.name = "MyCustomName";
            var unrelated = new GameObject("UserChild"); unrelated.transform.SetParent(root.transform, false);
            part.SetGameObject(root);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var tracked = (List<GameObject>)typeof(HEU_PartData).GetField("_packedInstances", flags).GetValue(part);
            tracked.Add(copy);
            part.ClearInstances();
            Assert.IsTrue(copy == null);
            Assert.IsTrue(mesh != null);
            Assert.AreSame(mesh, source.GetComponent<MeshFilter>().sharedMesh);
            Assert.IsTrue(unrelated != null);
            Assert.AreEqual(0, tracked.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(part); UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

}
