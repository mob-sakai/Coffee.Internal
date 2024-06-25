using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Coffee.Development
{
    [Serializable]
    public class AllocExecutionEvent : UnityEvent<MonoBehaviour>
    {
    }

    public class AllocTest : MonoBehaviour
    {
        [SerializeField]
        protected List<MonoBehaviour> m_Targets;

        [Header("Move")]
        [SerializeField]
        protected bool m_Translate;

        [SerializeField]
        protected bool m_Rotate;

        [SerializeField]
        protected bool m_Scale;

        [Header("Activation")]
        [SerializeField]
        protected bool m_SwitchActivation;

        [SerializeField]
        protected bool m_SwitchEnabled;

        [Header("Execution")]
        [SerializeField]
        protected bool m_Execute;

        [SerializeField]
        protected AllocExecutionEvent m_OnExecute = new AllocExecutionEvent();

        [Header("Clone")]
        [SerializeField]
        protected GameObject m_ClonePrefab;

        [SerializeField]
        protected Transform m_CloneInto;

        public bool switchActivation
        {
            get => m_SwitchActivation;
            set => m_SwitchActivation = value;
        }

        public bool switchEnabled
        {
            get => m_SwitchEnabled;
            set => m_SwitchEnabled = value;
        }

        public bool execute
        {
            get => m_Execute;
            set => m_Execute = value;
        }

        public bool translate
        {
            get => m_Translate;
            set => m_Translate = value;
        }

        public bool rotate
        {
            get => m_Rotate;
            set => m_Rotate = value;
        }

        public bool scale
        {
            get => m_Scale;
            set => m_Scale = value;
        }

        private void Update()
        {
            foreach (var target in m_Targets)
            {
                Update(target);
            }
        }

        protected virtual void Update(MonoBehaviour target)
        {
            if (!target) return;
            var go = target.gameObject;
            if (m_SwitchActivation)
            {
                go.SetActive(!go.activeSelf);
            }
            else if (!go.activeSelf)
            {
                go.SetActive(true);
            }

            if (m_SwitchEnabled)
            {
                target.enabled = !target.enabled;
            }
            else if (!target.enabled)
            {
                target.enabled = true;
            }

            if (m_Execute)
            {
                OnExecute(target);
            }

            var v = (Mathf.PingPong(Time.timeSinceLevelLoad, 4) - 2) / 2 * Time.deltaTime;
            if (m_Translate)
            {
                target.transform.Translate(v * 100f * Vector3.one);
            }

            if (m_Rotate)
            {
                target.transform.Rotate(v * 100f * Vector3.one);
            }

            if (m_Scale)
            {
                target.transform.localScale = v * 1f * Vector3.one + Vector3.one;
            }
        }

        protected virtual void OnExecute(MonoBehaviour target)
        {
            m_OnExecute.Invoke(target);
        }

        public void CloneAndDestroy()
        {
            var clone = Instantiate(m_ClonePrefab, m_CloneInto ? m_CloneInto : transform);
            clone.SetActive(true);

            // Destroy next frame
            Destroy(clone, 0.001f);
            Debug.Break();
        }
    }
}
