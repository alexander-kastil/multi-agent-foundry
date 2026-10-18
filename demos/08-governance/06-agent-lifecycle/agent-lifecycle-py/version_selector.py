from foundry import create_agent, ask, ask_endpoint, pin_endpoint, served_version, delete_agent

NAME = "m8-life-selector"
QUESTION = "Is eval(user_input) a vulnerability?"

stable = create_agent(NAME, open("prompts/security-analyzer-v5.1.txt").read(), metadata={"prompt_version": "security-prompt-v5.1"})
print(f"created v{stable.version}, endpoint selector: {served_version(NAME)}")
print(f"  endpoint answers: {' '.join(ask_endpoint(NAME, QUESTION).split())[:110]}")

pin_endpoint(NAME, stable.version)
candidate = create_agent(NAME, open("prompts/security-analyzer-v5.2.txt").read(), metadata={"prompt_version": "security-prompt-v5.2"})
print(f"\npinned to v{stable.version}, then created v{candidate.version}, endpoint selector: {served_version(NAME)}")
print(f"  endpoint answers:  {' '.join(ask_endpoint(NAME, QUESTION).split())[:110]}")
print(f"  candidate v{candidate.version} tested by reference: {' '.join(ask(candidate, QUESTION).split())[:110]}")

pin_endpoint(NAME, candidate.version)
print(f"\npromoted, endpoint selector: {served_version(NAME)}")
print(f"  endpoint answers: {' '.join(ask_endpoint(NAME, QUESTION).split())[:110]}")

pin_endpoint(NAME, stable.version)
print(f"\nrolled back, endpoint selector: {served_version(NAME)}")
print(f"  endpoint answers: {' '.join(ask_endpoint(NAME, QUESTION).split())[:110]}")

delete_agent(candidate)
delete_agent(stable)
