#include "base/commandlineflags.h"

#include "absl/flags/parse.h"

void base::InitCommandLineFlags(int argc, char** argv) {
  absl::ParseCommandLine(argc, argv);
}
