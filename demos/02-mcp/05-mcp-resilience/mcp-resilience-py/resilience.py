import time
from dataclasses import dataclass
from enum import Enum
from tenacity import retry, retry_if_exception_type, stop_after_attempt, wait_exponential_jitter
from backend import TransientError


class CircuitState(Enum):
    CLOSED = "closed"
    OPEN = "open"
    HALF_OPEN = "half_open"


class CircuitBreakerOpenError(Exception):
    pass


@dataclass
class CircuitBreakerConfig:
    failure_threshold: int = 3
    recovery_timeout: float = 10.0
    success_threshold: int = 2


class CircuitBreaker:
    def __init__(self, config):
        self.config = config
        self.state = CircuitState.CLOSED
        self.failure_count = 0
        self.success_count = 0
        self.last_failure_time = None

    def call(self, func, *args, **kwargs):
        if self.state == CircuitState.OPEN:
            if time.time() - self.last_failure_time >= self.config.recovery_timeout:
                self.state = CircuitState.HALF_OPEN
                self.success_count = 0
            else:
                raise CircuitBreakerOpenError(f"Circuit open, next attempt in {self.time_until_recovery():.1f}s")
        try:
            result = func(*args, **kwargs)
        except TransientError:
            self._on_failure()
            raise
        self._on_success()
        return result

    def _on_success(self):
        if self.state == CircuitState.HALF_OPEN:
            self.success_count += 1
            if self.success_count >= self.config.success_threshold:
                self.state = CircuitState.CLOSED
                self.failure_count = 0
        elif self.state == CircuitState.CLOSED:
            self.failure_count = 0

    def _on_failure(self):
        self.failure_count += 1
        self.last_failure_time = time.time()
        if self.failure_count >= self.config.failure_threshold or self.state == CircuitState.HALF_OPEN:
            self.state = CircuitState.OPEN

    def time_until_recovery(self):
        if self.last_failure_time is None:
            return 0
        return max(0, self.config.recovery_timeout - (time.time() - self.last_failure_time))


def mcp_retry_with_backoff(max_attempts=3, base_delay_seconds=0.2, max_delay_seconds=2):
    return retry(
        stop=stop_after_attempt(max_attempts),
        wait=wait_exponential_jitter(initial=base_delay_seconds, max=max_delay_seconds, jitter=0.1),
        retry=retry_if_exception_type(TransientError),
        reraise=True,
    )
