using System;
using System.Collections.Generic;

namespace Vertigo.WheelOfFortune.Core
{
    public interface IEventBus
    {
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct;
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct;
        void Publish<TEvent>(TEvent message) where TEvent : struct;
    }

    /// <summary>
    /// Instance-scoped synchronous notifications. Publication uses the current immutable
    /// multicast delegate, so subscription changes apply to the next publication.
    /// Handlers execute in registration order; exceptions propagate to the publisher.
    /// Intended for the Unity main thread, not concurrent access.
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        #region Channels

        private interface IChannel { }

        private sealed class Channel<TEvent> : IChannel where TEvent : struct
        {
            private readonly List<Action<TEvent>> _handlers = new List<Action<TEvent>>();
            private Action<TEvent> _dispatch;

            public void Subscribe(Action<TEvent> handler)
            {
                if (_handlers.Contains(handler))
                {
                    return;
                }
                _handlers.Add(handler);
                _dispatch += handler;
            }

            public void Unsubscribe(Action<TEvent> handler)
            {
                if (_handlers.Remove(handler))
                {
                    _dispatch -= handler;
                }
            }

            public void Publish(TEvent message)
            {
                _dispatch?.Invoke(message);
            }
        }

        private readonly Dictionary<Type, IChannel> _channels = new Dictionary<Type, IChannel>();

        #endregion

        #region Public API

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            Type type = typeof(TEvent);
            if (!_channels.TryGetValue(type, out IChannel channel))
            {
                channel = new Channel<TEvent>();
                _channels.Add(type, channel);
            }
            ((Channel<TEvent>)channel).Subscribe(handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            if (_channels.TryGetValue(typeof(TEvent), out IChannel channel))
            {
                ((Channel<TEvent>)channel).Unsubscribe(handler);
            }
        }

        public void Publish<TEvent>(TEvent message) where TEvent : struct
        {
            if (_channels.TryGetValue(typeof(TEvent), out IChannel channel))
            {
                ((Channel<TEvent>)channel).Publish(message);
            }
        }

        #endregion
    }
}
