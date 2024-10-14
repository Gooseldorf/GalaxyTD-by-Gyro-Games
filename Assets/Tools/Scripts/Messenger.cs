using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardTD.Utilities
{
    public enum MessengerMode
    {
        DONT_REQUIRE_LISTENER,
        REQUIRE_LISTENER,
    }

    internal static class MessengerInternal
    {
        public const MessengerMode DEFAULT_MODE = MessengerMode.REQUIRE_LISTENER;
    
        public static readonly Dictionary<string, Delegate> EventTable = new();

        public static void AddListener(string eventName, Delegate callback)
        {
            OnListenerAdding(eventName, callback);
            EventTable[eventName] = Delegate.Combine(EventTable[eventName], callback);
        }

        public static void RemoveListener(string eventName, Delegate handler)
        {
            if (OnListenerRemoving(eventName, handler) == false) 
                return;
        
            EventTable[eventName] = Delegate.Remove(EventTable[eventName], handler);
            OnListenerRemoved(eventName);
        }

        public static T[] GetInvocationList<T>(string eventName)
        {
            if (!EventTable.TryGetValue(eventName, out Delegate del)) 
                return default;
        
            if (del != null)
            {
                Delegate[] invocationList = del.GetInvocationList();
                IEnumerable<T> enumerable = Cast(invocationList);
                T[] array = new T[invocationList.Length];
                using IEnumerator<T> en = enumerable.GetEnumerator();

                int index = 0;
                while (en.MoveNext())
                {
                    T current = en.Current;
                    array[index] = current;
                    index++;
                }

                return array;
                
                // return del
                //     .GetInvocationList()
                //     .Cast<T>()
                //     .ToArray();
            }

            IEnumerable<T> Cast(IEnumerable source)
            {
                foreach (T result in source)
                    yield return result;
            }
            
            throw CreateBroadcastSignatureException(eventName);
        }
        
        public static void Clear() => EventTable.Clear();

        private static void OnListenerAdding(string eventName, Delegate listenerBeingAdded)
        {
            if (!EventTable.ContainsKey(eventName))
                EventTable.Add(eventName, null);

            Delegate d = EventTable[eventName];
        
            if (d != null && d.GetType() != listenerBeingAdded.GetType())
                throw new ListenerException($"Attempting to add listener with inconsistent signature for event type {eventName}. Current listeners have type {d.GetType().Name} and listener being added has type {listenerBeingAdded.GetType().Name}");
        }

        private static bool OnListenerRemoving(string eventName, Delegate listenerBeingRemoved)
        {
            if (!EventTable.ContainsKey(eventName))
                return false;

            Delegate d = EventTable[eventName];

            if (d == null)
                return false;

            if (d.GetType() != listenerBeingRemoved.GetType())
                throw new ListenerException($"Attempting to remove listener with inconsistent signature for event type {eventName}. Current listeners have type {d.GetType().Name} and listener being removed has type {listenerBeingRemoved.GetType().Name}");
        
            return true;
        }

        private static void OnListenerRemoved(string eventName)
        {
            if (EventTable[eventName] == null)
                EventTable.Remove(eventName);
        }

        public static void OnBroadcasting(string eventName, MessengerMode mode)
        {
            if (mode == MessengerMode.REQUIRE_LISTENER && !EventTable.ContainsKey(eventName))
                throw new BroadcastException($"Broadcasting message {eventName} but no listener found.");
        }

        private static BroadcastException CreateBroadcastSignatureException(string eventName)
        {
            return new BroadcastException($"Broadcasting message {eventName} but listeners have a different signature than the broadcaster.");
        }

        public sealed class BroadcastException : Exception
        {
            public BroadcastException(string msg) : base(msg) {}
        }

        private sealed class ListenerException : Exception
        {
            public ListenerException(string msg) : base(msg) {}
        }
    }

// No parameters
    public static class Messenger
    {
        public static void AddListener(string eventName, Action handler)
        {
            MessengerInternal.AddListener(eventName, handler);
        }

        public static void AddListener<T>(string eventName, Action<T> handler)
        {
            MessengerInternal.AddListener(eventName, handler);
        }

        public static void RemoveListener(string eventName, Action handler)
        {
            MessengerInternal.RemoveListener(eventName, handler);
        }

        public static void RemoveListener<T>(string eventName, Action<T> handler)
        {
            MessengerInternal.RemoveListener(eventName, handler);
        }

        public static void Broadcast(string eventName)
        {
            Broadcast(eventName, MessengerInternal.DEFAULT_MODE);
        }

        public static void Broadcast<TReturn>(string eventName, Action<TReturn> returnCall)
        {
            Broadcast(eventName, returnCall, MessengerInternal.DEFAULT_MODE);
        }

        public static void Broadcast(string eventName, MessengerMode mode)
        {
            MessengerInternal.OnBroadcasting(eventName, mode);
            //var invocationList = MessengerInternal.GetInvocationList<Action>(eventName);

            Action[] invocationList = MessengerInternal.GetInvocationList<Action>(eventName);

            if (invocationList == null)
                return;

            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    invocationList[i].Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError("Error In Broadcast of: " + eventName + " event. " + e.ToString());
                    Debug.LogException(e);
                    //TODO: Remove Listener from this broadcast 
                }
            }

            //var invocationList = MessengerInternal.GetInvocationListMike(eventName);
            //if (invocationList == null) return;
            //foreach (var callback in invocationList)
            //    callback.Invoke();
        }

        public static void Broadcast<TReturn>(string eventName, Action<TReturn> returnCall, MessengerMode mode)
        {
            MessengerInternal.OnBroadcasting(eventName, mode);

            var invocationList = MessengerInternal.GetInvocationList<Func<TReturn>>(eventName);

            if (invocationList == null)
                return;

            List<TReturn> returns = new();
            foreach (Func<TReturn> func in invocationList)
                returns.Add(func.Invoke());
            
            foreach (TReturn result in returns)
            {
                try
                {
                    returnCall.Invoke(result);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error In Broadcast of: " + eventName + " event. " + e.ToString());
                    Debug.LogException(e);
                    //TODO: Remove Listener from this broadcast 
                }
            }
        }
        
        public static void Clear() => MessengerInternal.Clear();
    }

