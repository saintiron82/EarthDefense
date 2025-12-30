using System;
using UnityEngine.UI;

namespace SG.UI
{
    public sealed class ObservableSelectable : Selectable // DESC :: Selectable을 R3.Triggers.ObservableSelectable로 확장을 위해 필요한 클래스
    {
        public event Action<bool> OnInteractableChanged;
        private bool _lastInteractable;

        protected override void OnEnable()
        {
            base.OnEnable();
            _lastInteractable = interactable;
        }

        void Update()
        {
            if (_lastInteractable != interactable)
            {
                _lastInteractable = interactable;
                OnInteractableChanged?.Invoke(interactable);
            }
        }
    }
}