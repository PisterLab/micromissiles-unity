#include "assignment/weighted_even_assignment.h"

#include <gtest/gtest.h>

#include <unordered_map>
#include <vector>

#include "Plugin/status.pb.h"
#include "assignment/assignment.h"

namespace assignment {
namespace {

TEST(WeightedEvenAssignmentTest, AssignUnique) {
  constexpr int kNumAgents = 5;
  constexpr int kNumTasks = 4;
  const std::vector<std::vector<double>> costs{
      {90, 80, 75, 70},   {35, 85, 55, 65},   {125, 95, 90, 95},
      {45, 110, 95, 115}, {50, 100, 90, 100},
  };
  const std::vector<double> weights{1, 1, 1, 1};
  WeightedEvenAssignment assignment(kNumAgents, kNumTasks, costs, weights,
                                    /*weighted_scaling_factor=*/1);
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

TEST(WeightedEvenAssignmentTest, AssignMultiple) {
  constexpr int kNumAgents = 5;
  constexpr int kNumTasks = 2;
  const std::vector<std::vector<double>> costs{
      {1, 3}, {5, 1}, {4, 2}, {6, 1}, {7, 1},
  };
  const std::vector<double> weights{1, 1};
  WeightedEvenAssignment assignment(kNumAgents, kNumTasks, costs, weights,
                                    /*weighted_scaling_factor=*/1);
  std::vector<Assignment::AssignmentItem> assignments;
  EXPECT_EQ(assignment.Assign(&assignments), plugin::STATUS_OK);
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

TEST(WeightedEvenAssignmentTest, AssignWeighted) {
  constexpr int kNumAgents = 6;
  constexpr int kNumTasks = 2;
  const std::vector<std::vector<double>> costs{
      {1, 3}, {5, 1}, {4, 2}, {6, 1}, {1, 7}, {1, 8},
  };
  const std::vector<double> weights{1, 0.5};
  WeightedEvenAssignment assignment(kNumAgents, kNumTasks, costs, weights,
                                    /*weighted_scaling_factor=*/2);
  std::vector<Assignment::AssignmentItem> assignments;
  EXPECT_EQ(assignment.Assign(&assignments), plugin::STATUS_OK);
  std::unordered_map<int, int> expected_assignments{
      {0, 1}, {1, 1}, {2, 1}, {3, 1}, {4, 0}, {5, 0},
  };
  EXPECT_EQ(assignments.size(), expected_assignments.size());
  for (const auto& [agent_index, task_index] : assignments) {
    EXPECT_EQ(expected_assignments[agent_index], task_index)
        << "Agent " << agent_index << " was assigned to task " << task_index
        << " but expected task " << expected_assignments[agent_index] << ".";
  }
}

TEST(WeightedEvenAssignmentTest, AssignFewer) {
  constexpr int kNumAgents = 2;
  constexpr int kNumTasks = 3;
  const std::vector<std::vector<double>> costs{
      {3, 0, 2},
      {4, 2, 3},
  };
  const std::vector<double> weights{1, 1, 1};
  WeightedEvenAssignment assignment(kNumAgents, kNumTasks, costs, weights,
                                    /*weighted_scaling_factor=*/1);
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

TEST(WeightedEvenAssignmentTest, AssignInvalidCostsNumAgents) {
  constexpr int kNumAgents = 2;
  constexpr int kNumTasks = 3;
  const std::vector<std::vector<double>> costs{
      {3, 0, 2},
      {4, 2, 3},
      {4, 2, 3},
  };
  const std::vector<double> weights{1, 1, 1};
  WeightedEvenAssignment assignment(kNumAgents, kNumTasks, costs, weights,
                                    /*weighted_scaling_factor=*/1);
  std::vector<Assignment::AssignmentItem> assignments;
  EXPECT_NE(assignment.Assign(&assignments), plugin::STATUS_OK);
}

TEST(WeightedEvenAssignmentTest, AssignInvalidCostsNumTasks) {
  constexpr int kNumAgents = 2;
  constexpr int kNumTasks = 3;
  const std::vector<std::vector<double>> costs{
      {3, 0, 2},
      {4, 2, 3, 4},
  };
  const std::vector<double> weights{1, 1, 1};
  WeightedEvenAssignment assignment(kNumAgents, kNumTasks, costs, weights,
                                    /*weighted_scaling_factor=*/1);
  std::vector<Assignment::AssignmentItem> assignments;
  EXPECT_NE(assignment.Assign(&assignments), plugin::STATUS_OK);
}

TEST(WeightedEvenAssignmentTest, AssignInvalidWeights) {
  constexpr int kNumAgents = 2;
  constexpr int kNumTasks = 3;
  const std::vector<std::vector<double>> costs{
      {3, 0, 2},
      {4, 2, 3},
  };
  const std::vector<double> weights{1, 1, 1, 2};
  WeightedEvenAssignment assignment(kNumAgents, kNumTasks, costs, weights,
                                    /*weighted_scaling_factor=*/1);
  std::vector<Assignment::AssignmentItem> assignments;
  EXPECT_NE(assignment.Assign(&assignments), plugin::STATUS_OK);
}

}  // namespace
}  // namespace assignment
