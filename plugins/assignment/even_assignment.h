// The even assignment assigns one task to each agent while trying to evenly
// distribute the agents among the tasks.

#pragma once

#include <vector>

#include "Plugin/status.pb.h"
#include "assignment/cp_assignment.h"
#include "ortools/sat/cp_model.h"

namespace assignment {

// Even assignment.
class EvenAssignment : public CpAssignment {
 public:
  EvenAssignment(const int num_agents, const int num_tasks,
                 std::vector<std::vector<double>> costs)
      : CpAssignment(num_agents, num_tasks, std::move(costs)) {}

  EvenAssignment(const EvenAssignment&) = default;
  EvenAssignment& operator=(const EvenAssignment&) = default;

 protected:
  // Define the constraints of the assignment problem.
  plugin::StatusCode DefineConstraints(
      const std::vector<std::vector<operations_research::sat::BoolVar>>& x,
      operations_research::sat::CpModelBuilder* cp_model) const override;
};

}  // namespace assignment
