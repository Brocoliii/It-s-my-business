using UnityEngine;
public interface IDraggable
{
    void OnBeginDrag();
    void OnDrag(Vector2 mousePos);
    void OnEndDrag();
}