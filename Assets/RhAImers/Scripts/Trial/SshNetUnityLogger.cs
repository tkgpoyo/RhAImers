using System;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

/// <summary>
/// SSH.NET の内部ログを Unity Console に流すための最小ブリッジ。
/// </summary>
public sealed class SshNetUnityLoggerFactory : ILoggerFactory
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        SshNetLoggingConfiguration.InitializeLogging(new SshNetUnityLoggerFactory());
    }

    public ILogger CreateLogger(string categoryName) => new UnityLogger(categoryName);

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }

    private sealed class UnityLogger : ILogger
    {
        private readonly string _category;

        public UnityLogger(string category) => _category = category;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var message = $"[SSH.NET][{_category}] {formatter(state, exception)}";
            if (exception != null) message += $"\n{exception}";

            // 内部ログはスレッドプールから来るが、Debug.Log はスレッドセーフ
            if (logLevel >= LogLevel.Error) UnityEngine.Debug.LogError(message);
            else if (logLevel >= LogLevel.Warning) UnityEngine.Debug.LogWarning(message);
            else UnityEngine.Debug.Log(message);
        }
    }
}
