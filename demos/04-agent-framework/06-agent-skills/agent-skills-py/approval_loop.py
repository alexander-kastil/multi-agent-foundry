from agent_framework import Message


async def run_with_approvals(agent, prompt: str, session, approve: bool = True):
    steps = []
    result = await agent.run(prompt, session=session)
    while result.user_input_requests:
        responses = []
        for request in result.user_input_requests:
            call = request.function_call
            steps.append(f"{call.name}({call.arguments})")
            responses.append(Message(role="user", contents=[request.to_function_approval_response(approve)]))
        result = await agent.run(responses, session=session)
    return result, steps
