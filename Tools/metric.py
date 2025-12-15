"""Metrics extract specific values from the simulation event logs."""

from abc import ABC, abstractmethod
from typing import Any

import pandas as pd


class Metric(ABC):
    """A metric represents an output from a single simulation run."""

    @property
    @abstractmethod
    def name(self) -> str:
        """Returns the name of the metric."""

    @abstractmethod
    def emit(self, event_df: pd.DataFrame) -> Any:
        """Emits the metric from the given event log.

        Args:
            event_df: Dataframe containing the events.
        """
