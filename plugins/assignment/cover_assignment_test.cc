#include "assignment/cover_assignment.h"

#include <gtest/gtest.h>

#include <unordered_map>
#include <vector>

#include "Plugin/status.pb.h"
#include "assignment/assignment.h"

namespace assignment {
namespace {

TEST(CoverAssignmentTest, AssignUnique) {
  constexpr int kNumAgents = 5;
  constexpr int kNumTasks = 4;
  const std::vector<std::vector<double>> costs{
      {90, 80, 75, 70},   {35, 85, 55, 65},   {125, 95, 90, 95},
      {45, 110, 95, 115}, {50, 100, 90, 100},
  };
  CoverAssignment assignment(kNumAgents, kNumTasks, costs);
  std::vector<Assignment::AssignmentItem> assignments;
  EXPECT_EQ(assignment.Assign(&assignments), plugin::STATUS_OK);
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

TEST(CoverAssignmentTest, AssignMultiple) {
  constexpr int kNumAgents = 5;
  constexpr int kNumTasks = 2;
  const std::vector<std::vector<double>> costs{
      {1, 3}, {5, 1}, {4, 2}, {6, 1}, {7, 1},
  };
  CoverAssignment assignment(kNumAgents, kNumTasks, costs);
  std::vector<Assignment::AssignmentItem> assignments;
  EXPECT_EQ(assignment.Assign(&assignments), plugin::STATUS_OK);
  std::unordered_map<int, int> expected_assignments{
      {0, 0}, {1, 1}, {2, 1}, {3, 1}, {4, 1},
  };
  EXPECT_EQ(assignments.size(), expected_assignments.size());
  for (const auto& [agent_index, task_index] : assignments) {
    EXPECT_EQ(expected_assignments[agent_index], task_index)
        << "Agent " << agent_index << " was assigned to task " << task_index
        << " but expected task " << expected_assignments[agent_index] << ".";
  }
}

TEST(CoverAssignmentTest, AssignFewer) {
  constexpr int kNumAgents = 2;
  constexpr int kNumTasks = 3;
  const std::vector<std::vector<double>> costs{
      {3, 0, 2},
      {4, 2, 3},
  };
  CoverAssignment assignment(kNumAgents, kNumTasks, costs);
  std::vector<Assignment::AssignmentItem> assignments;
  EXPECT_EQ(assignment.Assign(&assignments), plugin::STATUS_OK);
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

TEST(CoverAssignmentTest, AssignInvalidCostsNumAgents) {
  constexpr int kNumAgents = 2;
  constexpr int kNumTasks = 3;
  const std::vector<std::vector<double>> costs{
      {3, 0, 2},
      {4, 2, 3},
      {4, 2, 3},
  };
  CoverAssignment assignment(kNumAgents, kNumTasks, costs);
  std::vector<Assignment::AssignmentItem> assignments;
  EXPECT_NE(assignment.Assign(&assignments), plugin::STATUS_OK);
}

TEST(CoverAssignmentTest, AssignInvalidCostsNumTasks) {
  constexpr int kNumAgents = 2;
  constexpr int kNumTasks = 3;
  const std::vector<std::vector<double>> costs{
      {3, 0, 2},
      {4, 2, 3, 4},
  };
  CoverAssignment assignment(kNumAgents, kNumTasks, costs);
  std::vector<Assignment::AssignmentItem> assignments;
  EXPECT_NE(assignment.Assign(&assignments), plugin::STATUS_OK);
}

}  // namespace
}  // namespace assignment
