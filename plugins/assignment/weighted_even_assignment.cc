#include "assignment/weighted_even_assignment.h"

#include <cstddef>
#include <cstdint>
#include <vector>

#include "Plugin/status.pb.h"
#include "base/logging.h"
#include "ortools/sat/cp_model.h"

namespace assignment {

plugin::StatusCode WeightedEvenAssignment::DefineConstraints(
    const std::vector<std::vector<operations_research::sat::BoolVar>>& x,
    operations_research::sat::CpModelBuilder* cp_model) const {
  // Validate the weights.
  const auto validate_status = ValidateWeights();
  if (validate_status != plugin::STATUS_OK) {
    return validate_status;
  }

  // Minimum and maximum number of assigned agents for a task.
  auto min_task_assignments = operations_research::sat::LinearExpr(
      cp_model->NewIntVar({0, num_agents_}));
  auto max_task_assignments = operations_research::sat::LinearExpr(
      cp_model->NewIntVar({0, num_agents_}));

  // Distribute the agents evenly among the tasks under the task weights.
  std::vector<operations_research::sat::LinearExpr> task_sums;
  task_sums.reserve(num_tasks_);
  for (int j = 0; j < num_tasks_; ++j) {
    std::vector<operations_research::sat::BoolVar> tasks;
    tasks.reserve(num_agents_);
    for (int i = 0; i < num_agents_; ++i) {
      tasks.emplace_back(x[i][j]);
    }
    const int64_t task_weight =
        static_cast<int64_t>(weights_[j] * weight_scaling_factor_);
    task_sums.emplace_back(operations_research::sat::LinearExpr::Sum(tasks) *
                           task_weight);
  }
  cp_model->AddMinEquality(min_task_assignments, task_sums);
  cp_model->AddMaxEquality(max_task_assignments, task_sums);
  cp_model->AddLessOrEqual(max_task_assignments - min_task_assignments,
                           weight_scaling_factor_);
  return plugin::STATUS_OK;
}

plugin::StatusCode WeightedEvenAssignment::ValidateWeights() const {
  // Validate the size of the weights.
  if (weights_.size() != static_cast<std::size_t>(num_tasks_)) {
    LOG(ERROR)
        << "The number of task weights does not match the number of tasks: "
        << weights_.size() << " vs. " << num_tasks_ << ".";
    return plugin::STATUS_INVALID_ARGUMENT;
  }
  return plugin::STATUS_OK;
}

}  // namespace assignment
