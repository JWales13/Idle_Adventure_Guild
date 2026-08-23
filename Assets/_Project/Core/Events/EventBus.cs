using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleGuild.Core.Events
{
    /// <summary>
    /// Typed publish/subscribe used for all cross-system communication.
    ///
    /// Systems live in separate assemblies that reference Core and nothing else, so
    /// they cannot call each other directly even if tempted. This bus is how they
    /// talk: the Guild publishes that a building was upgraded, and whoever cares
    /// reacts, with neither side holding a reference to the other.
    ///
    /// Dispatch is a plain delegate call per event type, with no dictionary lookup
    /// and no boxing, because generic static state gives each event type its own
    /// storage at JIT time.
    ///
    /// One caveat worth knowing: a handler that throws aborts the remaining handlers
    /// for that single publish. The exception is logged rather than swallowed, and it
    /// never propagates back to the publisher. Keep handlers small and defensive.
    /// </summary>
    public static class EventBus
    {
        private static class Channel<TEvent> where TEvent : struct
        {
            public static Action<TEvent> Handlers;
            public static bool IsRegistered;
        }

        private static readonly List<Action> ChannelResets = new List<Action>();

        /// <summary>Start receiving <typeparamref name="TEvent"/>. Pair with <see cref="Unsubscribe{TEvent}"/>.</summary>
        public static void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            if (handler == null)
            {
                return;
            }

            if (!Channel<TEvent>.IsRegistered)
            {
                Channel<TEvent>.IsRegistered = true;
                ChannelResets.Add(static () =>
                {
                    Channel<TEvent>.Handlers = null;
                    Channel<TEvent>.IsRegistered = false;
                });
            }

            Channel<TEvent>.Handlers += handler;
        }

        /// <summary>
        /// Stop receiving <typeparamref name="TEvent"/>. Safe to call for a handler that
        /// was never subscribed. MonoBehaviours must call this in OnDisable, or the bus
        /// will hold destroyed objects alive.
        /// </summary>
        public static void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            if (handler == null)
            {
                return;
            }

            Channel<TEvent>.Handlers -= handler;
        }

        /// <summary>Deliver <paramref name="payload"/> to every current subscriber.</summary>
        public static void Publish<TEvent>(TEvent payload) where TEvent : struct
        {
            Action<TEvent> handlers = Channel<TEvent>.Handlers;
            if (handlers == null)
            {
                return;
            }

            try
            {
                handlers.Invoke(payload);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// Drop every subscription. Runs automatically before a play session so that
        /// Unity's fast enter-play-mode, which does not reload the domain, cannot carry
        /// last session's handlers into this one. Tests should call it between cases.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            foreach (Action reset in ChannelResets)
            {
                reset.Invoke();
            }

            ChannelResets.Clear();
        }
    }
}
