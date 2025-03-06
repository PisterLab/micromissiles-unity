#include <cstdlib>
#include <memory>

#include "base/base.h"
#include "base/logging.h"
#include "ortools/linear_solver/linear_solver.h"

int main(int argc, char** argv) {
  base::Init(argc, argv);

  // Create the mixed integer programming solver with the SCIP backend.
  std::unique_ptr<operations_research::MPSolver> solver(
      operations_research::MPSolver::CreateSolver("SCIP"));
  if (!solver) {
    LOG(WARNING) << "SCIP solver unavailable.";
    return EXIT_FAILURE;
  }

  const double infinity = solver->infinity();
  // x and y are integer non-negative variables.
  const operations_research::MPVariable* x =
      solver->MakeIntVar(0.0, infinity, "x");
  const operations_research::MPVariable* y =
      solver->MakeIntVar(0.0, infinity, "y");

  LOG(INFO) << "Number of variables: " << solver->NumVariables() << ".";

  // x + 7 * y <= 17.5.
  operations_research::MPConstraint* c0 =
      solver->MakeRowConstraint(-infinity, 17.5, "c0");
  c0->SetCoefficient(x, 1);
  c0->SetCoefficient(y, 7);

  // x <= 3.5.
  operations_research::MPConstraint* c1 =
      solver->MakeRowConstraint(-infinity, 3.5, "c1");
  c1->SetCoefficient(x, 1);
  c1->SetCoefficient(y, 0);

  LOG(INFO) << "Number of constraints: " << solver->NumConstraints() << ".";

  // Maximize x + 10 * y.
  operations_research::MPObjective* objective = solver->MutableObjective();
  objective->SetCoefficient(x, 1);
  objective->SetCoefficient(y, 10);
  objective->SetMaximization();

  const operations_research::MPSolver::ResultStatus result_status =
      solver->Solve();
  // Check that the problem has an optimal solution.
  if (result_status != operations_research::MPSolver::OPTIMAL) {
    LOG(FATAL) << "The problem does not have an optimal solution.";
  }

  LOG(INFO) << "Solution:";
  LOG(INFO) << "Objective value: " << objective->Value() << ".";
  LOG(INFO) << "x: " << x->solution_value() << ".";
  LOG(INFO) << "y: " << y->solution_value() << ".";

  LOG(INFO) << "\nAdvanced usage:";
  LOG(INFO) << "Problem solved in " << solver->wall_time() << " milliseconds.";
  LOG(INFO) << "Problem solved in " << solver->iterations() << " iterations.";
  LOG(INFO) << "Problem solved in " << solver->nodes()
            << " branch-and-bound nodes.";

  return EXIT_SUCCESS;
}
