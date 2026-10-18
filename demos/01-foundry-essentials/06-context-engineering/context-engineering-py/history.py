import json

CITIES = ["Salzburg", "Graz", "Linz", "Innsbruck", "Vienna", "Klagenfurt", "Bregenz", "Eisenstadt"]
SKIES = ["clear", "light rain", "cloudy", "sunny spells", "showers", "fog"]

QUESTION = "What is my booking reference and who am I travelling with?"


def forecast(day):
    return "\n".join(
        f"{hour:02d}:00 {SKIES[(hour + day) % len(SKIES)]}, {8 + (hour * day) % 14} C, wind {5 + (hour + day) % 20} km/h, rain {(hour * day) % 60} percent"
        for hour in range(24)
    )


def turn(day):
    city = CITIES[day % len(CITIES)]
    first = "My booking reference is ZX-4417 and I am travelling with my sister Marta. " if day == 1 else ""
    return [
        {"role": "user", "content": f"{first}Day {day}: what is the weather in {city}?"},
        {"type": "function_call", "call_id": f"call_{day}", "name": "get_forecast", "arguments": json.dumps({"city": city, "day": day}), "status": "completed"},
        {"type": "function_call_output", "call_id": f"call_{day}", "output": forecast(day)},
        {
            "type": "message",
            "role": "assistant",
            "status": "completed",
            "content": [{"type": "output_text", "text": f"Day {day} in {city} is mostly {SKIES[day % len(SKIES)]}, so plan indoor stops for the afternoon."}],
        },
    ]


def build_turns(days):
    return [turn(day) for day in range(1, days + 1)]


def flatten(turns):
    return [item for group in turns for item in group]
