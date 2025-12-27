"""A scalar metric outputs a single number from a single simulation run.

This file defines the scalar metric base class and all implementations thereof.
"""

from abc import abstractmethod

import numpy as np
import pandas as pd
from constants import AgentType, Column, EventType
from metric import Metric


class ScalarMetric(Metric):
    """A scalar metric represents a single scalar output from a single
    simulation run.
    """

    @abstractmethod
    def emit(self, event_df: pd.DataFrame) -> int | float:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """


class NumMissileInterceptors(ScalarMetric):
    """A metric for the number of missile interceptors spawned."""

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Number of missile interceptors"

    def emit(self, event_df: pd.DataFrame) -> int:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        return sum(
            (event_df[Column.AGENT_TYPE] == AgentType.MISSILE_INTERCEPTOR) &
            (event_df[Column.EVENT] == EventType.NEW_INTERCEPTOR))


class NumMissileInterceptorHits(ScalarMetric):
    """A metric for the number of missile interceptor hits."""

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Number of missile interceptor hits"

    def emit(self, event_df: pd.DataFrame) -> int:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        return sum(
            (event_df[Column.AGENT_TYPE] == AgentType.MISSILE_INTERCEPTOR) &
            (event_df[Column.EVENT] == EventType.INTERCEPTOR_HIT))


class NumMissileInterceptorMisses(ScalarMetric):
    """A metric for the number of missile interceptor misses."""

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Number of missile interceptor misses"

    def emit(self, event_df: pd.DataFrame) -> int:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        return sum(
            (event_df[Column.AGENT_TYPE] == AgentType.MISSILE_INTERCEPTOR) &
            (event_df[Column.EVENT] == EventType.INTERCEPTOR_MISS))


class MissileInterceptorHitRate(ScalarMetric):
    """A metric for the hit rate of the missile interceptors."""

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Missile interceptor hit rate"

    def emit(self, event_df: pd.DataFrame) -> float:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        num_missile_interceptor_hits = (
            NumMissileInterceptorHits().emit(event_df))
        num_missile_interceptor_misses = (
            NumMissileInterceptorMisses().emit(event_df))
        total = num_missile_interceptor_hits + num_missile_interceptor_misses
        if total == 0:
            return 0
        return num_missile_interceptor_hits / total


class MissileInterceptorEfficiency(ScalarMetric):
    """A metric for the missile interceptor efficiency.

    The missile interceptor efficiency is the average number of threats destroyed by a
    single missile interceptor.
    """

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Missile interceptor efficiency"

    def emit(self, event_df: pd.DataFrame) -> float:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        num_missile_interceptors = NumMissileInterceptors().emit(event_df)
        num_missile_interceptor_hits = (
            NumMissileInterceptorHits().emit(event_df))
        if num_missile_interceptors == 0:
            return 0
        return num_missile_interceptor_hits / num_missile_interceptors


class MinInterceptDistance(ScalarMetric):
    """A metric for the minimum intercept distance."""

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Minimum intercept distance"

    def emit(self, event_df: pd.DataFrame) -> float:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        interceptor_hits = (
            event_df[event_df[Column.EVENT] == EventType.INTERCEPTOR_HIT])
        if interceptor_hits.empty:
            return np.inf
        interceptor_hit_positions = interceptor_hits[[
            Column.POSITION_X,
            Column.POSITION_Y,
            Column.POSITION_Z,
        ]]
        interceptor_hit_distances = np.linalg.norm(
            interceptor_hit_positions.to_numpy(), axis=1)
        return np.min(interceptor_hit_distances)
