"""Run-level performance metrics derived from a simulation event log."""

from collections.abc import Iterable

import numpy as np
import pandas as pd

EVENT = "Event"
AGENT_TYPE = "AgentType"
AGENT_ID = "AgentID"
TIME = "Time"
POSITION_COLUMNS = ["PositionX", "PositionY", "PositionZ"]
RELATED_AGENT_ID = "RelatedAgentID"
MESSAGE_TYPE = "MessageType"
MESSAGE_ID = "MessageID"
CORRELATION_ID = "CorrelationID"
TARGET_IDS = "TargetIDs"

NEW_INTERCEPTOR = "NEW_INTERCEPTOR"
NEW_THREAT = "NEW_THREAT"
INTERCEPTOR_HIT = "INTERCEPTOR_HIT"
INTERCEPTOR_MISS = "INTERCEPTOR_MISS"
INTERCEPTOR_DESTROYED = "INTERCEPTOR_DESTROYED"
THREAT_HIT = "THREAT_HIT"
THREAT_DESTROYED = "THREAT_DESTROYED"
INTERCEPTOR_TARGET_ASSIGNED = "INTERCEPTOR_TARGET_ASSIGNED"
MESSAGE_SENT = "MESSAGE_SENT"
MESSAGE_DELIVERED = "MESSAGE_DELIVERED"
MISSILE_INTERCEPTOR = "MissileInterceptor"
ASSIGN_TARGET_REQUEST = "AssignTargetRequest"
ASSIGN_TARGET_RESPONSE = "AssignTargetResponse"

PROCESS_RUN_METRICS = [
    "num_missile_interceptors",
    "num_missile_interceptor_hits",
    "num_missile_interceptor_misses",
    "num_missile_interceptors_destroyed",
    "interceptor_hit_rate",
    "interceptor_efficiency",
    "minimum_intercept_distance_m",
]

REPORTABLE_METRICS = [
    "num_threats",
    "num_threat_hits",
    "num_threats_destroyed",
    "num_unresolved_threats",
    "threat_penetration_rate",
    "threat_destroyed_rate",
    "unresolved_threat_rate",
    "num_missile_interceptors",
    "num_missile_interceptor_hits",
    "num_missile_interceptor_misses",
    "num_missile_interceptors_destroyed",
    "interceptor_hit_rate",
    "interceptor_terminal_success_rate",
    "interceptor_efficiency",
    "missile_interceptors_per_hit",
    "missile_interceptors_per_threat_destroyed",
    "minimum_intercept_distance_m",
    "mean_intercept_distance_m",
    "median_intercept_distance_m",
    "num_threats_with_destruction_time",
    "mean_threat_spawn_to_destroyed_time_s",
    "last_threat_spawn_to_destroyed_time_s",
    "last_threat_destroyed_time_s",
    "time_to_destroy_all_threats_s",
    "last_threat_outcome_time_s",
    "last_event_time_s",
    "time_to_first_threat_resolution_s",
    "time_to_50_percent_threats_resolved_s",
    "time_to_90_percent_threats_resolved_s",
    "time_to_100_percent_threats_resolved_s",
    "threat_exposure_time_s",
    "mean_threat_exposure_time_s",
    "mean_missile_interceptor_flight_time_s",
    "median_missile_interceptor_flight_time_s",
    "p95_missile_interceptor_flight_time_s",
    "mean_missile_interceptor_hit_displacement_m",
    "median_missile_interceptor_hit_displacement_m",
    "p95_missile_interceptor_hit_displacement_m",
    "mean_missile_interceptor_hit_speed_proxy_mps",
    "peak_active_missile_interceptors",
    "num_missile_interceptors_with_miss",
    "num_missile_interceptors_missed_then_hit",
    "missile_interceptor_miss_recovery_rate",
    "num_missile_interceptors_no_terminal_outcome",
    "missile_interceptor_hit_fraction",
    "missile_interceptor_destroyed_fraction",
    "missile_interceptor_no_terminal_fraction",
    "time_to_first_missile_interceptor_launch_s",
    "time_to_first_missile_interceptor_hit_s",
]

LOG_DEPENDENT_METRICS = [
    "num_coordination_requests",
    "num_coordination_responses",
    "coordination_response_rate",
    "mean_coordination_response_time_s",
    "median_coordination_response_time_s",
    "p95_coordination_response_time_s",
    "num_reassigned_missile_interceptors",
    "num_reassigned_missile_interceptor_hits",
    "num_reassigned_missile_interceptor_misses",
    "num_reassigned_missile_interceptors_destroyed",
    "reassigned_missile_interceptor_efficiency",
    "num_missile_interceptor_hits_on_non_original_targets",
]

DEFAULT_ANALYSIS_METRICS = REPORTABLE_METRICS + LOG_DEPENDENT_METRICS

DEFAULT_PLOT_METRICS = REPORTABLE_METRICS

