using System.Linq;
using UnityEditor;
#if UNITY_6000_3_OR_NEWER
using UnityEditor.Build;
#endif

namespace Coffee.InternalEditor
{
    internal static class ScriptSymbolMenu
    {
        private const string k_EnableSymbol = "MULTI_CURVE_EDITOR_DEV";
        private const string k_EnableLoggingText = "Development/" + k_EnableSymbol;

        [MenuItem(k_EnableLoggingText, false, 20)]
        private static void EnableLogging()
        {
            SwitchSymbol(k_EnableSymbol);
        }

        [MenuItem(k_EnableLoggingText, true, 20)]
        private static bool EnableLogging_Valid()
        {
            Menu.SetChecked(k_EnableLoggingText, HasSymbol(k_EnableSymbol));
            return true;
        }

        private static string[] GetSymbols()
        {
#if UNITY_6000_3_OR_NEWER
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            return PlayerSettings.GetScriptingDefineSymbols(namedTarget)
#else
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
#endif
                .Split(';', ',');
        }

        private static void SetSymbols(string[] symbols)
        {
#if UNITY_6000_3_OR_NEWER
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbols(namedTarget,
#else
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
#endif
                string.Join(";", symbols));
        }

        private static bool HasSymbol(string symbol)
        {
            return GetSymbols().Contains(symbol);
        }

        private static void SwitchSymbol(string symbol)
        {
            var symbols = GetSymbols();
            SetSymbols(symbols.Any(x => x == symbol)
                ? symbols.Where(x => x != symbol).ToArray()
                : symbols.Concat(new[] { symbol }).ToArray()
            );
        }
    }
}
