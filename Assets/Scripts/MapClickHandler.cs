using UnityEngine;
using UnityEngine.EventSystems;

public class MapClickHandler : MonoBehaviour, IPointerClickHandler {

    public void OnPointerClick(PointerEventData pointerEventData) {
        GameHandler.Instance.HandleClickOnBoard();
    }
}