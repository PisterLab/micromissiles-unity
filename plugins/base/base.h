// The file contains base utilities.

#pragma once

#include <cstdbool>
#include <cstdint>
#include <cstdlib>
#include <string>

#include "base/commandlineflags.h"
#include "base/logging.h"

namespace base {

// Initialize the base utilities.
void Init(int argc, char** argv) {
  base::InitCommandLineFlags(argc, argv);
  base::InitLogging();
}

}  // namespace base
