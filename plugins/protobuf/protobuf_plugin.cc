// The Protobuf plugin allows Protobuf text files to be parsed and loaded into
// the Unity simulator. Since the Protobuf C# library does not natively parse
// text format, the Protobuf plugin converts the text format into binary for the
// Protobuf C# library to then parse.

#include <cstring>

#include "configs/proto/simulation_config.pb.h"
#include "configs/proto/simulator_config.pb.h"
#include "configs/proto/static_config.pb.h"
#include "protobuf/protobuf.h"

// Macro to define a function to load a Protobuf message from a text file to
// binary format and serialize it to a buffer. The function returns the length
// of the serialized message.
#define DEFINE_PROTOBUF_LOADER(Message)                                 \
  int Protobuf_##Message##_LoadToBinary(const char* file, void* buffer, \
                                        const int size) {               \
    const auto message =                                                \
        protobuf::LoadProtobufTextFile<micromissiles::Message>(         \
            std::string(file));                                         \
    const auto serialized_length =                                      \
        protobuf::SerializeToBuffer(message, buffer, size);             \
    return static_cast<int>(serialized_length);                         \
  }

extern "C" {
DEFINE_PROTOBUF_LOADER(StaticConfig);
DEFINE_PROTOBUF_LOADER(SimulationConfig);
DEFINE_PROTOBUF_LOADER(SimulatorConfig);
}