ADVANCED_ANALYSIS_METRICS = [
    "time_to_first_threat_resolution_s",
    "time_to_50_percent_threats_resolved_s",
    "time_to_90_percent_threats_resolved_s",
    "time_to_100_percent_threats_resolved_s",
    "mean_threat_exposure_time_s",
    "mean_missile_interceptor_flight_time_s",
    "p95_missile_interceptor_flight_time_s",
    "mean_missile_interceptor_hit_displacement_m",
    "mean_missile_interceptor_hit_speed_proxy_mps",
    "peak_active_missile_interceptors",
    "num_missile_interceptors_with_miss",
    "num_missile_interceptors_missed_then_hit",
    "missile_interceptor_miss_recovery_rate",
    "num_missile_interceptors_no_terminal_outcome",
    "missile_interceptor_hit_fraction",
    "missile_interceptor_destroyed_fraction",
    "missile_interceptor_no_terminal_fraction",
    "missile_interceptors_per_threat_destroyed",
    "time_to_first_missile_interceptor_launch_s",
    "time_to_first_missile_interceptor_hit_s",
]

ADVANCED_PLOT_METRICS = [
    "time_to_50_percent_threats_resolved_s",
    "time_to_90_percent_threats_resolved_s",
    "time_to_100_percent_threats_resolved_s",
    "mean_threat_exposure_time_s",
    "mean_missile_interceptor_flight_time_s",
    "p95_missile_interceptor_flight_time_s",
    "mean_missile_interceptor_hit_displacement_m",
    "mean_missile_interceptor_hit_speed_proxy_mps",
    "peak_active_missile_interceptors",
    "missile_interceptor_miss_recovery_rate",
    "num_missile_interceptors_no_terminal_outcome",
    "missile_interceptors_per_threat_destroyed",
]

METRIC_LABELS = {
    "num_threats":
        "Number of Threats Spawned",
    "num_threat_hits":
        "Number of Threat Penetrations",
    "num_threats_destroyed":
        "Number of Threats Destroyed",
    "num_unresolved_threats":
        "Number of Unresolved Threats",
    "threat_penetration_rate":
        "Threat Penetration Rate",
    "threat_destroyed_rate":
        "Threat Destroyed Rate",
    "unresolved_threat_rate":
        "Unresolved Threat Rate",
    "num_missile_interceptors":
        "Number of Missile Interceptors Launched",
    "num_missile_interceptor_hits":
        "Number of Missile Interceptor Hits",
    "num_missile_interceptor_misses":
        "Number of Missile Interceptor Misses",
    "num_missile_interceptors_destroyed":
        "Number of Missile Interceptors Destroyed",
    "interceptor_hit_rate":
        "Missile Interceptor Hit Rate",
    "interceptor_terminal_success_rate":
        "Interceptor Terminal Success Rate",
    "interceptor_efficiency":
        "Missile Interceptor Efficiency",
    "missile_interceptors_per_hit":
        "Missile Interceptors per Hit",
    "minimum_intercept_distance_m":
        "Minimum Intercept Distance [m]",
    "mean_intercept_distance_m":
        "Mean Intercept Distance [m]",
    "median_intercept_distance_m":
        "Median Intercept Distance [m]",
    "num_threats_with_destruction_time":
        "Number of Threats with a Recorded Destruction Time",
    "mean_threat_spawn_to_destroyed_time_s":
        "Mean Threat Spawn-to-Destroyed Time [s]",
    "last_threat_spawn_to_destroyed_time_s":
        "Longest Threat Spawn-to-Destroyed Time [s]",
    "last_threat_destroyed_time_s":
        "Last Threat-Destroyed Event Time [s]",
    "time_to_destroy_all_threats_s":
        "Time from First Threat Spawn Until All Threats Are Destroyed [s]",
    "last_event_time_s":
        "Last Recorded Event Time [s]",
    "num_coordination_requests":
        "Number of Coordination Requests",
    "num_coordination_responses":
        "Number of Coordination Responses",
    "coordination_response_rate":
        "Coordination Response Rate",
    "mean_coordination_response_time_s":
        "Mean Assignment-Request-to-Response Time [s]",
    "median_coordination_response_time_s":
        "Median Assignment-Request-to-Response Time [s]",
    "p95_coordination_response_time_s":
        "95th Percentile Assignment-Request-to-Response Time [s]",
    "num_reassigned_missile_interceptors":
        "Number of Missile Interceptors Reassigned at Least Once",
    "num_reassigned_missile_interceptor_hits":
        "Number of Hits by Reassigned Missile Interceptors",
    "num_reassigned_missile_interceptor_misses":
        "Number of Misses by Reassigned Missile Interceptors",
    "num_reassigned_missile_interceptors_destroyed":
        "Number of Reassigned Missile Interceptors Destroyed",
    "reassigned_missile_interceptor_efficiency":
        "Reassigned Missile Interceptor Efficiency",
    "num_missile_interceptor_hits_on_non_original_targets":
        "Number of Hits on a Non-Original Target",
    "last_threat_outcome_time_s":
        "Last Threat Outcome Time [s]",
    "time_to_first_threat_resolution_s":
        "Time to First Threat Resolution [s]",
    "time_to_50_percent_threats_resolved_s":
        "Time to 50% of Threats Resolved [s]",
    "time_to_90_percent_threats_resolved_s":
        "Time to 90% of Threats Resolved [s]",
    "time_to_100_percent_threats_resolved_s":
        "Time to 100% of Threats Resolved [s]",
    "threat_exposure_time_s":
        "Threat Exposure [threat-s]",
    "mean_threat_exposure_time_s":
        "Mean Threat Exposure Time [s/threat]",
    "mean_missile_interceptor_flight_time_s":
        "Mean Successful Interceptor Flight Time [s]",
    "median_missile_interceptor_flight_time_s":
        "Median Successful Interceptor Flight Time [s]",
    "p95_missile_interceptor_flight_time_s":
        "95th Percentile Successful Interceptor Flight Time [s]",
    "mean_missile_interceptor_hit_displacement_m":
        "Mean Launch-to-Hit Displacement [m]",
    "median_missile_interceptor_hit_displacement_m":
        "Median Launch-to-Hit Displacement [m]",
    "p95_missile_interceptor_hit_displacement_m":
        "95th Percentile Launch-to-Hit Displacement [m]",
    "mean_missile_interceptor_hit_speed_proxy_mps":
        "Mean Straight-Line Launch-to-Hit Speed Proxy [m/s]",
    "peak_active_missile_interceptors":
        "Peak Number of Simultaneously Active Missile Interceptors",
    "num_missile_interceptors_with_miss":
        "Number of Missile Interceptors with at Least One Miss",
    "num_missile_interceptors_missed_then_hit":
        "Number of Missile Interceptors That Missed Then Hit",
    "missile_interceptor_miss_recovery_rate":
        "Miss Recovery Rate",
    "num_missile_interceptors_no_terminal_outcome":
        "Number of Missile Interceptors with No Terminal Outcome",
    "missile_interceptor_hit_fraction":
        "Eventually-Hit Fraction of Spawned Interceptors",
    "missile_interceptor_destroyed_fraction":
        "Destroyed-without-Hit Fraction of Spawned Interceptors",
    "missile_interceptor_no_terminal_fraction":
        "No-Terminal-Outcome Fraction of Spawned Interceptors",
    "missile_interceptors_per_threat_destroyed":
        "Missile Interceptors Spawned per Threat Destroyed",
    "time_to_first_missile_interceptor_launch_s":
        "Time to First Missile Interceptor Launch [s]",
    "time_to_first_missile_interceptor_hit_s":
        "Time to First Missile Interceptor Hit [s]",
}

