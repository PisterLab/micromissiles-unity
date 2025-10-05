#include "assignment/weighted_even_assignment.h"

#include <cstddef>
#include <cstdint>
#include <stdexcept>
#include <vector>

#include "absl/strings/str_format.h"
#include "assignment/assignment.h"
#include "base/logging.h"
#include "ortools/sat/cp_model.h"
#include "ortools/sat/cp_model.pb.h"
#include "ortools/sat/cp_model_solver.h"
#include "ortools/sat/model.h"
#include "ortools/sat/sat_parameters.pb.h"

namespace assignment {

std::vector<Assignment::AssignmentItem> WeightedEvenAssignment::Assign() const {
  // Create the constraint programming model.
  operations_research::sat::CpModelBuilder cp_model;

  // Define the variables.
  // x[i][j] is an array of boolean variables, such that x[i][j] is true if
  // agent i is assigned to task j.
  std::vector<std::vector<operations_research::sat::BoolVar>> x(
      num_agents_, std::vector<operations_research::sat::BoolVar>(num_tasks_));
  for (int i = 0; i < num_agents_; ++i) {
    for (int j = 0; j < num_tasks_; ++j) {
      x[i][j] = cp_model.NewBoolVar();
    }
  }
  // Minimum and maximum number of assigned agents for a task.
  auto min_task_assignments = operations_research::sat::LinearExpr(
      cp_model.NewIntVar({0, num_agents_}));
  auto max_task_assignments = operations_research::sat::LinearExpr(
      cp_model.NewIntVar({0, num_agents_}));

  // Define the constraints.
  // Each agent is assigned to one task.
  for (int i = 0; i < num_agents_; ++i) {
    cp_model.AddExactlyOne(x[i]);
  }
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
  cp_model.AddMinEquality(min_task_assignments, task_sums);
  cp_model.AddMaxEquality(max_task_assignments, task_sums);
  cp_model.AddLessOrEqual(max_task_assignments - min_task_assignments,
                          weight_scaling_factor_);

  // Define the objective function.
  operations_research::sat::DoubleLinearExpr total_cost;
  for (int i = 0; i < num_agents_; ++i) {
    for (int j = 0; j < num_tasks_; ++j) {
      total_cost.AddTerm(x[i][j], costs_[i][j]);
    }
  }
  cp_model.Minimize(total_cost);

  // Set the solver parameters.
  operations_research::sat::Model model;
  operations_research::sat::SatParameters params;
  params.set_log_search_progress(true);
  params.set_cp_model_presolve(false);
  params.set_num_workers(1);
  model.Add(operations_research::sat::NewSatParameters(params));

  // Solve the assignment problem.
  const operations_research::sat::CpSolverResponse response =
      operations_research::sat::SolveCpModel(cp_model.Build(), &model);

  // Check the feasibility of the solution.
  if (response.status() ==
      operations_research::sat::CpSolverStatus::INFEASIBLE) {
    LOG(ERROR) << "Weighted even assignment problem is infeasible.";
    return std::vector<AssignmentItem>();
  }

  // Record the assignments.
  std::vector<AssignmentItem> assignments;
  assignments.reserve(num_agents_);
  for (int i = 0; i < num_agents_; ++i) {
    for (int j = 0; j < num_tasks_; ++j) {
      if (SolutionBooleanValue(response, x[i][j])) {
        assignments.emplace_back(i, j);
        break;
      }
    }
  }
  return assignments;
}

void WeightedEvenAssignment::ValidateWeights() const {
  // Validate the size of the weights.
  if (weights_.size() != static_cast<std::size_t>(num_tasks_)) {
    throw std::invalid_argument(
        absl::StrFormat("The number of task weights does not match the number "
                        "of tasks: %d vs. %d.",
                        weights_.size(), num_tasks_));
  }
}

}  // namespace assignment
