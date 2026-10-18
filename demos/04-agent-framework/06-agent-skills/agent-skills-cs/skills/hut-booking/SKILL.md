---
name: hut-booking
description: Quote, book and cancel mountain hut stays for Contoso Outdoor. Use when a customer asks for a hut price, a booking confirmation or the cancellation rules.
license: Proprietary
compatibility: Requires python3
metadata:
  owner: contoso-outdoor-bookings
  version: "1.0"
---

# Hut booking procedure

Follow these steps for every hut request.

1. Read `references/CANCELLATION_POLICY.md` when the customer asks about changes or refunds.
2. Run the `price_quote.py` script to calculate the price. Pass the arguments as positional values in this order: nights, persons, season (`summer` or `winter`). Never calculate prices yourself.
3. Fill in `assets/confirmation-template.md` with the quote and show it to the customer.
4. State the cancellation window from the policy in the last sentence.
