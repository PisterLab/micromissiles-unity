#include "assignment/cover_assignment.h"

#include <vector>

#include "ortools/sat/cp_model.h"

namespace assignment {

void CoverAssignment::DefineConstraints(
    const std::vector<std::vector<operations_research::sat::BoolVar>>& x,
    operations_research::sat::CpModelBuilder* cp_model) const {
  // If there are at least as many agents as tasks, each task is assigned to at
  // least one agent. Otherwise, no more than one agent should be assigned to
  // any task.
  if (num_agents_ >= num_tasks_) {
    for (int j = 0; j < num_tasks_; ++j) {
      std::vector<operations_research::sat::BoolVar> tasks;
      tasks.reserve(num_agents_);
      for (int i = 0; i < num_agents_; ++i) {
        tasks.emplace_back(x[i][j]);
      }
      cp_model->AddAtLeastOne(tasks);
    }
  } else {
    for (int j = 0; j < num_tasks_; ++j) {
      std::vector<operations_research::sat::BoolVar> tasks;
      tasks.reserve(num_agents_);
      for (int i = 0; i < num_agents_; ++i) {
        tasks.emplace_back(x[i][j]);
      }
      cp_model->AddAtMostOne(tasks);
    }
  }
}

}  // namespace assignment
