import asyncio
import json
import time

import httpx
from mcp.client.session import ClientSession
from mcp.client.streamable_http import streamablehttp_client

from support import BASE, HEADERS, admin, section, show, start_server


async def call(session, count, drug_a="warfarin", drug_b="aspirin"):
    last = None
    for _ in range(count):
        result = await session.call_tool("lookup_drug_interaction", {"drug_a": drug_a, "drug_b": drug_b})
        last = json.loads(result.content[0].text)
    return last


async def main():
    server = start_server()
    try:
        section("Authentication")
        print("no key   ->", httpx.get(f"{BASE}/health").status_code)
        print("with key ->", httpx.get(f"{BASE}/health", headers=HEADERS).status_code)

        async with (
            streamablehttp_client(f"{BASE}/mcp", headers=HEADERS) as (read, write, _),
            ClientSession(read, write) as session,
        ):
            await session.initialize()

            section("Permanent error returns immediately")
            started = time.time()
            print(await call(session, 1, "zorbiflex", "aspirin"))
            print(f"took {time.time() - started:.2f}s, no retry")

            section("Latency-based routing")
            await call(session, 8, "metformin", "lisinopril")
            show("after 8 calls")

            section("Weighted canary at 30 percent")
            admin("/admin/canary?percent=30")
            await call(session, 30)
            show("after 30 calls")
            admin("/admin/canary?percent=0")

            section("Outage: retries, circuit breaker, fallback")
            admin("/admin/outage?on=1")
            started = time.time()
            await call(session, 1, "atorvastatin", "clarithromycin")
            print(f"first failing call took {time.time() - started:.1f}s (retries with backoff)")
            await call(session, 2)
            show("after 3 failed calls")
            started = time.time()
            fallback = await call(session, 1)
            print(f"open circuit answered in {time.time() - started:.2f}s ->", fallback)

            section("Recovery")
            admin("/admin/outage?on=0")
            time.sleep(10.5)
            await call(session, 2)
            show("after recovery")
    finally:
        server.terminate()


try:
    asyncio.run(main())
except KeyboardInterrupt:
    print("\nInterrupted.")
