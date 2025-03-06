#include <cstdlib>
#include <numeric>
#include <string>
#include <vector>

#include "base/base.h"
#include "base/logging.h"
#include "ortools/graph/assignment.h"

int main(int argc, char** argv) {
  base::Init(argc, argv);

  // Simple linear sum assignment problem.
  operations_research::SimpleLinearSumAssignment assignment;

  const int num_workers = 4;
  std::vector<int> all_workers(num_workers);
  std::iota(all_workers.begin(), all_workers.end(), 0);

  const int num_tasks = 4;
  std::vector<int> all_tasks(num_tasks);
  std::iota(all_tasks.begin(), all_tasks.end(), 0);

  const std::vector<std::vector<int>> costs = {{
      {{90, 76, 75, 70}},    // Worker 0
      {{35, 85, 55, 65}},    // Worker 1
      {{125, 95, 90, 105}},  // Worker 2
      {{45, 110, 95, 115}},  // Worker 3
  }};

  for (const int w : all_workers) {
    for (const int t : all_tasks) {
      if (costs[w][t]) {
        assignment.AddArcWithCost(w, t, costs[w][t]);
      }
    }
  }

  operations_research::SimpleLinearSumAssignment::Status status =
      assignment.Solve();

  if (status == operations_research::SimpleLinearSumAssignment::OPTIMAL) {
    LOG(INFO) << "Total cost: " << assignment.OptimalCost() << ".";
    for (const int worker : all_workers) {
      LOG(INFO) << "Worker " << std::to_string(worker) << " assigned to task "
                << std::to_string(assignment.RightMate(worker)) << ". Cost: "
                << std::to_string(assignment.AssignmentCost(worker)) << ".";
    }
  } else {
    LOG(INFO) << "Solving the linear assignment problem failed.";
  }

  return EXIT_SUCCESS;
}