// One parameter
    public static class Messenger<T>
    {
        public static void AddListener(string eventName, Action<T> handler)
        {
            MessengerInternal.AddListener(eventName, handler);
        }

        public static void AddListener<TReturn>(string eventName, Func<T, TReturn> handler)
        {
            MessengerInternal.AddListener(eventName, handler);
        }

        public static void RemoveListener(string eventName, Action<T> handler)
        {
            MessengerInternal.RemoveListener(eventName, handler);
        }

        public static void RemoveListener<TReturn>(string eventName, Func<T, TReturn> handler)
        {
            MessengerInternal.RemoveListener(eventName, handler);
        }

        public static void Broadcast(string eventName, T arg1)
        {
            Broadcast(eventName, arg1, MessengerInternal.DEFAULT_MODE);
        }

        public static void Broadcast<TReturn>(string eventName, T arg1, Action<TReturn> returnCall)
        {
            Broadcast(eventName, arg1, returnCall, MessengerInternal.DEFAULT_MODE);
        }

        public static void Broadcast(string eventName, T arg1, MessengerMode mode)
        {
            MessengerInternal.OnBroadcasting(eventName, mode);

            Action<T>[] invocationList = MessengerInternal.GetInvocationList<Action<T>>(eventName);

            if (invocationList == null)
                return;

            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    invocationList[i].Invoke(arg1);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error In Broadcast of: " + eventName + " event. " + e.ToString());
                    Debug.LogException(e);
                    //TODO: Remove Listener from this broadcast 
                }
            }

            //var invocationList = MessengerInternal.GetInvocationList<Action<T>>(eventName);

            //if (invocationList != null)
            //{
            //    foreach (var callback in invocationList)
            //        callback.Invoke(arg1);
            //}
        }

        public static void Broadcast<TReturn>(string eventName, T arg1, Action<TReturn> returnCall, MessengerMode mode)
        {
            MessengerInternal.OnBroadcasting(eventName, mode);
            var invocationList = MessengerInternal.GetInvocationList<Func<T, TReturn>>(eventName);
            if (invocationList == null)
                return;
            
            List<TReturn> returns = new();
            foreach (Func<T, TReturn> func in invocationList)
                returns.Add(func.Invoke(arg1));
            
            foreach (TReturn result in returns)
            {
                try
                {
                    returnCall.Invoke(result);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error In Broadcast of: " + eventName + " event. " + e.ToString());
                    Debug.LogException(e);
                    //TODO: Remove Listener from this broadcast 
                }
            }
        }
        
        public static void Clear() => MessengerInternal.Clear();
    }


