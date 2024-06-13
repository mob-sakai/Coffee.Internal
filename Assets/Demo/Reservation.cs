﻿using System;
using UnityEngine;
using UnityEngine.Serialization;


namespace Coffee.Internal
{
    public class Reservation : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            [Range(128, 1024)]
            public int m_Size = 512;

            [Range(0, 8)]
            public int m_Rate = 0;

            [NonSerialized]
            public RenderTexture rt;
        }

        [SerializeField] private Entry[] m_Entries = new Entry[0];

        private void OnEnable()
        {
            for (var i = 0; i < m_Entries.Length; i++)
            {
                var e = m_Entries[i];
                RenderTextureRepository.Get(i, Vector2.one * e.m_Size, e.m_Rate, ref e.rt, false);
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
    }
}
