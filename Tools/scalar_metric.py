"""A scalar metric outputs a single number from a single simulation run.

This file defines the scalar metric base class and all implementations thereof.
"""

from abc import abstractmethod

import numpy as np
import pandas as pd
from constants import Column, EventType
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


class NumInterceptors(ScalarMetric):
    """A metric for the number of interceptors spawned."""

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Number of interceptors"

    def emit(self, event_df: pd.DataFrame) -> int:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        return sum(event_df[Column.EVENT] == EventType.NEW_INTERCEPTOR)


class NumInterceptorHits(ScalarMetric):
    """A metric for the number of interceptor hits."""

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Number of interceptor hits"

    def emit(self, event_df: pd.DataFrame) -> int:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        return sum(event_df[Column.EVENT] == EventType.INTERCEPTOR_HIT)


class NumInterceptorMisses(ScalarMetric):
    """A metric for the number of interceptor misses."""

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Number of interceptor misses"

    def emit(self, event_df: pd.DataFrame) -> int:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        return sum(event_df[Column.EVENT] == EventType.INTERCEPTOR_MISS)


class InterceptorHitRate(ScalarMetric):
    """A metric for the hit rate of the interceptors."""

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Interceptor hit rate"

    def emit(self, event_df: pd.DataFrame) -> float:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        num_interceptor_hits = sum(
            event_df[Column.EVENT] == EventType.INTERCEPTOR_HIT)
        num_interceptor_misses = sum(
            event_df[Column.EVENT] == EventType.INTERCEPTOR_MISS)
        total = num_interceptor_hits + num_interceptor_misses
        if total == 0:
            return 0
        return num_interceptor_hits / total


class InterceptorEfficiency(ScalarMetric):
    """A metric for the interceptor efficiency.

    The interceptor efficiency is the average number of threats destroyed by a
    single interceptor.
    """

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Interceptor efficiency"

    def emit(self, event_df: pd.DataFrame) -> float:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        num_interceptors = sum(
            event_df[Column.EVENT] == EventType.NEW_INTERCEPTOR)
        num_interceptor_hits = sum(
            event_df[Column.EVENT] == EventType.INTERCEPTOR_HIT)
        if num_interceptors == 0:
            return 0
        return num_interceptor_hits / num_interceptors


class MinInterceptorDistance(ScalarMetric):
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
