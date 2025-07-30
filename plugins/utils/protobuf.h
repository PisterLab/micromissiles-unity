// This file defines some useful utilities for Protobuf.

#pragma once

#include <fcntl.h>

#include <stdexcept>
#include <string>

#include "absl/strings/str_format.h"
#include "google/protobuf/io/zero_copy_stream_impl.h"
#include "google/protobuf/text_format.h"

namespace protobuf {

// Load the Protobuf text file.
template <typename T>
T LoadProtobufTextFile(const std::string& file) {
  const int fd = open(file.c_str(), O_RDONLY);
  if (fd < 0) {
    throw std::runtime_error(
        absl::StrFormat("Failed to open the Protobuf text file: %s", file));
  }
  google::protobuf::io::FileInputStream file_stream(fd);
  T message;
  if (!google::protobuf::TextFormat::Parse(&file_stream, &message)) {
    throw std::runtime_error(
        absl::StrFormat("Failed to parse the Protobuf text file: %s.", file));
  }
  file_stream.Close();
  return message;
}

}  // namespace protobuf
