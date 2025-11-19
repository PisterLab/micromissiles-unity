"""Defines simulation-related constants."""

from enum import StrEnum


class AgentType(StrEnum):
    """Agent type enumeration."""
    VESSEL = "Vessel"
    SHORE_BATTERY = "ShoreBattery"
    CARRIER_INTERCEPTOR = "CarrierInterceptor"
    MISSILE_INTERCEPTOR = "MissileInterceptor"

    FIXED_WING_THREAT = "FixedWingThreat"
    ROTARY_WING_THREAT = "RotaryWingThreat"


class EventType(StrEnum):
    """Event type enumeration."""
    NEW_INTERCEPTOR = "NEW_INTERCEPTOR"
    NEW_THREAT = "NEW_THREAT"
    INTERCEPTOR_HIT = "INTERCEPTOR_HIT"
    INTERCEPTOR_MISS = "INTERCEPTOR_MISS"
    THREAT_HIT = "THREAT_HIT"
    THREAT_MISS = "THREAT_MISS"


class Column(StrEnum):
    """CSV column enumeration."""
    TIME = "Time"
    AGENT_TYPE = "AgentType"
    AGENT_ID = "AgentID"
    EVENT = "Event"
    POSITION_X = "PositionX"
    POSITION_Y = "PositionY"
    POSITION_Z = "PositionZ"
    VELOCITY_X = "VelocityX"
    VELOCITY_Y = "VelocityY"
    VELOCITY_Z = "VelocityZ"


# Telemetry file prefix.
TELEMETRY_FILE_PREFIX = "sim_telemetry"

# Event log prefix.
EVENT_LOG_FILE_PREFIX = "sim_events"

# Log directory name for a single run.
RUN_LOG_DIRECTORY_NAME_PATTERN = "run_*_seed_*"


def is_interceptor(agent_type: str) -> bool:
    """Returns whether the given agent type is an interceptor."""
    return agent_type in (
        AgentType.VESSEL,
        AgentType.SHORE_BATTERY,
        AgentType.CARRIER_INTERCEPTOR,
        AgentType.MISSILE_INTERCEPTOR,
    )


def is_threat(agent_type: str) -> bool:
    """Returns whether the given agent type is an interceptor."""
    return agent_type in (
        AgentType.FIXED_WING_THREAT,
        AgentType.ROTARY_WING_THREAT,
    )


def get_agent_color(agent_type: str) -> str:
    """Returns the visualization color for the given agent type."""
    if is_interceptor(agent_type):
        return "blue"
    if is_threat(agent_type):
        return "red"
    return "black"
