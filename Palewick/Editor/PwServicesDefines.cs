using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
namespace Palewick.EditorTools
{
    [InitializeOnLoad]
    public static class PwServicesDefines
    {
        private static readonly NamedBuildTarget[] Targets = { NamedBuildTarget.Android, NamedBuildTarget.Standalone };
        static PwServicesDefines()
        {
            EditorApplication.delayCall += Sync;
        }
        [MenuItem("Palewick/Refresh Service Defines")]
        public static void Sync()
        {
            bool admob = HasType("GoogleMobileAds.Api.MobileAds");
            bool gpgs = HasType("GooglePlayGames.PlayGamesPlatform");
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
        private static bool HasType(string fullName)
        {
            Assembly[] all = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < all.Length; i++)
            {
                try
                {
                    if (all[i].GetType(fullName, false) != null) return true;
                }
                catch (Exception) { }
            }
            return false;
        }
    }
}
