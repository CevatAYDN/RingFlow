using Nexus.Core;
using UnityEngine;

namespace RingFlow.Gameplay
{
    [Mediator(typeof(RingMediator))]
    public class RingView : View
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        
        public RingColor Color { get; private set; }

        public void SetColor(RingColor color, Color unityColor)
        {
            Color = color;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = unityColor;
            }
        }
    }
}
