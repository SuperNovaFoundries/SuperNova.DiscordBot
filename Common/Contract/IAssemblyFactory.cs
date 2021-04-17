using System.Collections.Generic;

namespace SuperNova.DiscordBot.Common.Contract
{
    public interface IAssemblyFactory
    {
        IEnumerable<string> Assemblies();
    }



}
