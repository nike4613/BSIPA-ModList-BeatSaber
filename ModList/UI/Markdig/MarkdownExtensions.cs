using Markdig;
using System.IO;
using System.Text;
using IPALogger = IPA.Logging.Logger;

namespace IPA.ModList.BeatSaber.UI.Markdig
{
    internal static class MarkdownExtensions
    {
        public static MarkdownPipelineBuilder WithDebugWriter(this MarkdownPipelineBuilder pipeline, TextWriter writer)
        {
            pipeline.DebugLog = writer;
            return pipeline;
        }

        public static MarkdownPipelineBuilder WithLogger(this MarkdownPipelineBuilder pipeline, IPALogger logger, IPALogger.Level level = IPALogger.Level.Debug)
            => pipeline.WithDebugWriter(new LoggerTextWriter(logger, level));

        private sealed class LoggerTextWriter : TextWriter
        {
            private readonly IPALogger logger;
            private readonly IPALogger.Level level;

            public LoggerTextWriter(IPALogger logger, IPALogger.Level level)
            {
                this.logger = logger;
                this.level = level;
            }

            public override Encoding Encoding => Encoding.Default;

            private readonly StringBuilder _builder = new StringBuilder();

            public override void Write(char value)
            {
                if ((value == '\n' || value == '\r') && _builder.Length > 0)
                {
                    Flush();
                }
                else
                {
                    _builder.Append(value);
                }
            }

            public override void Write(string value) => _builder.Append(value);

            public override void WriteLine() => Flush();

            public override void WriteLine(string value)
            {
                _builder.Append(value);
                Flush();
            }

            public override void Flush()
            {
                logger.Log(level, _builder.ToString());
                _builder.Clear();
            }
        }
    }
}