"""A multi-metric outputs a list of values from a single simulation run.

This file defines the multi-metric base class and all implementations thereof.
"""

from abc import abstractmethod
from typing import Any

import numpy as np
import pandas as pd
from constants import AgentType, Column, EventType
from metric import Metric


class MultiMetric(Metric):
    """A multi-metric represents multiple outputs from a single simulation run."""

    @abstractmethod
    def emit(self, event_df: pd.DataFrame) -> list[Any]:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """


class InterceptPosition2D(MultiMetric):
    """A metric for the 2D intercept positions.

    The 2D position ignores the elevation.
    """

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Intercept positions (2D)"

    def emit(self, event_df: pd.DataFrame) -> list[np.ndarray]:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.

        Returns:
            An array of shape (num_hits, 2) containing the 2D positions (x, z).
        """
        interceptor_hits = (
            event_df[event_df[Column.EVENT] == EventType.INTERCEPTOR_HIT])
        interceptor_hit_positions = interceptor_hits[[
            Column.POSITION_X,
            Column.POSITION_Z,
        ]]
        return list(interceptor_hit_positions.to_numpy())


class MissileInterceptorSpawnPosition2D(MultiMetric):
    """A metric for the 2D missile interceptor spawn positions.

    The 2D position ignores the elevation.
    """

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Missile interceptor spawn positions (2D)"

    def emit(self, event_df: pd.DataFrame) -> list[np.ndarray]:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.

        Returns:
            An array of shape (num_interceptors, 2) containing the 2D positions
            (x, z).
        """
        new_missile_interceptors = event_df[
            (event_df[Column.AGENT_TYPE] == AgentType.MISSILE_INTERCEPTOR) &
            (event_df[Column.EVENT] == EventType.NEW_INTERCEPTOR)]
        missile_interceptor_spawn_positions = new_missile_interceptors[[
            Column.POSITION_X,
            Column.POSITION_Z,
        ]]
        return list(missile_interceptor_spawn_positions.to_numpy())
