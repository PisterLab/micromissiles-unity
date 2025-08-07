#include "protobuf/protobuf.h"

#include <gtest/gtest.h>

#include <cstdint>
#include <stdexcept>
#include <string>
#include <vector>

#include "configs/proto/static_config.pb.h"

namespace protobuf {
namespace {

TEST(ProtobufTest, LoadProtobufTextFileTest) {
  const std::string kStaticConfigFile =
      "configs/models/Interceptors/micromissile.pbtxt";
  const auto static_config =
      LoadProtobufTextFile<micromissiles::StaticConfig>(kStaticConfigFile);
  EXPECT_TRUE(static_config.has_acceleration_config());
  EXPECT_TRUE(static_config.has_boost_config());
  EXPECT_TRUE(static_config.has_lift_drag_config());
  EXPECT_TRUE(static_config.has_body_config());
  EXPECT_FALSE(static_config.has_hit_config());
}

TEST(ProtobufTest, SerializeToBufferTest) {
  const std::string kStaticConfigFile =
      "configs/models/Interceptors/micromissile.pbtxt";
  const auto static_config =
      LoadProtobufTextFile<micromissiles::StaticConfig>(kStaticConfigFile);
  std::vector<uint8_t> buffer(1024);
  EXPECT_NO_THROW(
      SerializeToBuffer(static_config, buffer.data(), buffer.size()));
}

TEST(ProtobufTest, SerializeToBufferInsufficientSizeTest) {
  const std::string kStaticConfigFile =
      "configs/models/Interceptors/micromissile.pbtxt";
  const auto static_config =
      LoadProtobufTextFile<micromissiles::StaticConfig>(kStaticConfigFile);
  std::vector<uint8_t> buffer(1);
  EXPECT_THROW(SerializeToBuffer(static_config, buffer.data(), buffer.size()),
               std::runtime_error);
}

}  // namespace
}  // namespace protobuf