# Short, presentation-ready definitions used as plot subtitles. Equations use
# event-log counts and timestamps; N denotes a count and t denotes simulation
# time. Keep these concise because every generated metric figure displays one.
METRIC_DESCRIPTIONS = {
    "num_threats":
        "N_spawned = unique threats with a NEW_THREAT event.",
    "num_threat_hits":
        "N_penetrated = number of THREAT_HIT events.",
    "num_threats_destroyed":
        "N_destroyed = number of THREAT_DESTROYED events.",
    "num_unresolved_threats":
        "N_unresolved = spawned threats with neither a hit nor destruction outcome.",
    "threat_penetration_rate":
        "Penetration rate = N_penetrated / N_spawned.",
    "threat_destroyed_rate":
        "Destroyed rate = N_destroyed / N_spawned.",
    "unresolved_threat_rate":
        "Unresolved rate = N_unresolved / N_spawned.",
    "num_missile_interceptors":
        "N_launched = number of missile-interceptor NEW_INTERCEPTOR events.",
    "num_missile_interceptor_hits":
        "N_hits = number of missile-interceptor INTERCEPTOR_HIT events.",
    "num_missile_interceptor_misses":
        "N_misses = number of missile-interceptor INTERCEPTOR_MISS events.",
    "num_missile_interceptors_destroyed":
        "N_destroyed = number of missile-interceptor INTERCEPTOR_DESTROYED events.",
    "interceptor_hit_rate":
        "Hit rate = N_hits / (N_hits + N_misses).",
    "interceptor_terminal_success_rate":
        "Terminal success = N_hits / (N_hits + N_misses + N_destroyed).",
    "interceptor_efficiency":
        "Efficiency = N_hits / N_launched.",
    "missile_interceptors_per_hit":
        "Interceptors per hit = N_launched / N_hits.",
    "missile_interceptors_per_threat_destroyed":
        "Interceptors per threat destroyed = N_launched / N_threats_destroyed.",
    "minimum_intercept_distance_m":
        "Minimum ||hit position||: closest interceptor hit to the coordinate origin.",
    "mean_intercept_distance_m":
        "Mean ||hit position||: average interceptor-hit distance from the origin.",
    "median_intercept_distance_m":
        "Median ||hit position||: median interceptor-hit distance from the origin.",
    "num_threats_with_destruction_time":
        "Number of threats with valid spawn and subsequent destruction timestamps.",
    "mean_threat_spawn_to_destroyed_time_s":
        "Mean over destroyed threats of (t_destroyed - t_spawned).",
    "last_threat_spawn_to_destroyed_time_s":
        "Maximum over destroyed threats of (t_destroyed - t_spawned).",
    "last_threat_destroyed_time_s":
        "Maximum absolute timestamp of a threat's first THREAT_DESTROYED event.",
    "time_to_destroy_all_threats_s":
        ("t_last_destroyed - t_first_spawn; available only when every threat "
         "is destroyed."),
    "last_threat_outcome_time_s":
        "Maximum timestamp of any THREAT_HIT or THREAT_DESTROYED event.",
    "last_event_time_s":
        "Maximum timestamp of any event recorded in the run.",
    "time_to_first_threat_resolution_s":
        "Minimum threat spawn-to-outcome time; outcome is hit or destruction.",
    "time_to_50_percent_threats_resolved_s":
        "50% order statistic of per-threat (t_first_outcome - t_spawn).",
    "time_to_90_percent_threats_resolved_s":
        "90% order statistic of per-threat (t_first_outcome - t_spawn).",
    "time_to_100_percent_threats_resolved_s":
        "Maximum per-threat spawn-to-outcome time; requires every threat to resolve.",
    "threat_exposure_time_s":
        "Total exposure = sum(t_outcome or t_log_end - t_spawn) over all threats.",
    "mean_threat_exposure_time_s":
        "Mean exposure = total threat exposure / N_spawned.",
    "mean_missile_interceptor_flight_time_s":
        "Mean over hit interceptors of (t_first_hit - t_launch).",
    "median_missile_interceptor_flight_time_s":
        "Median over hit interceptors of (t_first_hit - t_launch).",
    "p95_missile_interceptor_flight_time_s":
        "95th percentile over hit interceptors of (t_first_hit - t_launch).",
    "mean_missile_interceptor_hit_displacement_m":
        "Mean ||hit position - launch position|| for interceptors that hit.",
    "median_missile_interceptor_hit_displacement_m":
        "Median ||hit position - launch position|| for interceptors that hit.",
    "p95_missile_interceptor_hit_displacement_m":
        "95th percentile of ||hit position - launch position|| for hit interceptors.",
    "mean_missile_interceptor_hit_speed_proxy_mps":
        "Mean straight-line speed proxy = launch-to-hit displacement / flight time.",
    "peak_active_missile_interceptors":
        "Maximum simultaneous launched-minus-terminal missile-interceptor count.",
    "num_missile_interceptors_with_miss":
        "Number of unique launched interceptors with at least one miss event.",
    "num_missile_interceptors_missed_then_hit":
        "Number of unique interceptors with both a miss and a later hit.",
    "missile_interceptor_miss_recovery_rate":
        "Miss recovery = N_interceptors_that_missed_then_hit / N_with_a_miss.",
    "num_missile_interceptors_no_terminal_outcome":
        "Launched interceptors with neither a hit nor destroyed terminal event.",
    "missile_interceptor_hit_fraction":
        "Eventually-hit fraction = unique hit interceptors / N_launched.",
    "missile_interceptor_destroyed_fraction":
        ("Destroyed-without-hit fraction = unique destroyed non-hit "
         "interceptors / N_launched."),
    "missile_interceptor_no_terminal_fraction":
        "No-terminal-outcome fraction = N_without_hit_or_destruction / N_launched.",
    "time_to_first_missile_interceptor_launch_s":
        "t_first_interceptor_launch - t_first_threat_spawn.",
    "time_to_first_missile_interceptor_hit_s":
        "t_first_interceptor_hit - t_first_threat_spawn.",
    "num_coordination_requests":
        "Number of sent initial target-assignment request messages.",
    "num_coordination_responses":
        "Number of initial assignment requests matched to a delivered response.",
    "coordination_response_rate":
        "Coordination response rate = N_matched_responses / N_requests.",
    "mean_coordination_response_time_s":
        "Mean matched assignment response time = t_response - t_request.",
    "median_coordination_response_time_s":
        "Median matched assignment response time = t_response - t_request.",
    "p95_coordination_response_time_s":
        "95th percentile matched assignment response time.",
    "num_reassigned_missile_interceptors":
        ("Unique interceptors assigned a target set different from their "
         "first assignment."),
    "num_reassigned_missile_interceptor_hits":
        "Number of hit events recorded by reassigned interceptors.",
    "num_reassigned_missile_interceptor_misses":
        "Number of miss events recorded by reassigned interceptors.",
    "num_reassigned_missile_interceptors_destroyed":
        "Number of destroyed events recorded by reassigned interceptors.",
    "reassigned_missile_interceptor_efficiency":
        "Reassigned efficiency = reassigned-interceptor hits / N_reassigned.",
    "num_missile_interceptor_hits_on_non_original_targets":
        "Hit events whose threat differs from the interceptor's original target set.",
}


