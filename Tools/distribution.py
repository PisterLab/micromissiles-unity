import numpy as np
import pandas as pd
from aggregator import Aggregator
from scalar_metric import ScalarMetric


class Distribution(Aggregator):
    """A distribution analyzes the statistics of scalar metric values."""

    def __init__(
        self,
        event_dfs: list[pd.DataFrame],
        metric: ScalarMetric,
    ):
        super().__init__(event_dfs, metric)

    def min(self) -> float:
        """Returns the minimum of the metric values."""
        if not self.values:
            raise ValueError("The list of metric values is empty.")
        return min(self.values)

    def max(self) -> float:
        """Returns the maximum of the metric values."""
        if not self.values:
            raise ValueError("The list of metric values is empty.")
        return max(self.values)

    def mean(self) -> float:
        """Returns the mean of the metric values."""
        return np.mean(self.values)

    def median(self) -> float:
        """Returns the median of the metric values."""
        return np.median(self.values)

    def std(self) -> float:
        """Returns the standard deviation of the metric values."""
        return np.std(self.values)
