from dataclasses import asdict, dataclass, field


@dataclass
class Employee:
    first_name: str
    last_name: str
    email: str
    current_role: str
    skills: list[str] = field(default_factory=list)
    spoken_languages: list[str] = field(default_factory=list)

    def to_dict(self):
        return asdict(self)


class EmployeeStore:
    def __init__(self):
        self._employees = {
            e.email.lower(): e
            for e in [
                Employee("Carlos", "Gomez", "carlos.gomez@restaurant.com", "Sous Chef", ["Menu Planning", "Food Safety"], ["Spanish", "English", "French"]),
                Employee("Marie", "Dubois", "marie.dubois@restaurant.com", "Pastry Chef", ["Dessert Creation", "Baking"], ["French", "English", "Italian"]),
                Employee("Luca", "Rossi", "luca.rossi@restaurant.com", "Head Waiter", ["Wine Pairing", "Table Management"], ["Italian", "English", "Spanish"]),
                Employee("Anna", "Huber", "anna.huber@restaurant.com", "Restaurant Manager", ["Scheduling", "Team Leadership"], ["German", "English"]),
            ]
        }

    def all(self):
        return sorted(self._employees.values(), key=lambda e: e.last_name)

    def find(self, email):
        return self._employees.get(email.lower())

    def add(self, employee):
        if employee.email.lower() in self._employees:
            return False
        self._employees[employee.email.lower()] = employee
        return True

    def remove(self, email):
        return self._employees.pop(email.lower(), None) is not None

    def search(self, term):
        term = term.lower()
        return [
            e for e in self.all()
            if term in f"{e.first_name} {e.last_name} {e.email} {e.current_role} {' '.join(e.skills)}".lower()
        ]
