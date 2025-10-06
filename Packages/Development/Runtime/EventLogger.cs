using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public class EventLogger : UIBehaviour
{
    [Header("Behaviour")]
    [SerializeField] private bool m_OnAwake = true;
    [SerializeField] private bool m_OnStart = true;
    [SerializeField] private bool m_OnEnable = true;
    [SerializeField] private bool m_OnDisable = true;
    [SerializeField] private bool m_OnDestroy = true;

    [Header("UI Behaviour")]
    [SerializeField] private bool m_OnRectTransformDimensionsChange = true;
    [SerializeField] private bool m_OnDidApplyAnimationProperties = true;
    [SerializeField] private bool m_OnBeforeTransformParentChanged = true;
    [SerializeField] private bool m_OnTransformParentChanged = true;
    [SerializeField] private bool m_OnCanvasGroupChanged = true;
    [SerializeField] private bool m_OnCanvasHierarchyChanged = true;

    [Header("Graphic")]
    [SerializeField] private bool m_OnDirtyLayout = true;
    [SerializeField] private bool m_OnDirtyMaterial = true;
    [SerializeField] private bool m_OnDirtyVertices = true;

    [Header("Pause")]
    [SerializeField] private bool m_PauseOnLog = false;

    protected override void Awake()
    {
        Log(m_OnAwake, "OnAwake");

        var graphic = GetComponent<Graphic>();
        if (graphic)
        {
            graphic.RegisterDirtyLayoutCallback(OnDirtyLayout);
            graphic.RegisterDirtyMaterialCallback(OnDirtyMaterial);
            graphic.RegisterDirtyVerticesCallback(OnDirtyVertices);
        }
    }

    protected override void Start()
    {
        Log(m_OnStart, "OnStart");
    }

    protected override void OnEnable()
    {
        Log(m_OnEnable, "OnEnable");
    }

    protected override void OnDisable()
    {
        Log(m_OnDisable, "OnDisable");
    }

    protected override void OnDestroy()
    {
        Log(m_OnDestroy, "OnDestroy");

        var graphic = GetComponent<Graphic>();
        if (graphic)
        {
            graphic.UnregisterDirtyLayoutCallback(OnDirtyLayout);
            graphic.UnregisterDirtyMaterialCallback(OnDirtyMaterial);
            graphic.UnregisterDirtyVerticesCallback(OnDirtyVertices);
        }
    }

    private void OnDirtyLayout()
    {
        Log(m_OnDirtyLayout, "OnDirtyLayout");
    }

    private void OnDirtyMaterial()
    {
        Log(m_OnDirtyMaterial, "OnDirtyMaterial");
    }

    private void OnDirtyVertices()
    {
        Log(m_OnDirtyVertices, "OnDirtyVertices");
    }


    protected override void OnRectTransformDimensionsChange()
    {
        Log(m_OnRectTransformDimensionsChange, "OnRectTransformDimensionsChange");
    }

    protected override void OnDidApplyAnimationProperties()
    {
        Log(m_OnDidApplyAnimationProperties, "OnDidApplyAnimationProperties");
    }

    protected override void OnBeforeTransformParentChanged()
    {
        Log(m_OnBeforeTransformParentChanged, "OnBeforeTransformParentChanged");
    }

    protected override void OnTransformParentChanged()
    {
        Log(m_OnTransformParentChanged, "OnTransformParentChanged");
    }

    protected override void OnCanvasGroupChanged()
    {
        Log(m_OnCanvasGroupChanged, "OnCanvasGroupChanged");
    }

    protected override void OnCanvasHierarchyChanged()
    {
        Log(m_OnCanvasHierarchyChanged, "OnCanvasHierarchyChanged");
    }

    private void Log(bool logEnabled, string eventName)
    {
        if (!logEnabled) return;

        Debug.Log($"[EventDebugger: {Time.frameCount}] <color=orange>{name}</color> {eventName}", this);

        if (m_PauseOnLog)
            Debug.Break();
    }
}
