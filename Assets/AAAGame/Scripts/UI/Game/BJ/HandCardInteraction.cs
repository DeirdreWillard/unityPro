using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HandCardInteraction : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
    private LocalOp_Bj m_OpManager;
    private int m_CardIndex = -1;

    public void Initialize(LocalOp_Bj opManager, int cardIndex)
    {
        m_OpManager = opManager;
        m_CardIndex = cardIndex;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (m_OpManager == null || m_CardIndex == -1) return;
        m_OpManager.HandleCardPointerDown(m_CardIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_OpManager == null || m_CardIndex == -1) return;
        m_OpManager.HandleCardPointerEnter(m_CardIndex);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (m_OpManager == null || m_CardIndex == -1) return;
        m_OpManager.HandleCardPointerUp(m_CardIndex); 
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (m_OpManager == null || m_CardIndex == -1) return;
        m_OpManager.HandleCardPointerExit(m_CardIndex);
    }
}