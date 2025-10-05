#include "assignment/even_assignment.h"

#include <gtest/gtest.h>

#include <unordered_map>
#include <vector>

namespace assignment {
namespace {

TEST(EvenAssignmentTest, AssignUnique) {
  constexpr int kNumAgents = 5;
  constexpr int kNumTasks = 4;
  const std::vector<std::vector<double>> costs{
      {90, 80, 75, 70},   {35, 85, 55, 65},   {125, 95, 90, 95},
      {45, 110, 95, 115}, {50, 100, 90, 100},
  };
  EvenAssignment assignment(kNumAgents, kNumTasks, costs);
  const auto assignments = assignment.Assign();
  std::unordered_map<int, int> expected_assignments{
      {0, 3}, {1, 2}, {2, 1}, {3, 0}, {4, 0},
  };
  EXPECT_EQ(assignments.size(), expected_assignments.size());
  for (const auto& [agent_index, task_index] : assignments) {
    EXPECT_EQ(expected_assignments[agent_index], task_index)
        << "Agent " << agent_index << " was assigned to task " << task_index
        << " but expected task " << expected_assignments[agent_index] << ".";
  }
}

TEST(EvenAssignmentTest, AssignMultiple) {
  constexpr int kNumAgents = 5;
  constexpr int kNumTasks = 2;
  const std::vector<std::vector<double>> costs{
      {1, 3}, {5, 1}, {4, 2}, {6, 1}, {7, 1},
  };
  EvenAssignment assignment(kNumAgents, kNumTasks, costs);
  const auto assignments = assignment.Assign();
  std::unordered_map<int, int> expected_assignments{
      {0, 0}, {1, 1}, {2, 0}, {3, 1}, {4, 1},
  };
  EXPECT_EQ(assignments.size(), expected_assignments.size());
  for (const auto& [agent_index, task_index] : assignments) {
    EXPECT_EQ(expected_assignments[agent_index], task_index)
        << "Agent " << agent_index << " was assigned to task " << task_index
        << " but expected task " << expected_assignments[agent_index] << ".";
  }
}

TEST(EvenAssignmentTest, AssignFewer) {
  constexpr int kNumAgents = 2;
  constexpr int kNumTasks = 3;
  const std::vector<std::vector<double>> costs{
      {3, 0, 2},
      {4, 2, 3},
  };
  EvenAssignment assignment(kNumAgents, kNumTasks, costs);
  const auto assignments = assignment.Assign();
  std::unordered_map<int, int> expected_assignments{
      {0, 1},
      {1, 2},
  };
  EXPECT_EQ(assignments.size(), expected_assignments.size());
  for (const auto& [agent_index, task_index] : assignments) {
    EXPECT_EQ(expected_assignments[agent_index], task_index)
        << "Agent " << agent_index << " was assigned to task " << task_index
        << " but expected task " << expected_assignments[agent_index] << ".";
  }
}

}  // namespace
}  // namespace assignment
