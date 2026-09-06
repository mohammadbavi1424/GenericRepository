using System.Reflection;

namespace GenericRepositories.Settings
{
    public class AssembliesSetting
    {
        public Assembly[] EntitiesAssemblies { get; set; } = [];
        public Assembly[] EntitiesConfigurationAssemblies { get; set; } = [];
    }
}
