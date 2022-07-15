namespace DMDynamite.Logger
{
    public class Logger : ILogger
    {
        public string Path { get; set; }

        public Logger(string FullPath)
        {
            Path = FullPath;
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if(exception == null)
                File.AppendAllText($"{Path}", $"{DateTime.Now} :: LogLevel: {logLevel} | TState : {state} " + Environment.NewLine);
            else
                File.AppendAllText($"{Path}", $"{DateTime.Now} :: LogLevel: {logLevel} | TState : {state} | Exception : {exception} " + Environment.NewLine);
        }
    }
}
