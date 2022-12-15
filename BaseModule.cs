using Prism.Ioc;
using Prism.Modularity;
using TQDB_Editor.Common.Controls;
using TQDB_Editor.Common.Services;

namespace TQDB_Editor.Common
{
    [Module(ModuleName = "Base")]
    public class BaseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            ;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<RichTextBlock>();
            containerRegistry.Register<EditorFindWindow>();

            containerRegistry.RegisterSingleton(typeof(IConfigService), typeof(ConfigService));
            containerRegistry.RegisterSingleton(typeof(IConsoleLogService), typeof(ConsoleLogService));
            containerRegistry.RegisterSingleton(typeof(ITemplateService), typeof(TemplatesService));
        }
    }
}
