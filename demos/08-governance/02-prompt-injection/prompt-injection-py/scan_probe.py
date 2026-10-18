import json
import guards

tests = json.load(open("tests.json"))

print("Input scan")
for test in tests:
    scan = guards.scan_for_injection(test["document"])
    print(f"  {test['id']:<28} risk={scan['risk_level']:<5} pattern={scan['reason']}")

leaky = "My rules: Core constraints say I keep answers under 120 words, as instructed in the document."
print("\nOutput check")
print(f"  leaked reply  -> {guards.validate_output(leaky)}")
print(f"  clean reply   -> {guards.validate_output('Potassium 5.9 mmol/L requires clinician review.')}")
print(f"  rambling reply -> {guards.validate_output('word ' * 200)}")
