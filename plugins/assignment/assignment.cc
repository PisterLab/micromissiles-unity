#include "assignment/assignment.h"

#include <cstddef>
#include <vector>

#include "Plugin/status.pb.h"
#include "base/logging.h"

namespace assignment {

plugin::StatusCode Assignment::Assign(
    std::vector<AssignmentItem>* assignments) const {
  const auto validate_status = ValidateCosts();
  if (validate_status != plugin::STATUS_OK) {
    return validate_status;
  }
  return AssignImpl(assignments);
}

plugin::StatusCode Assignment::ValidateCosts() const {
  // Validate the first dimension of the cost matrix.
  if (costs_.size() != static_cast<std::size_t>(num_agents_)) {
    LOG(ERROR) << "The assignment cost matrix has an incorrect number of "
                  "rows: "
               << costs_.size() << " vs. " << num_agents_ << ".";
    return plugin::STATUS_INVALID_ARGUMENT;
  }

  // Validate the second dimension of the cost matrix.
  for (const auto& row : costs_) {
    if (row.size() != static_cast<std::size_t>(num_tasks_)) {
      LOG(ERROR) << "The assignment cost matrix has an incorrect number "
                    "of columns: "
                 << row.size() << " vs. " << num_tasks_ << ".";
      return plugin::STATUS_INVALID_ARGUMENT;
    }
  }

  return plugin::STATUS_OK;
}

}  // namespace assignment
