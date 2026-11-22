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
    public class UIButton : MonoBehaviour, IClickSource, IButtonInteractability
    {
        #region Serialized References

        [SerializeField] private Button _button;

        #endregion

        #region Public API

        public event UnityAction Clicked;

        public bool IsInteractable
        {
            get
            {
                return _button && _button.interactable;
            }
            set
            {
                if (_button)
                {
                    _button.interactable = value;
                }
            }
        }

        #endregion

        #region Lifecycle

        protected virtual void OnEnable()
        {
            if (_button)
            {
                _button.onClick.AddListener(HandleClick);
            }
        }

        protected virtual void OnDisable()
        {
            if (_button)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            if (Clicked != null)
            {
                Clicked.Invoke();
            }
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        protected virtual void OnValidate()
        {
            if (!_button)
            {
                _button = GetComponent<Button>();
            }
        }

        #endregion
#endif
    }
}
