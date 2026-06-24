using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
#if TMP_ENABLE
using TMPro;
#endif

namespace Coffee.Internal
{
    /// <summary>
    /// Utility class to duplicate the mesh and texture of the graphic for reuse in another system
    /// </summary>
    internal static class GraphicDuplicator
    {
        public static Texture GetMainTexture(Graphic graphic)
        {
#if TMP_ENABLE
            if (graphic is TextMeshProUGUI || graphic is TMP_SubMeshUI)
            {
                var cr = graphic.canvasRenderer;
                if (cr == null || cr.materialCount == 0) return null;

                return cr.GetMaterial(0).mainTexture;
            }
#endif
            return graphic.mainTexture;
        }

        public static void CopyMesh(VertexHelper src, Mesh dst)
        {
            if (src == null || dst == null) return;

            Profiler.BeginSample("(COF)[GraphicDuplicator] CopyMesh");
            dst.Clear(false);
            src.FillMesh(dst);
            dst.RecalculateBounds();
            Profiler.EndSample();
            Logger.Log(nameof(GraphicDuplicator), " >>>> Graphic mesh is copied.");
        }

        public static void CopyMesh(Mesh src, Mesh dst)
        {
            if (src == null || dst == null) return;

            Profiler.BeginSample("(COF)[GraphicDuplicator] CopyMesh");
            src.CopyTo(dst);
            Profiler.EndSample();
            Logger.Log(nameof(GraphicDuplicator), " >>>> Graphic mesh is copied.");
        }
    }
}
