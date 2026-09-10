using System.Reflection;

namespace GenericRepository.Settings
{
    public class AssembliesSetting
    {
        public Assembly[] EntitiesAssemblies { get; set; } = [];
        public Assembly[] EntitiesConfigurationAssemblies { get; set; } = [];
    }
}
