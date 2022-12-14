using Godot;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace TQDBEditor.Common
{
    public partial class ConsoleLogHandler : Node
    {
        private ConsoleLogger logger;
        private Config config;

        public Config Config
        {
            set
            {
                config = value;
                TrulyReady();
            }
        }

        private void TrulyReady()
        {
            var console = GetNode<RichTextLabel>("/root/MainWindow/MainView/FilesConsole/ConsoleContainer/Console");
            if (console is null)
                return;

            if (!OS.HasFeature("editor"))
                logger = new ConsoleLogger(LogLevel.Trace, console, config);
            else
                logger = new ConsoleLogger(LogLevel.Information, console, config);
        }

        public ILogger Logger => logger;


        private class ConsoleLogger : ILogger
        {
            private readonly LogLevel _level;

            private readonly RichTextLabel _console;

            private readonly Config _config;

            public ConsoleLogger(LogLevel level, RichTextLabel console, Config config) =>
                (_level, _console, _config) = (level, console, config);

            public IDisposable BeginScope<TState>(TState state) => default!;

            public bool IsEnabled(LogLevel logLevel) =>
                _level <= logLevel;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
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
                {
                    _console.AppendText(ReplaceKnownPaths(formatter(state, exception)));
                    builder.Append(formatter(state, exception));
                    if (exception is not null)
                    {
                        _console.AddText("\n");
                        builder.AppendLine();
                    }
                }

                if (exception is not null)
                {
                    _console.AppendText(ReplaceKnownPaths(exception.ToString()));
                    builder.Append(exception.ToString());
                }

                _console.Pop();
                _console.AddText("\n");

                if (logLevel >= LogLevel.Error)
                    GD.PrintErr(builder);
                else
                    GD.Print(builder);
            }

            private string ReplaceKnownPaths(string text)
            {
                var ret = text.Replace(_config.ModDir, $"[i]{_config.ModName}[/i]");
                ret = ret.Replace(_config.WorkingDir, "[i]WorkingDir[/i]");
                return ret;
            }
        }
    }
}