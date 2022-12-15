using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Globalization;
using Avalonia.Utilities;
using DynamicData.Kernel;
using DynamicData;
using TQDB_Editor.Common.Controls;

namespace TQDB_Editor.Common.Services
{
    public interface IConsoleLogService : ILogger { }

    public class ConsoleLogService : IConsoleLogService
    {
        private readonly LogLevel _level;

        private readonly RichTextBlock _console;

        private readonly ConfigService _config;

        private readonly ILogger _logger;

        public ConsoleLogService(RichTextBlock console, ConfigService config, ILogger logger)
        {
            if (console is null)
                throw new ArgumentNullException(nameof(console));
            if (config is null)
                throw new ArgumentNullException(nameof(config));
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));

            (_logger, _console, _config) = (logger, console, config);
#if DEBUG
            _level = LogLevel.Trace;
#else
            _level = LogLevel.Information;
#endif
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) =>
            _level <= logLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            switch (logLevel)
            {
                case LogLevel.Trace:
                    _console.PushColor(Colors.Gray);
                    break;
                case LogLevel.Debug:
                    _console.PushColor(Colors.DarkGray);
                    break;
                case LogLevel.Information:
                    _console.PushColor(Colors.White);
                    break;
                case LogLevel.Warning:
                    _console.PushColor(Colors.Yellow);
                    break;
                case LogLevel.Error:
                    _console.PushColor(Colors.Red);
                    break;
                case LogLevel.Critical:
                    _console.PushColor(Colors.DarkRed);
                    break;
                case LogLevel.None:
                    _console.PushColor(Colors.Brown);
                    break;
                default:
                    _console.PushColor(Colors.Brown);
                    break;
            }
            var builder = new StringBuilder();
            builder.Append($"[{logLevel}]: ");
            _console.AddText($"[{logLevel}]: ");

            if (state.ToString() != "[null]")
                builder.Append(ReplaceKnownPaths(formatter(state, exception)));

            if (exception is not null)
            {
                _console.AddText("\n");
                builder.AppendLine();
                builder.Append(ReplaceKnownPaths(exception.ToString()));
            }

            if (exception is not null)
            {
                _console.AppendText(ReplaceKnownPaths(exception.ToString()));
                builder.Append(exception.ToString());
            }

            _logger.Log(logLevel, "{msg}", builder.ToString());
        }

        private string ReplaceKnownPaths(string text)
        {
            var ret = text.Replace(_config.ModDir, $"[i]{_config.ModName}[/i]");
            ret = ret.Replace(_config.WorkingDir, "[i]WorkingDir[/i]");
            return ret;
        }
    }
}
