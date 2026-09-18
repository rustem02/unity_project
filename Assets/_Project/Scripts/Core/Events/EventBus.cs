using System;
using System.Collections.Generic;

namespace VRTraining.Core.Events
{
    /// <summary>
    /// Minimal typed publish/subscribe bus. No DI containers — systems subscribe in OnEnable.
    /// </summary>
    public static class EventBus
    {
        public delegate void EventHandler<T>(T payload) where T : struct;

        private static readonly List<Action> ClearActions = new List<Action>();

        public static void Subscribe<T>(EventHandler<T> handler) where T : struct
        {
            EventHub<T>.EnsureRegistered();
            EventHub<T>.Handlers += handler;
        }

        public static void Unsubscribe<T>(EventHandler<T> handler) where T : struct
        {
            EventHub<T>.Handlers -= handler;
        }

        public static void Publish<T>(T payload) where T : struct
        {
            EventHub<T>.Handlers?.Invoke(payload);
        }

        public static void ClearAll()
        {
            for (var i = 0; i < ClearActions.Count; i++)
                ClearActions[i]?.Invoke();
        }

        private static void RegisterClear(Action clear)
        {
            ClearActions.Add(clear);
        }

        private static class EventHub<T> where T : struct
        {
            public static EventHandler<T> Handlers;
            private static bool _registered;

            public static void EnsureRegistered()
            {
                if (_registered)
                    return;
                _registered = true;
                RegisterClear(() => Handlers = null);
            }
        }
    }
}
