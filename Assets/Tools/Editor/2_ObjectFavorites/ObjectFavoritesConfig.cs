using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YuankunHuang.Tools.Editor
{
    public class ObjectFavoritesConfig : ScriptableObject
    {
        [System.Serializable]
        public class PageData
        {
            public string name;
            public List<string> globalObjIds = new List<string>();
        }

        public List<PageData> pages = new List<PageData>();

        private const string ConfigPath = "Assets/Tools/Editor/2_ObjectFavorites/ObjectFavoritesConfig.asset";

        public static ObjectFavoritesConfig LoadOrCreate()
        {
            var config = AssetDatabase.LoadAssetAtPath<ObjectFavoritesConfig>(ConfigPath);
            if (config == null)
            {
                config = CreateInstance<ObjectFavoritesConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
            }

            return config;
        }

        #region Helpers
        public static UnityEngine.Object ConvertGlobalIdToObject(string globalId)
        {
            if (string.IsNullOrEmpty(globalId)) return null;
            if (!GlobalObjectId.TryParse(globalId, out var gid)) return null; // validate
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
        }

        public static string ConvertObjectToGlobalId(UnityEngine.Object obj)
        {
            if(obj == null) return null;
            var gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            return gid.ToString();
        }
        #endregion
    }
}