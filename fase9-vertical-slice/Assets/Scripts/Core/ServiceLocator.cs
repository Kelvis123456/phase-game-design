using System;
using System.Collections.Generic;

public static class Services
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void Register<T>(T service)
    {
        _services[typeof(T)] = service;
    }

    public static T Get<T>()
    {
        if (_services.TryGetValue(typeof(T), out var s))
            return (T)s;
        throw new Exception($"[Services] '{typeof(T).Name}' no registrado. ¿Olvidaste llamar Register<T>() en Awake?");
    }

    public static bool TryGet<T>(out T service)
    {
        if (_services.TryGetValue(typeof(T), out var s))
        {
            service = (T)s;
            return true;
        }
        service = default;
        return false;
    }

    public static void Clear() => _services.Clear();
}
