using Nexus.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RingFlow.Gameplay
{
    [Mediator(typeof(PoleMediator))]
    public class PoleView : View, IPointerClickHandler
    {
        [SerializeField] private int _poleId;
        public int PoleId { get => _poleId; set => _poleId = value; }

        public System.Action OnClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke();
        }
    }
}
