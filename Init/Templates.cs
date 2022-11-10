using Godot;
using Microsoft.Extensions.Logging;
using TQDB_Parser;

namespace TQDBEditor.Common
{
    public partial class Templates : Node
    {
        private TemplateManager templateManager;
        private Config config;
        private ILogger logger;

        public TemplateManager TemplateManager => templateManager;

        public override void _Ready()
        {
            config = this.GetEditorConfig();
            config.TrulyReady += Config_WorkingDirChanged;
            if (config.ValidateConfig())
                Config_WorkingDirChanged();
            config.WorkingDirChanged += Config_WorkingDirChanged;
            logger = this.GetConsoleLogger();
        }

        private void Config_WorkingDirChanged()
        {
            templateManager = new TemplateManager(config.WorkingDir, useParallel: true, logger: logger);
            //templateManager.ParseAllTemplates();
            //templateManager.ResolveAllIncludes();
        }
    }
}
