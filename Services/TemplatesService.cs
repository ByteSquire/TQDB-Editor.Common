using Microsoft.Extensions.Logging;
using TQDB_Parser;
using TQDBEditor.Common.Services;

namespace TQDB_Editor.Common.Services
{
    public interface ITemplateService
    {
        public TemplateManager TemplateManager { get; }
    }

    public partial class TemplatesService : ITemplateService
    {
        private TemplateManager templateManager;
        private readonly IConfigService config;
        private readonly ILogger logger;

        public TemplateManager TemplateManager => templateManager;

        public TemplatesService(IConfigService config, ILogService logService)
        {
            this.config = config;
            logger = logService;
            config.WorkingDirChanged += Config_WorkingDirChanged;
            Config_WorkingDirChanged();
        }

        private void Config_WorkingDirChanged()
        {
            templateManager = new TemplateManager(config.WorkingDir, useParallel: true, logger: logger);
            //templateManager.ParseAllTemplates();
            //templateManager.ResolveAllIncludes();
        }
    }
}
