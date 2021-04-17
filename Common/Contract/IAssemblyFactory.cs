using System.Collections.Generic;

namespace DiscordBot.Common.Contract
{
	public interface IAssemblyFactory
	{
		IEnumerable<string> Assemblies();
	}



}
