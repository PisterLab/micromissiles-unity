#include "assignment/cp_assignment.h"

#include <vector>

#include "Plugin/status.pb.h"
#include "assignment/assignment.h"
#include "base/logging.h"
#include "ortools/sat/cp_model.h"
#include "ortools/sat/cp_model.pb.h"
#include "ortools/sat/cp_model_solver.h"
#include "ortools/sat/model.h"
#include "ortools/sat/sat_parameters.pb.h"

namespace assignment {

plugin::StatusCode CpAssignment::AssignImpl(
    std::vector<AssignmentItem>* assignments) const {
  assignments->clear();

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

  // Define the constraints.
  // Each agent is assigned to one task.
  for (int i = 0; i < num_agents_; ++i) {
    cp_model.AddExactlyOne(x[i]);
  }
  const auto constraints_status = DefineConstraints(x, &cp_model);
  if (constraints_status != plugin::STATUS_OK) {
    return constraints_status;
  }

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
  // Disabling the presolver and disabling parallelism are necessary to prevent
  // crashes and infinite loops. On Macs, checks for non-empty enforcement
  // literals would sometimes fail in the post-solver. Other times, a deadlock
  // over a mutex causes the simulation to hang.
  params.set_cp_model_presolve(false);
  params.set_num_workers(1);
  model.Add(operations_research::sat::NewSatParameters(params));

  // Solve the assignment problem.
  const operations_research::sat::CpSolverResponse response =
      operations_research::sat::SolveCpModel(cp_model.Build(), &model);

  // Check the feasibility of the solution.
  if (response.status() ==
      operations_research::sat::CpSolverStatus::INFEASIBLE) {
    LOG(ERROR) << "Assignment problem is infeasible.";
    return plugin::STATUS_INTERNAL;
  }

  // Record the assignments.
  assignments->reserve(num_agents_);
  for (int i = 0; i < num_agents_; ++i) {
    for (int j = 0; j < num_tasks_; ++j) {
      if (SolutionBooleanValue(response, x[i][j])) {
        assignments->emplace_back(i, j);
        break;
      }
    }
  }
  return plugin::STATUS_OK;
}

}  // namespace assignment
