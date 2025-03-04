// The weighted even assignment assigns one task to each agent while trying to
// evenly distribute the agents among the tasks under the given task weights.
// Since the constraints have to be integral and linear, the task weights are
// scaled by the weight scalinng factor and converted to integers.

#pragma once

#include <cstdint>
#include <vector>

#include "assignment/assignment.h"

namespace assignment {

// Weighted even assignment.
class WeightedEvenAssignment : public Assignment {
 public:
  WeightedEvenAssignment(const int num_agents, const int num_tasks,
                         std::vector<std::vector<double>> costs,
                         std::vector<double> weights,
                         const int64_t weight_scaling_factor)
      : Assignment(num_agents, num_tasks, std::move(costs)),
        weights_(std::move(weights)),
        weight_scaling_factor_(weight_scaling_factor) {
    ValidateWeights();
  }

  WeightedEvenAssignment(const WeightedEvenAssignment&) = default;
  WeightedEvenAssignment& operator=(const WeightedEvenAssignment&) = default;

  // Assign the agents to the tasks.
  std::vector<AssignmentItem> Assign() const override;

 private:
  // Validate the task weights.
  void ValidateWeights() const;

  // Task weights.
  std::vector<double> weights_;

  // Task weight scaling factor.
  int64_t weight_scaling_factor_ = 0;
};

}  // namespace assignment
