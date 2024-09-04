using UnityEditor;
using UnityEngine;

namespace Coffee.Development
{
    internal static class EditableContextMenu
    {
        private const string k_MenuPath = "CONTEXT/Object/Editable";

        [MenuItem(k_MenuPath, false, 2000)]
        private static void SwitchEditable(MenuCommand command)
        {
            command.context.hideFlags ^= HideFlags.NotEditable;
        }

        [MenuItem(k_MenuPath, true, 2000)]
        private static bool SwitchEditable_Valid(MenuCommand command)
        {
            var editable = (command.context.hideFlags & HideFlags.NotEditable) == 0;
            Menu.SetChecked(k_MenuPath, editable);
            return true;
        }
    }
}
