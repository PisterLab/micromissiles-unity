// This file defines some useful utilities for Protobuf.

#pragma once

#include <cstddef>
#include <filesystem>
#include <fstream>
#include <limits>

#include "Plugin/status.pb.h"
#include "base/logging.h"
#include "google/protobuf/io/zero_copy_stream_impl.h"
#include "google/protobuf/text_format.h"

namespace protobuf {

// Load the Protobuf text file into a Protobuf message.
template <typename T>
plugin::StatusCode LoadProtobufTextFile(const std::filesystem::path& file,
                                        T* message) {
  if (message == nullptr) {
    LOG(ERROR) << "Null message pointer.";
    return plugin::STATUS_INVALID_ARGUMENT;
  }
  if (!std::filesystem::exists(file)) {
    LOG(ERROR) << "Protobuf text file does not exist: " << file << ".";
    return plugin::STATUS_NOT_FOUND;
  }

  std::ifstream ifs(file);
  if (!ifs) {
    LOG(ERROR) << "Failed to open the Protobuf text file: " << file << ".";
    return plugin::STATUS_FAILED_PRECONDITION;
  }
  google::protobuf::io::IstreamInputStream file_stream(&ifs);
  if (!google::protobuf::TextFormat::Parse(&file_stream, message)) {
    LOG(ERROR) << "Failed to parse the Protobuf text file: " << file << ".";
    return plugin::STATUS_INTERNAL;
  }
  return plugin::STATUS_OK;
}

// Serialize the Protobuf message to a buffer and return the length of the
// serialized message in bytes as an output argument.
template <typename T>
plugin::StatusCode SerializeToBuffer(const T& message, void* buffer,
                                     const std::size_t size,
                                     std::size_t* serialized_length) {
  if (buffer == nullptr) {
    LOG(ERROR) << "Null buffer pointer.";
    return plugin::STATUS_INVALID_ARGUMENT;
  }
  if (serialized_length == nullptr) {
    LOG(ERROR) << "Null serialized length pointer.";
    return plugin::STATUS_INVALID_ARGUMENT;
  }

  const auto message_size = message.ByteSizeLong();
  if (message_size >
      static_cast<std::size_t>(std::numeric_limits<int>::max())) {
    LOG(ERROR) << "Protobuf message size " << message_size
               << " exceeds maximum serializable size.";
    return plugin::STATUS_OUT_OF_RANGE;
  }
  if (message_size > size) {
    LOG(ERROR) << "Failed to serialize the Protobuf message to a buffer due to "
                  "insufficient buffer size: "
               << message_size << " vs. " << size << ".";
    return plugin::STATUS_FAILED_PRECONDITION;
  }
  if (!message.SerializeToArray(buffer, static_cast<int>(message_size))) {
    LOG(ERROR) << "Failed to serialize the Protobuf message to a buffer.";
    return plugin::STATUS_INTERNAL;
  }
  *serialized_length = message_size;
  return plugin::STATUS_OK;
}

}  // namespace protobuf
