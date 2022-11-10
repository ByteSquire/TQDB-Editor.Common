using Godot;
using Microsoft.Extensions.Logging;
using System;
using TQDB_Parser;

namespace TQDBEditor.Common
{
    public static class AutoloadsExtension
    {
        public static Config GetEditorConfig(this Node me)
        {
            return me.GetNode<Config>("/root/Config");
        }

        public static TemplateManager GetTemplateManager(this Node me)
        {
            return me.GetNode<Templates>("/root/Templates").TemplateManager;
        }

        public static ILogger GetConsoleLogger(this Node me)
        {
            return me.GetNode<ConsoleLogHandler>("/root/Logging").Logger;
        }

        public static PCKHandler GetPCKHandler(this Node node)
        {
            return node.GetNode<PCKHandler>("/root/PckHandler");
        }
    }
}