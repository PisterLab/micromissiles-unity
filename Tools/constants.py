"""Defines simulation-related constants."""

from enum import StrEnum


class AgentType(StrEnum):
    """Agent type enumeration."""
    INTERCEPTOR = "M"
    THREAT = "T"


class EventType(StrEnum):
    """Event type enumeration."""
    NEW_INTERCEPTOR = "NEW_INTERCEPTOR"
    NEW_THREAT = "NEW_THREAT"
    INTERCEPTOR_HIT = "INTERCEPTOR_HIT"
    INTERCEPTOR_MISS = "INTERCEPTOR_MISS"


# Telemetry file prefix.
TELEMETRY_FILE_PREFIX = "sim_telemetry_"

# Event log prefix.
EVENT_LOG_FILE_PREFIX = "sim_events_"
