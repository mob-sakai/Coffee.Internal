using System.Text;

namespace Coffee.Internal.AssetModification
{
    internal interface ITextModifier
    {
        bool ModifyText(StringBuilder sb, string text);
    }
}
