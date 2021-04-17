using SuperNova.DiscordBot.Common.Contract;
using System.Collections.Generic;
using System.Composition;
using System.Linq;

namespace SuperNova.DiscordBot.Business.Core
{
    [Export(typeof(IAssemblyFactory))]
    public class DiscordAssemblyFactory : IAssemblyFactory
    {
        private static readonly string[] assemblies = {
            $"/var/task\\SuperNova.DiscordBot.Business.dll",
        };

        //todo - pull assemblies from s3 bucket for pluggability
        public IEnumerable<string> Assemblies() => assemblies.ToList().AsReadOnly();


        public DiscordAssemblyFactory() { }
    }

}