def _safe_rate(numerator: int | float, denominator: int | float) -> float:
    """Returns a rate, or NaN when its denominator is zero."""
    if denominator == 0:
        return np.nan
    return float(numerator) / float(denominator)


def _count_rows(df: pd.DataFrame,
                event: str,
                agent_type: str | None = None) -> int:
    """Counts event rows, optionally restricted to one agent type."""
    mask = df[EVENT] == event
    if agent_type is not None:
        mask &= df[AGENT_TYPE] == agent_type
    return int(mask.sum())


def _unique_agent_ids(df: pd.DataFrame, events: Iterable[str]) -> set[str]:
    """Returns agent IDs associated with any of the requested events."""
    return set(df.loc[df[EVENT].isin(events), AGENT_ID].dropna().astype(str))


def _threat_destruction_times(df: pd.DataFrame) -> dict[str, float]:
    """Calculates per-threat and whole-engagement destruction timing."""
    spawned = df[df[EVENT] == NEW_THREAT].copy()
    destroyed = df[df[EVENT] == THREAT_DESTROYED].copy()
    if spawned.empty or destroyed.empty:
        return {
            "num_threats_with_destruction_time": 0,
            "mean_threat_spawn_to_destroyed_time_s": np.nan,
            "last_threat_spawn_to_destroyed_time_s": np.nan,
            "last_threat_destroyed_time_s": np.nan,
            "time_to_destroy_all_threats_s": np.nan,
        }

    spawned[TIME] = pd.to_numeric(spawned[TIME], errors="coerce")
    destroyed[TIME] = pd.to_numeric(destroyed[TIME], errors="coerce")
    spawn_times = spawned.groupby(AGENT_ID)[TIME].min()
    destruction_times = destroyed.groupby(AGENT_ID)[TIME].min()
    paired = pd.concat(
        [spawn_times.rename("spawn"),
         destruction_times.rename("destroyed")],
        axis=1,
        join="inner",
    ).dropna()
    durations = paired["destroyed"] - paired["spawn"]
    durations = durations[durations >= 0]
    last_destroyed_time = float(destruction_times.max())
    all_destroyed = set(spawn_times.index).issubset(destruction_times.index)
    return {
        "num_threats_with_destruction_time":
            int(durations.size),
        "mean_threat_spawn_to_destroyed_time_s":
            (float(durations.mean()) if not durations.empty else np.nan),
        "last_threat_spawn_to_destroyed_time_s":
            (float(durations.max()) if not durations.empty else np.nan),
        "last_threat_destroyed_time_s":
            last_destroyed_time,
        "time_to_destroy_all_threats_s":
            (last_destroyed_time -
             float(spawn_times.min()) if all_destroyed else np.nan),
    }


