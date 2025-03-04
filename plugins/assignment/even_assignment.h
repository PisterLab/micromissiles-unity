// The even assignment assigns one task to each agent while trying to evenly
// distribute the agents among the tasks.

#pragma once

#include <vector>

#include "assignment/assignment.h"

namespace assignment {

// Even assignment.
class EvenAssignment : public Assignment {
 public:
  EvenAssignment(const int num_agents, const int num_tasks,
                 std::vector<std::vector<double>> costs)
      : Assignment(num_agents, num_tasks, std::move(costs)) {}

  EvenAssignment(const EvenAssignment&) = default;
  EvenAssignment& operator=(const EvenAssignment&) = default;

  // Assign the agents to the tasks.
  std::vector<AssignmentItem> Assign() const override;
};

}  // namespace assignment
