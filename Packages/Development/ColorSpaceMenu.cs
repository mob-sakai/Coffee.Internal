using UnityEditor;
using UnityEngine;

namespace Coffee.Development
{
    internal static class ColorSpaceMenu
    {
        private const string k_LinearText = "Development/Linear Color Space";
        private const string k_GammaText = "Development/Gamma Color Space";

        [MenuItem(k_LinearText, false, 1000)]
        private static void EnableLinear()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        [MenuItem(k_LinearText, true, 1000)]
        private static bool EnableLinear_Valid()
        {
            Menu.SetChecked(k_LinearText, PlayerSettings.colorSpace == ColorSpace.Linear);
            return true;
        }

        [MenuItem(k_GammaText, false, 1001)]
        private static void EnableGamma()
        {
            PlayerSettings.colorSpace = ColorSpace.Gamma;
        }

        [MenuItem(k_GammaText, true, 1001)]
        private static bool EnableGamma_Valid()
        {
            Menu.SetChecked(k_GammaText, PlayerSettings.colorSpace == ColorSpace.Gamma);
            return true;
        }
    }
}
