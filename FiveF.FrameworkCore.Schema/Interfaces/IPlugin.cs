namespace TCDev.APIGenerator.Schema.Interfaces
{

    // Plugin for any Rasepi API
    public interface IPlugin
    {
        public string Name { get; init; }
        public string Description { get; init; }
        public string Version { get; init; }
  
    }
}
