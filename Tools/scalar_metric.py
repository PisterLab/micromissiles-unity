"""A scalar metric outputs a single number from a single simulation run.

This file defines the scalar metric base class and all implementations thereof.
"""

from abc import abstractmethod

import numpy as np
import pandas as pd
from constants import EventType
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
        return sum(event_df["Event"] == EventType.NEW_INTERCEPTOR)


class NumHits(ScalarMetric):
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
        return sum(event_df["Event"] == EventType.INTERCEPTOR_HIT)


class NumMisses(ScalarMetric):
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
        return sum(event_df["Event"] == EventType.INTERCEPTOR_MISS)


class HitRate(ScalarMetric):
    """A metric for the hit rate of the interceptors."""

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Hit rate"

    def emit(self, event_df: pd.DataFrame) -> float:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        num_hits = sum(event_df["Event"] == EventType.INTERCEPTOR_HIT)
        num_misses = sum(event_df["Event"] == EventType.INTERCEPTOR_MISS)
        total = num_hits + num_misses
        if total == 0:
            return 0
        return num_hits / total


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
        num_interceptors = sum(event_df["Event"] == EventType.NEW_INTERCEPTOR)
        num_hits = sum(event_df["Event"] == EventType.INTERCEPTOR_HIT)
        if num_interceptors == 0:
            return 0
        return num_hits / num_interceptors


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
        hits = event_df[event_df["Event"] == EventType.INTERCEPTOR_HIT]
        if hits.empty:
            return np.inf
        hit_positions = hits[["PositionX", "PositionY", "PositionZ"]]
        hit_distances = np.linalg.norm(hit_positions.to_numpy(), axis=1)
        return np.min(hit_distances)
