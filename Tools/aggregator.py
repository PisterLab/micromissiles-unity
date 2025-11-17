import pandas as pd
from metric import Metric
from multi_metric import MultiMetric
from scalar_metric import ScalarMetric


class Aggregator:
    """An aggregator combines metrics over multiple simulation runs.

    Attributes:
        values: Aggregated metric values.
    """

    def __init__(
        self,
        event_dfs: list[pd.DataFrame],
        metric: Metric,
    ):
        if isinstance(metric, ScalarMetric):
            self.values = [metric.emit(event_df) for event_df in event_dfs]
        elif isinstance(metric, MultiMetric):
            self.values = [
                value for event_df in event_dfs
                for value in metric.emit(event_df)
            ]
