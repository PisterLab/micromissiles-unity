// This file defines some useful utilities for Protobuf.

#pragma once

#include <cstddef>
#include <fstream>
#include <iostream>
#include <string>

#include "Plugin/status.pb.h"
#include "google/protobuf/io/zero_copy_stream_impl.h"
#include "google/protobuf/text_format.h"

namespace protobuf {

// Load the Protobuf text file into a Protobuf message.
template <typename T>
plugin::StatusCode LoadProtobufTextFile(const std::string& file, T* message) {
  std::ifstream ifs(file);
  if (!ifs.is_open()) {
    std::cerr << "Failed to open the Protobuf text file: " << file << ".";
    return plugin::STATUS_INVALID_ARGUMENT;
  }
  google::protobuf::io::IstreamInputStream file_stream(&ifs);
  if (!google::protobuf::TextFormat::Parse(&file_stream, message)) {
    std::cerr << "Failed to parse the Protobuf text file: " << file << ".";
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
  *serialized_length = message.ByteSizeLong();
  if (*serialized_length > size) {
    std::cerr << "Failed to serialize the Protobuf message to a buffer due to "
                 "insufficient buffer size: "
              << *serialized_length << " vs. " << size << ".";
    return plugin::STATUS_FAILED_PRECONDITION;
  }
  if (!message.SerializeToArray(buffer, *serialized_length)) {
    std::cerr << "Failed to serialize the Protobuf message to a buffer.";
    return plugin::STATUS_INTERNAL;
  }
  return plugin::STATUS_OK;
}

}  // namespace protobuf
