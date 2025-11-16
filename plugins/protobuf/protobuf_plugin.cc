// The Protobuf plugin allows Protobuf text files to be parsed and loaded into
// the Unity simulator. Since the Protobuf C# library does not natively parse
// text format, the Protobuf plugin converts the text format into binary for the
// Protobuf C# library to then parse.

#include <cstddef>
#include <cstring>

#include "Configs/attack_behavior_config.pb.h"
#include "Configs/run_config.pb.h"
#include "Configs/simulation_config.pb.h"
#include "Configs/simulator_config.pb.h"
#include "Configs/static_config.pb.h"
#include "Plugin/status.pb.h"
#include "protobuf/protobuf.h"

// Macro to define a function to load a Protobuf configuration message from a
// text file to binary format and serialize it to a buffer.
#define DEFINE_PROTOBUF_LOADER(Message)                                        \
  plugin::StatusCode Protobuf_##Message##_GetSerializedLength(                 \
      const char* file, int* serialized_length) {                              \
    configs::Message message;                                                  \
    const auto status = protobuf::LoadProtobufTextFile<configs::Message>(      \
        std::string(file), &message);                                          \
    if (status != plugin::STATUS_OK) {                                         \
      return status;                                                           \
    }                                                                          \
    *serialized_length = static_cast<int>(message.ByteSizeLong());             \
    return plugin::STATUS_OK;                                                  \
  }                                                                            \
  plugin::StatusCode Protobuf_##Message##_LoadToBinary(                        \
      const char* file, void* buffer, const int size,                          \
      int* serialized_length) {                                                \
    configs::Message message;                                                  \
    const auto load_status = protobuf::LoadProtobufTextFile<configs::Message>( \
        std::string(file), &message);                                          \
    if (load_status != plugin::STATUS_OK) {                                    \
      return load_status;                                                      \
    }                                                                          \
    std::size_t length = 0;                                                    \
    const auto serialize_status =                                              \
        protobuf::SerializeToBuffer(message, buffer, size, &length);           \
    if (serialize_status != plugin::STATUS_OK) {                               \
      return serialize_status;                                                 \
    }                                                                          \
    *serialized_length = static_cast<int>(length);                             \
    return plugin::STATUS_OK;                                                  \
  }

extern "C" {
DEFINE_PROTOBUF_LOADER(AttackBehaviorConfig);
DEFINE_PROTOBUF_LOADER(RunConfig);
DEFINE_PROTOBUF_LOADER(SimulationConfig);
DEFINE_PROTOBUF_LOADER(SimulatorConfig);
DEFINE_PROTOBUF_LOADER(StaticConfig);
}