// Two parameters
    public static class Messenger<T, U>
    {
        public static void AddListener(string eventName, Action<T, U> handler)
        {
            MessengerInternal.AddListener(eventName, handler);
        }

        public static void AddListener<TReturn>(string eventName, Func<T, U, TReturn> handler)
        {
            MessengerInternal.AddListener(eventName, handler);
        }

        public static void RemoveListener(string eventName, Action<T, U> handler)
        {
            MessengerInternal.RemoveListener(eventName, handler);
        }

        public static void RemoveListener<TReturn>(string eventName, Func<T, U, TReturn> handler)
        {
            MessengerInternal.RemoveListener(eventName, handler);
        }

        public static void Broadcast(string eventName, T arg1, U arg2)
        {
            Broadcast(eventName, arg1, arg2, MessengerInternal.DEFAULT_MODE);
        }

        public static void Broadcast<TReturn>(string eventName, T arg1, U arg2, Action<TReturn> returnCall)
        {
            Broadcast(eventName, arg1, arg2, returnCall, MessengerInternal.DEFAULT_MODE);
        }

        public static void Broadcast(string eventName, T arg1, U arg2, MessengerMode mode)
        {
            MessengerInternal.OnBroadcasting(eventName, mode);

            //MessengerInternal.eventTable.TryGetValue(eventName, out del);
            Action<T, U>[] invocationList = MessengerInternal.GetInvocationList<Action<T, U>>(eventName);
            if (invocationList == null)
                return;
            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    invocationList[i].Invoke(arg1, arg2);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Error In Broadcast of: " + eventName + " event. " + e.ToString());
                    Debug.LogException(e);
                    //TODO: Remove Listener from this broadcast 
                }
            }

            //var invocationList = MessengerInternal.GetInvocationList<Action<T, U>>(eventName);
            //if (invocationList != null)
            //{
            //    foreach (var callback in invocationList)
            //        callback?.Invoke(arg1, arg2);
            //}
        }

        public static void Broadcast<TReturn>(string eventName, T arg1, U arg2, Action<TReturn> returnCall, MessengerMode mode)
        {
            MessengerInternal.OnBroadcasting(eventName, mode);
            var invocationList = MessengerInternal.GetInvocationList<Func<T, U, TReturn>>(eventName);
            if (invocationList == null)
                return;
            
            List<TReturn> returns = new();
            foreach (Func<T, U, TReturn> func in invocationList)
                returns.Add(func.Invoke(arg1, arg2));
            
            foreach (TReturn result in returns)
            {
                try
                {
                    returnCall.Invoke(result);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error In Broadcast of: " + eventName + " event. " + e.ToString());
                    Debug.LogException(e);
                    //TODO: Remove Listener from this broadcast 
                }
            }
        }
        
        public static void Clear() => MessengerInternal.Clear();
    }


// Three parameters
    public static class Messenger<T, U, V>
    {
        public static void AddListener(string eventName, Action<T, U, V> handler)
        {
            MessengerInternal.AddListener(eventName, handler);
        }

        public static void AddListener<TReturn>(string eventName, Func<T, U, V, TReturn> handler)
        {
            MessengerInternal.AddListener(eventName, handler);
        }

        public static void RemoveListener(string eventName, Action<T, U, V> handler)
        {
            MessengerInternal.RemoveListener(eventName, handler);
        }

        public static void RemoveListener<TReturn>(string eventName, Func<T, U, V, TReturn> handler)
        {
            MessengerInternal.RemoveListener(eventName, handler);
        }

        public static void Broadcast(string eventName, T arg1, U arg2, V arg3)
        {
            Broadcast(eventName, arg1, arg2, arg3, MessengerInternal.DEFAULT_MODE);
        }

        public static void Broadcast<TReturn>(string eventName, T arg1, U arg2, V arg3, Action<TReturn> returnCall)
        {
            Broadcast(eventName, arg1, arg2, arg3, returnCall, MessengerInternal.DEFAULT_MODE);
        }

        public static void Broadcast(string eventName, T arg1, U arg2, V arg3, MessengerMode mode)
        {
            MessengerInternal.OnBroadcasting(eventName, mode);
            if (mode == MessengerMode.DONT_REQUIRE_LISTENER && !MessengerInternal.EventTable.ContainsKey(eventName)) return;
            Delegate del;
            MessengerInternal.EventTable.TryGetValue(eventName, out del);
            if (del != null) ((Action<T, U, V>)del).Invoke(arg1, arg2, arg3);



            //var invocationList = MessengerInternal.GetInvocationList<Action<T, U, V>>(eventName);
            //foreach (var callback in invocationList)
            //    callback.Invoke(arg1, arg2, arg3);
        }

        public static void Broadcast<TReturn>(string eventName, T arg1, U arg2, V arg3, Action<TReturn> returnCall, MessengerMode mode)
        {
            MessengerInternal.OnBroadcasting(eventName, mode);
            var invocationList = MessengerInternal.GetInvocationList<Func<T, U, V, TReturn>>(eventName);
            if (invocationList == null)
                return;
            
            List<TReturn> returns = new();
            foreach (Func<T, U, V, TReturn> func in invocationList)
                returns.Add(func.Invoke(arg1, arg2, arg3));
            
            foreach (TReturn result in returns)
            {
                returnCall.Invoke(result);
            }
        }
        
        public static void Clear() => MessengerInternal.Clear();
    }
}