def _time_to_resolved_fraction(
    resolution_durations: np.ndarray,
    num_threats: int,
    fraction: float,
) -> float:
    """Returns elapsed time until a requested fraction of threats resolves."""
    required = int(np.ceil(num_threats * fraction))
    if required <= 0 or resolution_durations.size < required:
        return np.nan
    ordered = np.sort(resolution_durations)
    return float(ordered[required - 1])


def _threat_resolution_metrics(df: pd.DataFrame) -> dict[str, float]:
    """Calculates clearance timing and threat exposure from existing events."""
    spawned = df[df[EVENT] == NEW_THREAT].copy()
    outcomes = df[df[EVENT].isin([THREAT_HIT, THREAT_DESTROYED])].copy()
    unavailable = {
        "time_to_first_threat_resolution_s": np.nan,
        "time_to_50_percent_threats_resolved_s": np.nan,
        "time_to_90_percent_threats_resolved_s": np.nan,
        "time_to_100_percent_threats_resolved_s": np.nan,
        "threat_exposure_time_s": np.nan,
        "mean_threat_exposure_time_s": np.nan,
    }
    if spawned.empty:
        return unavailable

    spawned[TIME] = pd.to_numeric(spawned[TIME], errors="coerce")
    outcomes[TIME] = pd.to_numeric(outcomes[TIME], errors="coerce")
    spawn_times = spawned.groupby(AGENT_ID)[TIME].min().dropna()
    outcome_times = outcomes.groupby(AGENT_ID)[TIME].min().dropna()
    if spawn_times.empty:
        return unavailable

    paired = pd.concat(
        [spawn_times.rename("spawn"),
         outcome_times.rename("outcome")],
        axis=1,
        join="inner",
    ).dropna()
    durations = (paired["outcome"] - paired["spawn"])
    durations = durations[durations >= 0].to_numpy(dtype=float)
    num_threats = int(spawn_times.size)

    last_event_time = pd.to_numeric(df[TIME], errors="coerce").max()
    exposure_durations: list[float] = []
    if np.isfinite(last_event_time):
        for threat_id, spawn_time in spawn_times.items():
            outcome_time = outcome_times.get(threat_id, np.nan)
            end_time = (float(outcome_time) if np.isfinite(outcome_time) else
                        float(last_event_time))
            exposure_durations.append(max(0.0, end_time - float(spawn_time)))
    exposure = (float(np.sum(exposure_durations))
                if exposure_durations else np.nan)

    return {
        "time_to_first_threat_resolution_s":
            (float(np.min(durations)) if durations.size else np.nan),
        "time_to_50_percent_threats_resolved_s":
            _time_to_resolved_fraction(durations, num_threats, 0.50),
        "time_to_90_percent_threats_resolved_s":
            _time_to_resolved_fraction(durations, num_threats, 0.90),
        "time_to_100_percent_threats_resolved_s":
            _time_to_resolved_fraction(durations, num_threats, 1.00),
        "threat_exposure_time_s":
            exposure,
        "mean_threat_exposure_time_s":
            _safe_rate(exposure, num_threats),
    }


def _first_rows_by_agent(frame: pd.DataFrame) -> pd.DataFrame:
    """Returns the earliest event row for each non-null agent ID."""
    if frame.empty:
        return frame
    ordered = frame.dropna(subset=[AGENT_ID]).sort_values(TIME, kind="stable")
    return ordered.drop_duplicates(AGENT_ID, keep="first").set_index(AGENT_ID)


def _peak_active_interceptors(
    spawned_rows: pd.DataFrame,
    hit_rows: pd.DataFrame,
    destroyed_rows: pd.DataFrame,
) -> int:
    """Calculates peak concurrently active missiles from lifecycle events."""
    if spawned_rows.empty:
        return 0
    terminal = pd.concat([hit_rows, destroyed_rows], ignore_index=True)
    terminal = _first_rows_by_agent(terminal)
    changes: list[tuple[float, int]] = []
    for time_value in spawned_rows[TIME]:
        if np.isfinite(time_value):
            changes.append((float(time_value), 1))
    for time_value in terminal[TIME] if not terminal.empty else []:
        if np.isfinite(time_value):
            changes.append((float(time_value), -1))
    active = 0
    peak = 0
    # At equal timestamps, launches are processed before terminal events.
    for _, delta in sorted(changes, key=lambda value: (value[0], -value[1])):
        active = max(0, active + delta)
        peak = max(peak, active)
    return peak


