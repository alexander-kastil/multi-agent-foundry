using System.Globalization;
using DotNetEnv;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
Env.Load();

switch (args.FirstOrDefault())
{
    case "single":
        await SingleResponse.Run();
        break;
    case "chain":
        await ChainResponses.Run();
        break;
    case "conversation":
        await ConversationDemo.Run();
        break;
    case "stream":
        await StreamEvents.Run();
        break;
    case "chat":
        await Chat.Run(args.Skip(1));
        break;
}
