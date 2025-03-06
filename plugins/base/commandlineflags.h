// The flag library contains utilities for commandline flags.

#pragma once

#include <cstring>
#include <vector>

#include "absl/flags/declare.h"
#include "absl/flags/flag.h"

// Macros to define commandline flags by type.
#define DEFINE_bool(name, default_value, help) \
  DEFINE_FLAG_IMPL(bool, name, default_value, help)
#define DEFINE_int16(name, default_value, help) \
  DEFINE_FLAG_IMPL(int16_t, name, default_value, help)
#define DEFINE_uint16(name, default_value, help) \
  DEFINE_FLAG_IMPL(uint16_t, name, default_value, help)
#define DEFINE_int32(name, default_value, help) \
  DEFINE_FLAG_IMPL(int32_t, name, default_value, help)
#define DEFINE_uint32(name, default_value, help) \
  DEFINE_FLAG_IMPL(uint32_t, name, default_value, help)
#define DEFINE_int64(name, default_value, help) \
  DEFINE_FLAG_IMPL(int64_t, name, default_value, help)
#define DEFINE_uint64(name, default_value, help) \
  DEFINE_FLAG_IMPL(uint64_t, name, default_value, help)
#define DEFINE_float(name, default_value, help) \
  DEFINE_FLAG_IMPL(float, name, default_value, help)
#define DEFINE_double(name, default_value, help) \
  DEFINE_FLAG_IMPL(double, name, default_value, help)
#define DEFINE_string(name, default_value, help) \
  DEFINE_FLAG_IMPL(std::string, name, default_value, help)
#define DEFINE_list(name, default_value, help) \
  DEFINE_FLAG_IMPL(std::vector<std::string>, name, default_value, help)

#define DEFINE_FLAG_IMPL(Type, name, default_value, help) \
  ABSL_FLAG(Type, name, default_value, help)

// Macros to declare commandline flags by type.
#define DECLARE_bool(name) DECLARE_FLAG_IMPL(bool, name)
#define DECLARE_int16(name) DECLARE_FLAG_IMPL(int16_t, name)
#define DECLARE_uint16(name) DECLARE_FLAG_IMPL(uint16_t, name)
#define DECLARE_int32(name) DECLARE_FLAG_IMPL(int32_t, name)
#define DECLARE_uint32(name) DECLARE_FLAG_IMPL(uint32_t, name)
#define DECLARE_int64(name) DECLARE_FLAG_IMPL(int64_t, name)
#define DECLARE_uint64(name) DECLARE_FLAG_IMPL(uint64_t, name)
#define DECLARE_float(name) DECLARE_FLAG_IMPL(float, name)
#define DECLARE_double(name) DECLARE_FLAG_IMPL(double, name)
#define DECLARE_string(name) DECLARE_FLAG_IMPL(std::string, name)
#define DECLARE_list(name) DECLARE_FLAG_IMPL(std::vector<std::string>, name)

#define DECLARE_FLAG_IMPL(Type, name) ABSL_DECLARE_FLAG(Type, name)

// Macros to access commandline flags.
#define FLAGS(name) absl::GetFlag(FLAGS_##name)

namespace base {

// Initialize the commandline flags.
void InitCommandLineFlags(int argc, char** argv);

}  // namespace base
