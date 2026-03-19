namespace Mezon_sdk.Managers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class EventManager
    {
        // Define handler types (we use a wrapper to determine if it's default and/or async)
        public class EventHandler
        {
            public Delegate Action { get; set; }
            public bool IsDefaultHandler { get; set; }

            public EventHandler(Delegate action, bool isDefaultHandler = false)
            {
                Action = action ?? throw new ArgumentNullException(nameof(action));
                IsDefaultHandler = isDefaultHandler;
            }
        }

        private readonly ConcurrentDictionary<string, List<EventHandler>> _eventHandlers = 
            new ConcurrentDictionary<string, List<EventHandler>>();

        public void On(string eventName, Delegate handler, bool isDefault = false)
        {
            _eventHandlers.AddOrUpdate(
                eventName,
                _ => new List<EventHandler> { new EventHandler(handler, isDefault) },
                (_, list) =>
                {
                    lock (list)
                    {
                        list.Add(new EventHandler(handler, isDefault));
                    }
                    return list;
                });
        }

        public void Off(string eventName, Delegate? handler = null)
        {
            if (!_eventHandlers.TryGetValue(eventName, out var list))
            {
                return;
            }

            if (handler == null)
            {
                _eventHandlers.TryRemove(eventName, out _);
            }
            else
            {
                lock (list)
                {
                    var toRemove = list.FirstOrDefault(h => h.Action == handler);
                    if (toRemove != null)
                    {
                        list.Remove(toRemove);
                    }

                    if (list.Count == 0)
                    {
                        _eventHandlers.TryRemove(eventName, out _);
                    }
                }
            }
        }

        public async Task EmitAsync(string eventName, params object[] args)
        {
            if (!_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                return;
            }

            List<EventHandler> handlersSnapshot;
            lock (handlers)
            {
                handlersSnapshot = handlers.ToList();
            }

            if (handlersSnapshot.Count == 0)
            {
                return;
            }

            var defaultHandlers = handlersSnapshot.Where(h => h.IsDefaultHandler).ToList();
            var userHandlers = handlersSnapshot.Where(h => !h.IsDefaultHandler).ToList();

            if (defaultHandlers.Count > 0)
            {
                var asyncDefaultTasks = new List<Task>();

                foreach (var handler in defaultHandlers)
                {
                    try
                    {
                        if (handler.Action is Func<Task> asyncFunc)
                        {
                            asyncDefaultTasks.Add(asyncFunc());
                        }
                        else if (handler.Action.Method.ReturnType == typeof(Task))
                        {
                            asyncDefaultTasks.Add((Task)handler.Action.DynamicInvoke(args)!);
                        }
                        else
                        {
                            handler.Action.DynamicInvoke(args);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in sync/invoke default handler for '{eventName}': {ex}");
                    }
                }

                if (asyncDefaultTasks.Count > 0)
                {
                    try
                    {
                        await Task.WhenAll(asyncDefaultTasks);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in async default handler for '{eventName}': {ex}");
                    }
                }
            }

            // User handlers fire and forget
            foreach (var handler in userHandlers)
            {
                try
                {
                    if (handler.Action is Func<Task> asyncFunc)
                    {
                        _ = asyncFunc().ContinueWith(t => HandleTaskException(t, eventName), TaskContinuationOptions.OnlyOnFaulted);
                    }
                    else if (handler.Action.Method.ReturnType == typeof(Task))
                    {
                        var task = (Task)handler.Action.DynamicInvoke(args)!;
                        _ = task.ContinueWith(t => HandleTaskException(t, eventName), TaskContinuationOptions.OnlyOnFaulted);
                    }
                    else
                    {
                        // synchronous execution
                        _ = Task.Run(() => handler.Action.DynamicInvoke(args));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scheduling user handler for '{eventName}': {ex}");
                }
            }
        }

        private void HandleTaskException(Task task, string eventName)
        {
            if (task.Exception != null)
            {
                Console.WriteLine($"Error in async event handler for '{eventName}': {task.Exception}");
            }
        }

        public bool HasListeners(string eventName)
        {
            return _eventHandlers.TryGetValue(eventName, out var list) && list.Count > 0;
        }
    }
}
