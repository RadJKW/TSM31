namespace TSM31.Core.Services.Config;

using Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

public class PubSubService
{
    // Handlers are stored per message as a list of weak subscriptions.
    private readonly ConcurrentDictionary<string, List<WeakHandler>> _handlers = [];

    /// <summary>
    /// Messages that were published before any handler was subscribed.
    /// </summary>
    private readonly ConcurrentBag<(string message, object? payload)> _persistentMessages = [];

    [AutoInject] private readonly IServiceProvider _serviceProvider = null!;


    public void Publish(string message, object? payload = null, bool persistent = false)
    {
        if (_handlers.TryGetValue(message, out var weakHandlers))
        {
            foreach (var weakHandler in weakHandlers.ToArray())
            {
                weakHandler.Invoke(payload)?.ContinueWith(HandleException, TaskContinuationOptions.OnlyOnFaulted);
            }
        }
        else if (persistent)
        {
            _persistentMessages.Add((message, payload));
        }
    }

    public Action Subscribe(string message, Func<object?, Task> handler)
    {
        var weakHandler = new WeakHandler(handler);
        var weakHandlers = _handlers.GetOrAdd(message, _ => []);
        weakHandlers.Add(weakHandler);

        // If persistent messages exist for this message, publish them immediately.
        foreach (var (notHandledMessage, payload) in _persistentMessages)
        {
            if (notHandledMessage != message) continue;
            weakHandler.Invoke(payload)?.ContinueWith(HandleException, TaskContinuationOptions.OnlyOnFaulted);
            _persistentMessages.TryTake(out _);
        }

        return () =>
        {
            var removedHandlersCount = weakHandlers.RemoveAll(wh => wh.Matches(handler));
        };
    }

    private void HandleException(Task t)
    {
        _serviceProvider.GetRequiredService<IExceptionHandler>().Handle(t.Exception!);
    }

    private class WeakHandler
    {
        private string _targetInfo;
        private readonly bool _isStatic;
        private readonly MethodInfo _method;
        private readonly WeakReference? _target;

        public WeakHandler(Func<object?, Task> handler)
        {
            _isStatic = handler.Target is null;
            if (_isStatic is false)
            {
                _target = new WeakReference(handler.Target);
            }

            _method = handler.Method;
            _targetInfo = $"{handler.Target?.GetType().FullName ?? "static"}'s {_method.Name}";
        }

        /// <summary>
        /// Invokes the stored handler if it is still alive.
        /// Returns the Task from the handler or null if the target is no longer available.
        /// </summary>
        public Task? Invoke(object? payload)
        {
            if (_isStatic)
            {
                return (Task?)_method.Invoke(null, [payload]);
            }

            if (_target is { IsAlive: true, Target: { } target })
            {
                return (Task?)_method.Invoke(target, [payload]);
            }

            return null;
        }

        /// <summary>
        /// Checks if the given handler matches this weak subscription.
        /// </summary>
        public bool Matches(Func<object?, Task> handler)
        {
            if (_isStatic)
            {
                return handler.Target is null && handler.Method.Equals(_method);
            }

            return handler.Target is not null &&
                   ReferenceEquals(handler.Target, _target?.Target) &&
                   handler.Method.Equals(_method);
        }

        public override string ToString()
        {
            return _targetInfo;
        }
    }
}
