import time

INTERACTIONS = {
    frozenset(["warfarin", "aspirin"]): {"severity": "major", "mechanism": "pharmacodynamic", "note": "Additive bleeding risk"},
    frozenset(["atorvastatin", "clarithromycin"]): {"severity": "major", "mechanism": "pharmacokinetic", "note": "CYP3A4 inhibition raises statin exposure"},
    frozenset(["metformin", "lisinopril"]): {"severity": "none", "mechanism": "none", "note": "No clinically relevant interaction"},
}
FORMULARY = {drug for pair in INTERACTIONS for drug in pair}


class TransientError(Exception):
    pass


class PermanentError(Exception):
    pass


class DrugInteractionInstance:
    def __init__(self, name, version, delay_seconds):
        self.name = name
        self.version = version
        self.delay_seconds = delay_seconds
        self.outage = False

    def lookup(self, drug_a, drug_b):
        time.sleep(self.delay_seconds)
        if self.outage:
            raise TransientError(f"{self.name} returned 503")
        for drug in (drug_a, drug_b):
            if drug.lower() not in FORMULARY:
                raise PermanentError(f"invalid medication code: {drug}")
        result = INTERACTIONS.get(
            frozenset([drug_a.lower(), drug_b.lower()]),
            {"severity": "unknown", "mechanism": "unknown", "note": "No record in the formulary, consult a pharmacist"},
        )
        return {"status": "ok", "instance": self.name, "version": self.version, "drug_a": drug_a, "drug_b": drug_b, **result}