def _missile_interceptor_lifecycle_metrics(
        df: pd.DataFrame) -> dict[str, float]:
    """Calculates unique-interceptor timing, outcomes, and miss recovery."""
    missile_events = df[df[AGENT_TYPE] == MISSILE_INTERCEPTOR].copy()
    missile_events[TIME] = pd.to_numeric(missile_events[TIME], errors="coerce")
    spawned = _first_rows_by_agent(
        missile_events[missile_events[EVENT] == NEW_INTERCEPTOR])
    hits = _first_rows_by_agent(
        missile_events[missile_events[EVENT] == INTERCEPTOR_HIT])
    misses = _first_rows_by_agent(
        missile_events[missile_events[EVENT] == INTERCEPTOR_MISS])
    destroyed = _first_rows_by_agent(
        missile_events[missile_events[EVENT] == INTERCEPTOR_DESTROYED])

    spawned_ids = set(spawned.index.astype(str)) if not spawned.empty else set()
    hit_ids = set(hits.index.astype(str)).intersection(spawned_ids)
    destroyed_ids = set(destroyed.index.astype(str)).intersection(spawned_ids)
    miss_ids = set(misses.index.astype(str)).intersection(spawned_ids)
    missed_then_hit_ids = miss_ids.intersection(hit_ids)
    terminal_ids = hit_ids.union(destroyed_ids)
    no_terminal_ids = spawned_ids.difference(terminal_ids)

    flight_times: list[float] = []
    displacements: list[float] = []
    speed_proxies: list[float] = []
    for interceptor_id in hit_ids:
        spawn_time = float(spawned.loc[interceptor_id, TIME])
        hit_time = float(hits.loc[interceptor_id, TIME])
        flight_time = np.nan
        if np.isfinite(spawn_time) and np.isfinite(
                hit_time) and hit_time >= spawn_time:
            flight_time = hit_time - spawn_time
            flight_times.append(flight_time)
        spawn_position = pd.to_numeric(spawned.loc[interceptor_id,
                                                   POSITION_COLUMNS],
                                       errors="coerce").to_numpy(dtype=float)
        hit_position = pd.to_numeric(hits.loc[interceptor_id, POSITION_COLUMNS],
                                     errors="coerce").to_numpy(dtype=float)
        if np.isfinite(spawn_position).all() and np.isfinite(
                hit_position).all():
            displacement = float(np.linalg.norm(hit_position - spawn_position))
            displacements.append(displacement)
            if np.isfinite(flight_time) and flight_time > 0:
                speed_proxies.append(displacement / flight_time)

    flight_values = np.asarray(flight_times, dtype=float)
    displacement_values = np.asarray(displacements, dtype=float)
    speed_proxy_values = np.asarray(speed_proxies, dtype=float)
    num_spawned = len(spawned_ids)
    first_threat_spawn = pd.to_numeric(df.loc[df[EVENT] == NEW_THREAT, TIME],
                                       errors="coerce").min()

    def elapsed_from_first_threat(frame: pd.DataFrame) -> float:
        if frame.empty or not np.isfinite(first_threat_spawn):
            return np.nan
        first_event = pd.to_numeric(frame[TIME], errors="coerce").min()
        if not np.isfinite(first_event):
            return np.nan
        return float(first_event - first_threat_spawn)

    return {
        "mean_missile_interceptor_flight_time_s":
            (float(np.mean(flight_values)) if flight_values.size else np.nan),
        "median_missile_interceptor_flight_time_s":
            (float(np.median(flight_values)) if flight_values.size else np.nan),
        "p95_missile_interceptor_flight_time_s":
            (float(np.quantile(flight_values, 0.95))
             if flight_values.size else np.nan),
        "mean_missile_interceptor_hit_displacement_m":
            (float(np.mean(displacement_values))
             if displacement_values.size else np.nan),
        "median_missile_interceptor_hit_displacement_m":
            (float(np.median(displacement_values))
             if displacement_values.size else np.nan),
        "p95_missile_interceptor_hit_displacement_m":
            (float(np.quantile(displacement_values, 0.95))
             if displacement_values.size else np.nan),
        "mean_missile_interceptor_hit_speed_proxy_mps":
            (float(np.mean(speed_proxy_values))
             if speed_proxy_values.size else np.nan),
        "peak_active_missile_interceptors":
            _peak_active_interceptors(spawned.reset_index(), hits.reset_index(),
                                      destroyed.reset_index()),
        "num_missile_interceptors_with_miss":
            len(miss_ids),
        "num_missile_interceptors_missed_then_hit":
            len(missed_then_hit_ids),
        "missile_interceptor_miss_recovery_rate":
            _safe_rate(len(missed_then_hit_ids), len(miss_ids)),
        "num_missile_interceptors_no_terminal_outcome":
            len(no_terminal_ids),
        "missile_interceptor_hit_fraction":
            _safe_rate(len(hit_ids), num_spawned),
        "missile_interceptor_destroyed_fraction":
            _safe_rate(len(destroyed_ids.difference(hit_ids)), num_spawned),
        "missile_interceptor_no_terminal_fraction":
            _safe_rate(len(no_terminal_ids), num_spawned),
        "time_to_first_missile_interceptor_launch_s":
            elapsed_from_first_threat(spawned),
        "time_to_first_missile_interceptor_hit_s":
            elapsed_from_first_threat(hits),
    }


