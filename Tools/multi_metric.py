from abc import abstractmethod
from typing import Any

import numpy as np
import pandas as pd
from constants import EventType
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
        """
        hits = event_df[event_df["Event"] == EventType.INTERCEPTOR_HIT]
        hit_positions = hits[["PositionX", "PositionZ"]]
        return list(hit_positions.to_numpy())


class InterceptorSpawnPosition2D(MultiMetric):
    """A metric for the 2D interceptor spawn positions.

    The 2D position ignores the elevation.
    """

    @property
    def name(self) -> str:
        """Returns the name of the metric."""
        return "Interceptor spawn positions (2D)"

    def emit(self, event_df: pd.DataFrame) -> list[np.ndarray]:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
        new_interceptors = (
            event_df[event_df["Event"] == EventType.NEW_INTERCEPTOR])
        spawn_positions = new_interceptors[["PositionX", "PositionZ"]]
        return list(spawn_positions.to_numpy())
