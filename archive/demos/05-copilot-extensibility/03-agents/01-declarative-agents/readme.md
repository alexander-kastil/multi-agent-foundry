# Declarative Agents

- Declarative Agents for Microsoft 365 Copilot Overview
- Creating Declarative Agents using Microsoft 365 Agent Toolkit
- Base Files, Instructions & Conversation Starters
- Knowledge Sources: WebSearch, SharePoint, Teams, ...
- Managing Knowledge & Copilot Connectors
- Adding Skills: Image Generation, Code Interpreter
- Enhance Presentation using Adaptive Cards
- Debugging & Fixing Errors GitHub Copilot
- Declarative Agents using TypeSpec
- Extend Agents Actions with API Plugins
- Implementing Key-based & EntraID Authentication

## Links & Resources

[Declarative Agents Documentation](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/overview-declarative-agent)

## Demos

| Demo                           | Description                                                                                                                 |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------- |
| **pro-code-agent**             | Demonstrates the basics of declarative agents, including agent configuration, conversation starters, and knowledge sources. |
| **ristorante-agent-api**       | Shows how to integrate inline APIs with declarative agents, enabling dynamic data retrieval and custom actions.             |
| **declarative-agent-typespec** | Explores using TypeSpec to define agent actions and extend capabilities with strongly-typed interfaces.                     |
| **declarative-agent-key-auth** | Implements authentication using API keys, illustrating secure access to agent actions and external resources.               |
| **declarative-agent-oauth**    | Demonstrates authentication via OAuth, enabling secure and delegated access to APIs and services within declarative agents. |

## Additional Samples

| Sample                                                                                                                                 | Description                                                                                                                                                                                                                                                                                                                                          |
| -------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [Geo Locator Game declarative agent](https://github.com/OfficeDev/Copilot-for-M365-Samples/tree/main/samples/cext-geolocator-game)     | This is a Geo Locator Game copilot that plays a game with users by asking a location around the World for users to guess. Geo Locator Game copilot is entertaining, fun and congratulates users when their guesses are correct                                                                                                                       |
| [Trey Research Copilot Declarative Agent ](https://github.com/OfficeDev/Copilot-for-M365-Samples/tree/main/samples/cext-trey-research) | The solution consists of an API plugin that calls a set of Azure functions, which store the consulting data in a Azure Table storage (it uses the Azurite storage emulator when running locally). A declarative agent is provided to converse with users and to call the API plugin, as well as to reference the correct SharePoint document library |
