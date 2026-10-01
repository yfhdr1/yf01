using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Palewick.EditorTools
{
    public static class BuildWarningsFixer
    {
        [MenuItem("Tools/Palewick/Auto Fix Warnings")]
        public static void FixAllWarnings()
        {
            FixGrassTexture();
            EnablePreBakeCollision();
            RemoveTestRunFiles();
            AssetDatabase.Refresh();
            Debug.Log("Palewick: Build warnings fixed successfully!");
        }
        private static void FixGrassTexture()
        {
            string[] guids = AssetDatabase.FindAssets("NAT_Grass1_AS t:Texture2D");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && !importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
            }
        }
        private static void EnablePreBakeCollision()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset");
            if (assets != null && assets.Length > 0)
            {
                SerializedObject physicsSettings = new SerializedObject(assets[0]);
                SerializedProperty bakeProperty = physicsSettings.FindProperty("m_BakeCollisionMeshes");
                if (bakeProperty != null && !bakeProperty.boolValue)
                {
                    bakeProperty.boolValue = true;
                    physicsSettings.ApplyModifiedProperties();
                }
            }
        }
        private static void RemoveTestRunFiles()
        {
            string infoPath = "Assets/Resources/PerformanceTestRunInfo.json";
            string settingsPath = "Assets/Resources/PerformanceTestRunSettings.json";
            if (File.Exists(infoPath)) File.Delete(infoPath);
            if (File.Exists(infoPath + ".meta")) File.Delete(infoPath + ".meta");
            if (File.Exists(settingsPath)) File.Delete(settingsPath);
            if (File.Exists(settingsPath + ".meta")) File.Delete(settingsPath + ".meta");
        }
    }
}
