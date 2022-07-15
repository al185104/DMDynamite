namespace DMDynamite.Logger
{
    public class LoggerFileProvider : ILoggerProvider
    {
        public string Path { get; set; }
        public LoggerFileProvider(string FullPath)
        {
            Path = FullPath;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new Logger(Path);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
