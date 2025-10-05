#include <cstdlib>
#include <vector>

#include "base/base.h"
#include "base/logging.h"
#include "ortools/sat/cp_model.h"
#include "ortools/sat/cp_model.pb.h"
#include "ortools/sat/cp_model_solver.h"

int main(int argc, char** argv) {
  base::Init(argc, argv);

  // Data.
  const std::vector<std::vector<int>> costs{
      {90, 80, 75, 70},   {35, 85, 55, 65},   {125, 95, 90, 95},
      {45, 110, 95, 115}, {50, 100, 90, 100},
  };
  const auto num_workers = costs.size();
  const auto num_tasks = costs[0].size();

  // Model.
  operations_research::sat::CpModelBuilder cp_model;

  // Variables.
  // x[i][j] is an array of boolean variables, such that x[i][j] is true if
  // worker i is assigned to task j.
  std::vector<std::vector<operations_research::sat::BoolVar>> x(
      num_workers, std::vector<operations_research::sat::BoolVar>(num_tasks));
  for (int i = 0; i < num_workers; ++i) {
    for (int j = 0; j < num_tasks; ++j) {
      x[i][j] = cp_model.NewBoolVar();
    }
  }

  // Constraints.
  // Each worker is assigned to at most one task.
  for (int i = 0; i < num_workers; ++i) {
    cp_model.AddAtMostOne(x[i]);
  }
  // Each task is assigned to exactly one worker.
  for (int j = 0; j < num_tasks; ++j) {
    std::vector<operations_research::sat::BoolVar> tasks;
    for (int i = 0; i < num_workers; ++i) {
      tasks.push_back(x[i][j]);
    }
    cp_model.AddExactlyOne(tasks);
  }

  // Objective.
  operations_research::sat::LinearExpr total_cost;
  for (int i = 0; i < num_workers; ++i) {
    for (int j = 0; j < num_tasks; ++j) {
      total_cost += x[i][j] * costs[i][j];
    }
  }
  cp_model.Minimize(total_cost);

  // Solve.
  const operations_research::sat::CpSolverResponse response =
      operations_research::sat::Solve(cp_model.Build());

  // Print solution.
  if (response.status() ==
      operations_research::sat::CpSolverStatus::INFEASIBLE) {
    LOG(FATAL) << "No solution found.";
  }

  LOG(INFO) << "Total cost: " << response.objective_value() << ".";
  for (int i = 0; i < num_workers; ++i) {
    for (int j = 0; j < num_tasks; ++j) {
      if (SolutionBooleanValue(response, x[i][j])) {
        LOG(INFO) << "Task " << i << " assigned to worker " << j
                  << ".  Cost: " << costs[i][j] << ".";
      }
    }
  }

  return EXIT_SUCCESS;
}
