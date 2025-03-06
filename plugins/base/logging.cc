#include "base/logging.h"

#include "absl/base/log_severity.h"
#include "absl/log/globals.h"
#include "absl/log/initialize.h"

void base::InitLogging() {
  absl::InitializeLog();

  // Set the minimum log level and stderr threshold to INFO.
  absl::SetMinLogLevel(absl::LogSeverityAtLeast::kInfo);
  absl::SetStderrThreshold(absl::LogSeverityAtLeast::kInfo);
}
