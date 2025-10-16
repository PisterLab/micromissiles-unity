#include "assignment/assignment.h"

#include <cstddef>
#include <stdexcept>
#include <vector>

#include "absl/strings/str_format.h"

namespace assignment {

void Assignment::ValidateCosts() const {
  // Validate the first dimension of the cost matrix.
  if (costs_.size() != static_cast<std::size_t>(num_agents_)) {
    throw std::invalid_argument(
        absl::StrFormat("The assignment cost matrix has an incorrect number of "
                        "rows: %d vs. %d.",
                        costs_.size(), num_agents_));
  }

  // Validate the second dimension of the cost matrix.
  for (const auto& row : costs_) {
    if (row.size() != static_cast<std::size_t>(num_tasks_)) {
      throw std::invalid_argument(
          absl::StrFormat("The assignment cost matrix has an incorrect number "
                          "of columns: %d vs. %d.",
                          row.size(), num_tasks_));
    }
  }
}

}  // namespace assignment