def _coordination_metrics(df: pd.DataFrame) -> dict[str, float]:
    """Matches initial assignment requests to their delivered responses."""
    unavailable = {
        "num_coordination_requests": np.nan,
        "num_coordination_responses": np.nan,
        "coordination_response_rate": np.nan,
        "mean_coordination_response_time_s": np.nan,
        "median_coordination_response_time_s": np.nan,
        "p95_coordination_response_time_s": np.nan,
    }
    required = {MESSAGE_TYPE, MESSAGE_ID, CORRELATION_ID}
    if not required.issubset(df.columns):
        return unavailable

    message_ids = pd.to_numeric(df[MESSAGE_ID], errors="coerce")
    correlation_ids = pd.to_numeric(df[CORRELATION_ID], errors="coerce")
    initial_requests = df[(df[EVENT] == MESSAGE_SENT) &
                          (df[MESSAGE_TYPE] == ASSIGN_TARGET_REQUEST) &
                          message_ids.notna() & correlation_ids.notna() &
                          (message_ids == correlation_ids)].copy()
    delivered_responses = df[(df[EVENT] == MESSAGE_DELIVERED) &
                             (df[MESSAGE_TYPE] == ASSIGN_TARGET_RESPONSE) &
                             correlation_ids.notna()].copy()
    if initial_requests.empty:
        return unavailable

    initial_requests[CORRELATION_ID] = pd.to_numeric(
        initial_requests[CORRELATION_ID], errors="coerce")
    delivered_responses[CORRELATION_ID] = pd.to_numeric(
        delivered_responses[CORRELATION_ID], errors="coerce")
    request_times = initial_requests.groupby(CORRELATION_ID)[TIME].min()
    response_times = delivered_responses.groupby(CORRELATION_ID)[TIME].min()
    paired = pd.concat(
        [request_times.rename("request"),
         response_times.rename("response")],
        axis=1,
        join="inner",
    ).dropna()
    durations = paired["response"] - paired["request"]
    durations = durations[durations >= 0]
    num_requests = int(request_times.size)
    num_responses = int(durations.size)
    return {
        "num_coordination_requests":
            num_requests,
        "num_coordination_responses":
            num_responses,
        "coordination_response_rate":
            _safe_rate(num_responses, num_requests),
        "mean_coordination_response_time_s":
            (float(durations.mean()) if not durations.empty else np.nan),
        "median_coordination_response_time_s":
            (float(durations.median()) if not durations.empty else np.nan),
        "p95_coordination_response_time_s":
            (float(durations.quantile(0.95)) if not durations.empty else np.nan
            ),
    }


def _target_id_set(value: object) -> frozenset[str]:
    if pd.isna(value):
        return frozenset()
    return frozenset(part for part in str(value).split("|") if part)


def _reassignment_metrics(df: pd.DataFrame) -> dict[str, float]:
    """Calculates outcomes for interceptors assigned to a new target set."""
    unavailable = {
        "num_reassigned_missile_interceptors": np.nan,
        "num_reassigned_missile_interceptor_hits": np.nan,
        "num_reassigned_missile_interceptor_misses": np.nan,
        "num_reassigned_missile_interceptors_destroyed": np.nan,
        "reassigned_missile_interceptor_efficiency": np.nan,
        "num_missile_interceptor_hits_on_non_original_targets": np.nan,
    }
    if TARGET_IDS not in df.columns:
        return unavailable
    assignments = df[(df[EVENT] == INTERCEPTOR_TARGET_ASSIGNED) &
                     (df[AGENT_TYPE] == MISSILE_INTERCEPTOR)].copy()
    if assignments.empty:
        return unavailable

    assignments = assignments.sort_values(TIME, kind="stable")
    original_targets: dict[str, frozenset[str]] = {}
    reassigned_ids: set[str] = set()
    for row in assignments[[AGENT_ID, TARGET_IDS]].itertuples(index=False,
                                                              name=None):
        interceptor_id, target_ids_text = str(row[0]), row[1]
        target_ids = _target_id_set(target_ids_text)
        if not target_ids:
            continue
        if interceptor_id not in original_targets:
            original_targets[interceptor_id] = target_ids
        elif target_ids != original_targets[interceptor_id]:
            reassigned_ids.add(interceptor_id)

    outcomes = df[(df[AGENT_TYPE] == MISSILE_INTERCEPTOR) &
                  (df[AGENT_ID].astype(str).isin(reassigned_ids))]
    num_reassigned = len(reassigned_ids)
    num_hits = _count_rows(outcomes, INTERCEPTOR_HIT)
    num_misses = _count_rows(outcomes, INTERCEPTOR_MISS)
    num_destroyed = _count_rows(outcomes, INTERCEPTOR_DESTROYED)

    hit_on_non_original_count: float = np.nan
    if RELATED_AGENT_ID in df.columns:
        hit_on_non_original_count = 0
        hit_events = df[(df[EVENT] == INTERCEPTOR_HIT) &
                        (df[AGENT_TYPE] == MISSILE_INTERCEPTOR)]
        for interceptor_id, threat_id in hit_events[[
                AGENT_ID, RELATED_AGENT_ID
        ]].itertuples(index=False, name=None):
            original = original_targets.get(str(interceptor_id))
            if original and pd.notna(threat_id) and str(
                    threat_id) not in original:
                hit_on_non_original_count += 1

    return {
        "num_reassigned_missile_interceptors":
            num_reassigned,
        "num_reassigned_missile_interceptor_hits":
            num_hits,
        "num_reassigned_missile_interceptor_misses":
            num_misses,
        "num_reassigned_missile_interceptors_destroyed":
            num_destroyed,
        "reassigned_missile_interceptor_efficiency":
            _safe_rate(num_hits, num_reassigned),
        "num_missile_interceptor_hits_on_non_original_targets":
            hit_on_non_original_count,
    }


