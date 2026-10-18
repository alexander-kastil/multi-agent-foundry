from dataclasses import dataclass

RAW_INVOICES = [
    "INV-001,Contoso Ltd,Cloud hosting,420.00",
    "INV-002,Fabrikam Inc,Office furniture,2750.50",
    "INV-003,Northwind Traders,Security audit,18400.00",
    "INV-004,Adatum Corp,Unreadable scan,-1",
]


@dataclass
class Invoice:
    number: str
    vendor: str
    description: str
    amount: float
