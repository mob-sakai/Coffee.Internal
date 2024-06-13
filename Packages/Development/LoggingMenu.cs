using System.Linq;
using UnityEditor;

namespace Coffee.Development
{
    internal static class LoggingMenu
    {
        private const string k_EnableSymbol = "ENABLE_COFFEE_LOGGER";
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
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                .Split(';', ',');
        }

        private static void SetSymbols(string[] symbols)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
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
