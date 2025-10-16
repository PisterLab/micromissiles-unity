// The cover assignment assigns one task to each agent under the condition that
// all tasks are assigned to at least one agent.

#pragma once

#include <vector>

#include "Plugin/status.pb.h"
#include "assignment/cp_assignment.h"
#include "ortools/sat/cp_model.h"

namespace assignment {

// Cover assignment.
class CoverAssignment : public CpAssignment {
 public:
  CoverAssignment(const int num_agents, const int num_tasks,
                  std::vector<std::vector<double>> costs)
      : CpAssignment(num_agents, num_tasks, std::move(costs)) {}

  CoverAssignment(const CoverAssignment&) = default;
  CoverAssignment& operator=(const CoverAssignment&) = default;

 protected:
  // Define the constraints of the assignment problem.
  plugin::StatusCode DefineConstraints(
      const std::vector<std::vector<operations_research::sat::BoolVar>>& x,
      operations_research::sat::CpModelBuilder* cp_model) const override;
};

}  // namespace assignment
