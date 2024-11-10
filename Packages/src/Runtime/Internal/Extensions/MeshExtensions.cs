using UnityEngine;
using UnityEngine.UI;

namespace Coffee.Internal
{
    internal static class MeshExtensions
    {
        internal static readonly ObjectPool<Mesh> s_MeshPool = new ObjectPool<Mesh>(
            () =>
            {
                var mesh = new Mesh
                {
                    hideFlags = HideFlags.DontSave | HideFlags.NotEditable
                };
                mesh.MarkDynamic();
                return mesh;
            },
            mesh => mesh,
            mesh =>
            {
                if (mesh)
                {
                    mesh.Clear();
                }
            });

        public static Mesh Rent()
        {
            return s_MeshPool.Rent();
        }

        public static void Return(ref Mesh mesh)
        {
            s_MeshPool.Return(ref mesh);
        }

        public static void CopyTo(this Mesh self, Mesh dst)
        {
            if (!self || !dst) return;

            var vector3List = ListPool<Vector3>.Rent();
            var vector4List = ListPool<Vector4>.Rent();
            var color32List = ListPool<Color32>.Rent();
            var intList = ListPool<int>.Rent();

            dst.Clear(false);

            self.GetVertices(vector3List);
            dst.SetVertices(vector3List);

            self.GetTriangles(intList, 0);
            dst.SetTriangles(intList, 0);

            self.GetNormals(vector3List);
            dst.SetNormals(vector3List);

            self.GetTangents(vector4List);
            dst.SetTangents(vector4List);

            self.GetColors(color32List);
            dst.SetColors(color32List);

            self.GetUVs(0, vector4List);
            dst.SetUVs(0, vector4List);

            self.GetUVs(1, vector4List);
            dst.SetUVs(1, vector4List);

            self.GetUVs(2, vector4List);
            dst.SetUVs(2, vector4List);

            self.GetUVs(3, vector4List);
            dst.SetUVs(3, vector4List);

            dst.RecalculateBounds();
            ListPool<Vector3>.Return(ref vector3List);
            ListPool<Vector4>.Return(ref vector4List);
            ListPool<Color32>.Return(ref color32List);
            ListPool<int>.Return(ref intList);
        }

        public static void CopyTo(this Mesh self, VertexHelper dst)
        {
            if (!self || dst == null) return;

            var vertexCount = self.vertexCount;
            var indexCount = self.triangles.Length;
            self.CopyTo(dst, vertexCount, indexCount);
        }

        public static void CopyTo(this Mesh self, VertexHelper dst, int vertexCount, int indexCount)
        {
            if (!self || dst == null) return;

            var positions = ListPool<Vector3>.Rent();
            var normals = ListPool<Vector3>.Rent();
            var uv0 = ListPool<Vector4>.Rent();
            var uv1 = ListPool<Vector4>.Rent();
            var tangents = ListPool<Vector4>.Rent();
            var colors = ListPool<Color32>.Rent();
            var indices = ListPool<int>.Rent();
            self.GetVertices(positions);
            self.GetColors(colors);
            self.GetUVs(0, uv0);
            self.GetUVs(1, uv1);
            self.GetNormals(normals);
            self.GetTangents(tangents);
            self.GetIndices(indices, 0);

            dst.Clear();
            for (var i = 0; i < vertexCount; i++)
            {
                dst.AddVert(positions[i], colors[i], uv0[i], uv1[i], normals[i], tangents[i]);
            }

            for (var i = 0; i < indexCount; i += 3)
            {
                dst.AddTriangle(indices[i], indices[i + 1], indices[i + 2]);
            }

            ListPool<Vector3>.Return(ref positions);
            ListPool<Vector3>.Return(ref normals);
            ListPool<Vector4>.Return(ref uv0);
            ListPool<Vector4>.Return(ref uv1);
            ListPool<Vector4>.Return(ref tangents);
            ListPool<Color32>.Return(ref colors);
            ListPool<int>.Return(ref indices);
        }
    }
}
