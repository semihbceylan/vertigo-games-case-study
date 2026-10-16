using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Reusable button wrapper. Registers its click listener in code (never via Inspector OnClick)
    /// and resolves its <see cref="Button"/> automatically.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class UIButton : MonoBehaviour
    {
        [SerializeField] protected Button button;

        public event UnityAction Clicked;

        public Button Button => button;

        public bool Interactable
        {
            get => button == null || button.interactable;
            set { if (button != null) button.interactable = value; }
        }

        protected virtual void OnValidate()
        {
            if (button == null) button = GetComponent<Button>();
        }

        protected virtual void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        protected virtual void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick() => Clicked?.Invoke();
    }
}
