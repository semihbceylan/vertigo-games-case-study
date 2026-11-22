using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Game
{
    /// <summary>
    /// Editor and development-build helper that jumps the live run to another zone. The session's
    /// ready state is the only one that accepts the jump, so it can never interrupt a spin.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DevZoneTeleport : MonoBehaviour
    {
        #region Configuration

        private const int NO_START_ZONE = 0;

        [Serializable]
        private struct Shortcut
        {
            public Key Key;
            [Tooltip("Relative shortcuts add Zone to the current zone; absolute ones jump to it.")]
            public bool IsRelative;
            public int Zone;
        }

        [Tooltip("Start the run at this zone. Zero keeps the configured starting zone.")]
        [SerializeField, Min(0)] private int _startZone;
        [SerializeField] private Shortcut[] _shortcuts = new Shortcut[0];

        private GameSession _session;

        #endregion

        #region Teleport

        public void Bind(GameSession session)
        {
            _session = session;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_startZone != NO_START_ZONE)
            {
                _session.JumpToZone(_startZone);
            }
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (_session == null || keyboard == null)
            {
                return;
            }
            foreach (Shortcut shortcut in _shortcuts)
            {
                if (keyboard[shortcut.Key].wasPressedThisFrame)
                {
                    _session.JumpToZone(shortcut.IsRelative ? _session.Zone + shortcut.Zone : shortcut.Zone);
                    return;
                }
            }
        }
#endif

        #endregion
    }
}
