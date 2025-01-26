using UnityEngine;
using UnityEngine.Events;

namespace Coffee.Development
{
    public class PlayModeTrigger : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent m_OnEnable = new UnityEvent();

        public void OnEnable()
        {
            m_OnEnable.Invoke();
        }
    }
}
