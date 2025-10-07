// The weighted even assignment assigns one task to each agent while trying to
// evenly distribute the agents among the tasks under the given task weights.
// Since the constraints have to be integral and linear, the task weights are
// scaled by the weight scalinng factor and converted to integers.

#pragma once

#include <cstdint>
#include <vector>

#include "Plugin/status.pb.h"
#include "assignment/cp_assignment.h"
#include "ortools/sat/cp_model.h"

namespace assignment {

// Weighted even assignment.
class WeightedEvenAssignment : public CpAssignment {
 public:
  WeightedEvenAssignment(const int num_agents, const int num_tasks,
                         std::vector<std::vector<double>> costs,
                         std::vector<double> weights,
                         const int64_t weight_scaling_factor)
      : CpAssignment(num_agents, num_tasks, std::move(costs)),
        weights_(std::move(weights)),
        weight_scaling_factor_(weight_scaling_factor) {}

  WeightedEvenAssignment(const WeightedEvenAssignment&) = default;
  WeightedEvenAssignment& operator=(const WeightedEvenAssignment&) = default;

 protected:
  // Define the constraints of the assignment problem.
  plugin::StatusCode DefineConstraints(
      const std::vector<std::vector<operations_research::sat::BoolVar>>& x,
      operations_research::sat::CpModelBuilder* cp_model) const override;

 private:
  // Validate the task weights.
  plugin::StatusCode ValidateWeights() const;

  // Task weights.
  std::vector<double> weights_;

  // Task weight scaling factor.
  int64_t weight_scaling_factor_ = 0;
};

}  // namespace assignment
