using System;
using UnityEngine;

namespace Coffee.Internal
{
    public class Reservation : MonoBehaviour
    {
        [SerializeField] private Entry[] m_Entries = new Entry[0];

        private void OnEnable()
        {
            for (var i = 0; i < m_Entries.Length; i++)
            {
                var e = m_Entries[i];
                var size = new Vector2Int(e.m_Size, e.m_Size);
                size = RenderTextureRepository.GetPreferSize(size, e.m_Rate);
                var hash = new Hash128((uint)GetInstanceID(), (uint)size.x, (uint)size.y, 0);
                RenderTextureRepository.Get(hash, ref e.rt,
                    x => new RenderTexture(RenderTextureRepository.GetDescriptor(x, false)), size);
            }
        }

        private void OnDisable()
        {
            for (var i = 0; i < m_Entries.Length; i++)
            {
                var e = m_Entries[i];
                RenderTextureRepository.Release(ref e.rt);
            }
        }

        [Serializable]
        public class Entry
        {
            [Range(128, 1024)]
            public int m_Size = 512;

            [Range(0, 8)]
            public int m_Rate;

            [NonSerialized]
            public RenderTexture rt;
        }
    }
}
