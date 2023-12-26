using UnityEngine;

namespace Coffee.Internal.AssetModification
{
    internal interface IComponentModifier
    {
        bool isModified { get; }
        bool ModifyComponent(GameObject root, bool dryRun);
        string Report();
    }
}
