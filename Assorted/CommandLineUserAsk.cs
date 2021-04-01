using Multicad.DatabaseServices;

namespace Assorted
{
	public static class CommandLineUserAsk
	{

		private static bool AskUserYesOrNo()
		{
			var result = true;

			using (var jig = new InputJig())
			{
				var cmdBuilder = jig.BuildCommand(0, -1);
				cmdBuilder.Add(1, "Yes", "Y");
				cmdBuilder.Add(2, "No", "N");

				cmdBuilder.Complete();
				var res = jig.GetTextInput("So yes or no?");
				if (res == true)
				{
					CommandEventArgs cmdResult = jig.GetCommand();
					if (cmdResult.CommandID == 2)
						result = false;
				}
			}

			return result;
		}
	}
}
