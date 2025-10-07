// The constraint programming assignment class is an interface for cost-based
// assignments that formulate the assignment problem as a constraint programming
// problem. The assignment problem is solved using the CP-SAT solver provided by
// the Google OR-Tools library.

#pragma once

#include <vector>

#include "Plugin/status.pb.h"
#include "assignment/assignment.h"
#include "ortools/sat/cp_model.h"

namespace assignment {

// Constraint programming assignment interface.
class CpAssignment : public Assignment {
 public:
  CpAssignment(const int num_agents, const int num_tasks,
               std::vector<std::vector<double>> costs)
      : Assignment(num_agents, num_tasks, std::move(costs)) {}

  CpAssignment(const CpAssignment&) = default;
  CpAssignment& operator=(const CpAssignment&) = default;

  virtual ~CpAssignment() = default;

 protected:
  // Implementation of assigning the agents to the tasks and returning the
  // assignments.
  plugin::StatusCode AssignImpl(
      std::vector<AssignmentItem>* assignments) const override;

  // Define the constraints of the assignment problem.
  // The only constraint defined by this interface is that each agent is
  // assigned to one task. x is a 2D array of boolean variables, such that
  // x[i][j] is true if agent i is assigned to task j.
  virtual plugin::StatusCode DefineConstraints(
      const std::vector<std::vector<operations_research::sat::BoolVar>>& x,
      operations_research::sat::CpModelBuilder* cp_model) const = 0;
};

}  // namespace assignment
