using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
namespace Palewick.EditorTools
{
    [InitializeOnLoad]
    public static class PwServicesDefines
    {
        private static readonly NamedBuildTarget[] Targets = { NamedBuildTarget.Android, NamedBuildTarget.Standalone };
        private static readonly string[] AdmobFolders = { "Assets/GoogleMobileAds", "Packages/com.google.ads.mobile" };
        private static readonly string[] GpgsFolders = { "Assets/GooglePlayGames", "Packages/com.google.play.games" };
        private static readonly string[] AdmobAssemblies = { "GoogleMobileAds", "GoogleMobileAds.Core" };
        private static readonly string[] GpgsAssemblies = { "GooglePlayGames", "com.google.play.games" };
        static PwServicesDefines()
        {
            EditorApplication.delayCall += Sync;
        }
        [MenuItem("Palewick/Refresh Service Defines")]
        public static void Sync()
        {
            bool admob = Found(AdmobFolders, AdmobAssemblies);
            bool gpgs = Found(GpgsFolders, GpgsAssemblies);
            for (int i = 0; i < Targets.Length; i++)
            {
                Apply(Targets[i], "PW_ADMOB", admob);
                Apply(Targets[i], "PW_GPGS", gpgs);
            }
        }
        private static void Apply(NamedBuildTarget target, string symbol, bool wanted)
        {
            string raw;
            try { raw = PlayerSettings.GetScriptingDefineSymbols(target); }
            catch (Exception) { return; }
            List<string> list = new List<string>(raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            bool has = list.Contains(symbol);
            if (has == wanted) return;
            if (wanted) list.Add(symbol);
            else list.Remove(symbol);
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list.ToArray()));
        }
        private static bool Found(string[] folders, string[] assemblies)
        {
            for (int i = 0; i < folders.Length; i++)
            {
                if (AssetDatabase.IsValidFolder(folders[i])) return true;
            }
            UnityEditor.Compilation.Assembly[] all;
            try { all = CompilationPipeline.GetAssemblies(AssembliesType.Editor); }
            catch (Exception) { return false; }
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                for (int j = 0; j < assemblies.Length; j++)
                {
                    if (all[i].name == assemblies[j]) return true;
                }
            }
            return false;
        }
    }
}
