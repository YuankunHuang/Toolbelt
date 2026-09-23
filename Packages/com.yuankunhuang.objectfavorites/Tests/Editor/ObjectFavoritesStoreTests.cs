using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Toolbelt.ObjectFavorites.Tests
{
    public class ObjectFavoritesStoreTests
    {
        private const string TempFolder = "Assets/ToolbeltObjectFavoritesTestTemp";

        private readonly List<Object> _created = new List<Object>();
        private readonly List<int> _indices = new List<int>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
            {
                if (obj != null && !AssetDatabase.Contains(obj))
                {
                    Object.DestroyImmediate(obj);
                }
            }
            _created.Clear();

            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }
        }

        private Object Named(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }

        [Test]
        public void VisibleIndices_EmptyKeywordShowsEverythingIncludingEmptySlots()
        {
            var objects = new List<Object> { Named("A"), null, Named("B") };
            ObjectFavoritesStore.GetVisibleIndices(objects, "", _indices);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, _indices);

            ObjectFavoritesStore.GetVisibleIndices(objects, null, _indices);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, _indices);
        }

        [Test]
        public void VisibleIndices_PointIntoTheUnfilteredList()
        {
            var objects = new List<Object> { Named("Player"), null, Named("EnemyBoss"), Named("enemy_01") };

            ObjectFavoritesStore.GetVisibleIndices(objects, "enemy", _indices);
            CollectionAssert.AreEqual(new[] { 2, 3 }, _indices, "plain text matches any part, ignoring case");

            ObjectFavoritesStore.GetVisibleIndices(objects, "Enemy_*", _indices);
            CollectionAssert.AreEqual(new[] { 3 }, _indices, "wildcards match the whole name");
        }

        [Test]
        public void GlobalId_RoundTripsAnAsset()
        {
            AssetDatabase.CreateFolder("Assets", "ToolbeltObjectFavoritesTestTemp");
            var material = new Material(Shader.Find("Hidden/InternalErrorShader"));
            var path = TempFolder + "/Favorite.mat";
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();

            var id = ObjectFavoritesStore.ConvertObjectToGlobalId(material);
            var resolved = ObjectFavoritesStore.ConvertGlobalIdToObject(id);

            // The resolved object is the same asset, not necessarily the same in-memory instance.
            Assert.IsFalse(string.IsNullOrEmpty(id));
            Assert.IsInstanceOf<Material>(resolved);
            Assert.AreEqual(path, AssetDatabase.GetAssetPath(resolved));
        }

        [Test]
        public void GlobalId_NullAndGarbageAreSafe()
        {
            Assert.IsNull(ObjectFavoritesStore.ConvertObjectToGlobalId(null));
            Assert.IsNull(ObjectFavoritesStore.ConvertGlobalIdToObject(null));
            Assert.IsNull(ObjectFavoritesStore.ConvertGlobalIdToObject("not a global id"));
        }
    }
}
