import sys

RATES = {"summer": 38, "winter": 52}

nights, persons, season = int(sys.argv[1]), int(sys.argv[2]), sys.argv[3]
total = nights * persons * RATES[season]
print(f"total={total} euro rate={RATES[season]} euro per person and night")
