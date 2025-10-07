#include "assignment/even_assignment.h"

#include <vector>

#include "Plugin/status.pb.h"
#include "ortools/sat/cp_model.h"

namespace assignment {

plugin::StatusCode EvenAssignment::DefineConstraints(
    const std::vector<std::vector<operations_research::sat::BoolVar>>& x,
    operations_research::sat::CpModelBuilder* cp_model) const {
  // Minimum and maximum number of assigned agents for a task.
  auto min_task_assignments = operations_research::sat::LinearExpr(
      cp_model->NewIntVar({0, num_agents_}));
  auto max_task_assignments = operations_research::sat::LinearExpr(
      cp_model->NewIntVar({0, num_agents_}));

  // Distribute the agents evenly among the tasks.
  std::vector<operations_research::sat::LinearExpr> task_sums;
  task_sums.reserve(num_tasks_);
  for (int j = 0; j < num_tasks_; ++j) {
    std::vector<operations_research::sat::BoolVar> tasks;
    tasks.reserve(num_agents_);
    for (int i = 0; i < num_agents_; ++i) {
      tasks.emplace_back(x[i][j]);
    }
    task_sums.emplace_back(operations_research::sat::LinearExpr::Sum(tasks));
  }
  cp_model->AddMinEquality(min_task_assignments, task_sums);
  cp_model->AddMaxEquality(max_task_assignments, task_sums);
  cp_model->AddLessOrEqual(max_task_assignments - min_task_assignments, 1);
  return plugin::STATUS_OK;
}

}  // namespace assignment
