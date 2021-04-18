using System.Collections.Generic;

namespace SuperNova.DiscordBot.Contract
{
    public interface IAssemblyFactory
    {
        IEnumerable<string> Assemblies();
    }



}
