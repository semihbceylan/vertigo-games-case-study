using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Reusable button wrapper. Registers its click listener in code (never via Inspector OnClick)
    /// and uses an editor-wired <see cref="Button"/> reference.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class UIButton : MonoBehaviour
    {
        [SerializeField] protected Button button;

        public event UnityAction Clicked;

        public Button Button { get { return button; } }

        public bool Interactable
        {
            get { return button && button.interactable; }
            set { if (button) button.interactable = value; }
        }

        protected virtual void OnValidate()
        {
            if (button == null) button = GetComponent<Button>();
        }

        protected virtual void OnEnable()
        {
            if (button) button.onClick.AddListener(HandleClick);
        }

        protected virtual void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            Clicked?.Invoke();
        }
    }
}
