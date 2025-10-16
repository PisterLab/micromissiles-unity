// The assignment class is an interface for a cost-based assignment problem.

#pragma once

#include <utility>
#include <vector>

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
        costs_(std::move(costs)) {
    ValidateCosts();
  }

  Assignment(const Assignment&) = default;
  Assignment& operator=(const Assignment&) = default;

  virtual ~Assignment() = default;

  // Assign the agents to the tasks.
  virtual std::vector<AssignmentItem> Assign() const = 0;

 protected:
  // Number of agents.
  int num_agents_ = 0;

  // Number of tasks.
  int num_tasks_ = 0;

  // Assignment cost matrix of dimensions num_agents x num_tasks.
  std::vector<std::vector<double>> costs_;

 private:
  // Validate the cost matrix.
  void ValidateCosts() const;
};

}  // namespace assignment
