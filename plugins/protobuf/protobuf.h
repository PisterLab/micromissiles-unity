// This file defines some useful utilities for Protobuf.

#pragma once

#include <cstddef>
#include <fstream>
#include <stdexcept>
#include <string>

#include "absl/strings/str_format.h"
#include "google/protobuf/io/zero_copy_stream_impl.h"
#include "google/protobuf/text_format.h"

namespace protobuf {

// Load the Protobuf text file and return the Protobuf message.
template <typename T>
T LoadProtobufTextFile(const std::string& file) {
  std::ifstream ifs(file);
  if (!ifs.is_open()) {
    throw std::runtime_error(
        absl::StrFormat("Failed to open the Protobuf text file: %s.", file));
  }
  google::protobuf::io::IstreamInputStream file_stream(&ifs);
  T message;
  if (!google::protobuf::TextFormat::Parse(&file_stream, &message)) {
    throw std::runtime_error(
        absl::StrFormat("Failed to parse the Protobuf text file: %s.", file));
  }
  return message;
}

// Serialize the Protobuf message to a buffer and return the length of the
// serialized message in bytes.
template <typename T>
std::size_t SerializeToBuffer(const T& message, void* buffer,
                              const std::size_t size) {
  const auto message_size = message.ByteSizeLong();
  if (message_size > size) {
    throw std::runtime_error(
        absl::StrFormat("Failed to serialize the Protobuf message to a buffer "
                        "due to insufficient buffer size: %zu vs. %zu.",
                        message_size, size));
  }
  if (!message.SerializeToArray(buffer, message_size)) {
    throw std::runtime_error(
        "Failed to serialize the Protobuf message to a buffer.");
  }
  return message_size;
}

}  // namespace protobuf
