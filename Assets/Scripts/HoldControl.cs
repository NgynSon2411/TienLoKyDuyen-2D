using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TienLo
{
    public sealed class HoldControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private readonly HashSet<int> pointers = new HashSet<int>();
        public bool Held => pointers.Count > 0;

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
                pointers.Add(data.pointerId);
        }

        public void OnPointerUp(PointerEventData data) => pointers.Remove(data.pointerId);

        public void Clear() => pointers.Clear();

        private void OnDisable() => Clear();
    }
}