def summarize_event_log(event_df: pd.DataFrame) -> dict[str, int | float]:
    """Calculates one set of performance metrics for one simulation run.

    Args:
        event_df: Dataframe containing the simulation event log.

    Returns:
        Mapping from metric names to scalar values.
    """
    required_columns = {EVENT, AGENT_TYPE, AGENT_ID, TIME, *POSITION_COLUMNS}
    missing_columns = required_columns.difference(event_df.columns)
    if missing_columns:
        raise ValueError(
            f"Event log is missing columns: {sorted(missing_columns)}")

    df = event_df.copy()
    df[EVENT] = df[EVENT].astype(str).str.upper().str.strip()

    spawned_threat_ids = _unique_agent_ids(df[df[EVENT] == NEW_THREAT],
                                           [NEW_THREAT])
    resolved_threat_ids = _unique_agent_ids(df, [THREAT_HIT, THREAT_DESTROYED])

    num_threats = len(spawned_threat_ids)
    num_threat_hits = _count_rows(df, THREAT_HIT)
    num_threats_destroyed = _count_rows(df, THREAT_DESTROYED)
    num_unresolved_threats = len(spawned_threat_ids - resolved_threat_ids)

    num_missile_interceptors = _count_rows(df, NEW_INTERCEPTOR,
                                           MISSILE_INTERCEPTOR)
    num_interceptor_hits = _count_rows(df, INTERCEPTOR_HIT, MISSILE_INTERCEPTOR)
    num_interceptor_misses = _count_rows(df, INTERCEPTOR_MISS,
                                         MISSILE_INTERCEPTOR)
    num_interceptors_destroyed = _count_rows(df, INTERCEPTOR_DESTROYED,
                                             MISSILE_INTERCEPTOR)

    hit_denominator = num_interceptor_hits + num_interceptor_misses
    terminal_denominator = hit_denominator + num_interceptors_destroyed

    interceptor_hits = df[(df[EVENT] == INTERCEPTOR_HIT) &
                          (df[AGENT_TYPE] == MISSILE_INTERCEPTOR)]
    if interceptor_hits.empty:
        intercept_distances = np.array([], dtype=float)
    else:
        intercept_distances = np.linalg.norm(
            interceptor_hits[POSITION_COLUMNS].to_numpy(dtype=float), axis=1)

    threat_outcomes = df[df[EVENT].isin([THREAT_HIT, THREAT_DESTROYED])]
    last_threat_outcome_time = (float(threat_outcomes[TIME].max())
                                if not threat_outcomes.empty else np.nan)
    threat_destruction_times = _threat_destruction_times(df)
    threat_resolution_metrics = _threat_resolution_metrics(df)
    interceptor_lifecycle_metrics = _missile_interceptor_lifecycle_metrics(df)
    coordination_metrics = _coordination_metrics(df)
    reassignment_metrics = _reassignment_metrics(df)

    return {
        "num_threats":
            num_threats,
        "num_threat_hits":
            num_threat_hits,
        "num_threats_destroyed":
            num_threats_destroyed,
        "num_unresolved_threats":
            num_unresolved_threats,
        "threat_penetration_rate":
            _safe_rate(num_threat_hits, num_threats),
        "threat_destroyed_rate":
            _safe_rate(num_threats_destroyed, num_threats),
        "unresolved_threat_rate":
            _safe_rate(num_unresolved_threats, num_threats),
        "num_missile_interceptors":
            num_missile_interceptors,
        "num_missile_interceptor_hits":
            num_interceptor_hits,
        "num_missile_interceptor_misses":
            num_interceptor_misses,
        "num_missile_interceptors_destroyed":
            num_interceptors_destroyed,
        "interceptor_hit_rate":
            _safe_rate(num_interceptor_hits, hit_denominator),
        "interceptor_terminal_success_rate":
            _safe_rate(num_interceptor_hits, terminal_denominator),
        "interceptor_efficiency":
            _safe_rate(num_interceptor_hits, num_missile_interceptors),
        "missile_interceptors_per_hit":
            _safe_rate(num_missile_interceptors, num_interceptor_hits),
        "missile_interceptors_per_threat_destroyed":
            _safe_rate(num_missile_interceptors, num_threats_destroyed),
        "minimum_intercept_distance_m": (float(np.min(intercept_distances)) if
                                         intercept_distances.size else np.nan),
        "mean_intercept_distance_m": (float(np.mean(intercept_distances))
                                      if intercept_distances.size else np.nan),
        "median_intercept_distance_m": (float(np.median(intercept_distances)) if
                                        intercept_distances.size else np.nan),
        "last_threat_outcome_time_s":
            last_threat_outcome_time,
        "last_event_time_s":
            float(df[TIME].max()) if not df.empty else np.nan,
        **threat_destruction_times,
        **threat_resolution_metrics,
        **interceptor_lifecycle_metrics,
        **coordination_metrics,
        **reassignment_metrics,
    }
