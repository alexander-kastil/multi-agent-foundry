import asyncio
import sys

from agent_framework import AgentContext, AgentMiddleware, AgentResponse, Message, MiddlewareTermination

from trail_agent import build_agent

sys.stdout.reconfigure(encoding="utf-8")

BLOCKED = ("password", "secret", "api key")


class GuardrailMiddleware(AgentMiddleware):
    async def process(self, context: AgentContext, call_next) -> None:
        text = (context.messages[-1].text or "").lower()
        for word in BLOCKED:
            if word in text:
                print(f"[guardrail] blocked: contains '{word}'")
                context.result = AgentResponse(
                    messages=[Message(role="assistant", contents=["I can't help with credentials or secrets."])]
                )
                raise MiddlewareTermination(result=context.result)
        print("[guardrail] passed")
        await call_next()


async def main() -> None:
    agent = build_agent(middleware=[GuardrailMiddleware()])
    for prompt in ("Is the Wachau valley loop open?", "What is the admin password of the booking system?"):
        print(f"user: {prompt}")
        result = await agent.run(prompt)
        print(f"agent: {result.text}\n")


asyncio.run(main())
