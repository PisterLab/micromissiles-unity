// The cover assignment assigns one task to each agent under the condition that
// all tasks are assigned to at least one agent.

#pragma once

#include <vector>

#include "assignment/assignment.h"

namespace assignment {

// Cover assignment.
class CoverAssignment : public Assignment {
 public:
  CoverAssignment(const int num_agents, const int num_tasks,
                  std::vector<std::vector<double>> costs)
      : Assignment(num_agents, num_tasks, std::move(costs)) {}

  CoverAssignment(const CoverAssignment&) = default;
  CoverAssignment& operator=(const CoverAssignment&) = default;

  // Assign the agents to the tasks.
  std::vector<AssignmentItem> Assign() const override;
};

}  // namespace assignment
