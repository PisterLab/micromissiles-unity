// The assignment class is an interface for a cost-based assignment problem.

#pragma once

#include <utility>
#include <vector>

#include "Plugin/status.pb.h"

namespace assignment {

// Assignment interface.
class Assignment {
 public:
  // Assignment item type.
  using AssignmentItem = std::pair<int, int>;

  // The assignment cost matrix should be a matrix of dimensions num_agents x
  // num_tasks.
  Assignment(const int num_agents, const int num_tasks,
             std::vector<std::vector<double>> costs)
      : num_agents_(num_agents),
        num_tasks_(num_tasks),
        costs_(std::move(costs)) {}

  Assignment(const Assignment&) = default;
  Assignment& operator=(const Assignment&) = default;

  virtual ~Assignment() = default;

  // Assign the agents to the tasks and return the assignments.
  plugin::StatusCode Assign(std::vector<AssignmentItem>* assignments) const;

 protected:
  // Implementation of assigning the agents to the tasks and returning the
  // assignments.
  virtual plugin::StatusCode AssignImpl(
      std::vector<AssignmentItem>* assignments) const = 0;

  // Number of agents.
  int num_agents_ = 0;

  // Number of tasks.
  int num_tasks_ = 0;

  // Assignment cost matrix of dimensions num_agents x num_tasks.
  std::vector<std::vector<double>> costs_;

 private:
  // Validate the cost matrix.
  plugin::StatusCode ValidateCosts() const;
};

}  // namespace assignment
