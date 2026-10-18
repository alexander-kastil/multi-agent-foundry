import json
import os
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
import guards

load_dotenv()
model = os.getenv("MODEL_DEPLOYMENT_NAME")
baseline = open("prompts/v1.0.0.txt").read()
hardened = open("prompts/v1.1.0.txt").read()
tests = json.load(open("tests.json"))

with (
    DefaultAzureCredential() as credential,
    AIProjectClient(endpoint=os.getenv("PROJECT_ENDPOINT"), credential=credential) as project_client,
    project_client.get_openai_client() as openai_client,
):

    def ask(instructions, document):
        return guards.filtered_call(
            lambda: openai_client.responses.create(model=model, instructions=instructions, input=document)
        )

    def layer_baseline(test):
        return ask(baseline, test["document"]), []

    def layer_structural(test):
        return ask(hardened, guards.wrap_document(test["document"])), []

    def layer_full(test):
        scan = guards.scan_for_injection(test["document"])
        instructions = hardened + guards.ELEVATED_SAFETY_NOTE if scan["risk_level"] == "high" else hardened
        output = ask(instructions, guards.wrap_document(test["document"]))
        return output, guards.validate_output(output)

    layers = {
        "1 no defense": layer_baseline,
        "2 delimiters + persona stability": layer_structural,
        "3 scan + delimiters + persona + output check": layer_full,
    }

    for name, run in layers.items():
        print(f"\n=== {name} ===")
        survived = 0
        for test in tests:
            output, findings = run(test)
            ok = guards.passes(test, output)
            survived += ok
            blocked = " (blocked by platform jailbreak filter)" if output == guards.PLATFORM_BLOCK else ""
            print(f"  {test['id']:<28} {'DEFENDED' if ok else 'COMPROMISED'}  output_flags={findings}{blocked}")
            print(f"    {' '.join(output.split())[:110]}")
        print(f"  defended {survived}/{len(tests)}")
