#include "protobuf/protobuf.h"

#include <gtest/gtest.h>

#include <cstdint>
#include <stdexcept>
#include <string>
#include <vector>

#include "Configs/simulation_config.pb.h"
#include "Configs/static_config.pb.h"

namespace protobuf {
namespace {

TEST(ProtobufTest, LoadProtobufTextStaticConfigFileTest) {
  const std::string kStaticConfigFile =
      "../micromissiles-configs-data+/Models/micromissile.pbtxt";
  const auto static_config =
      LoadProtobufTextFile<configs::StaticConfig>(kStaticConfigFile);
  EXPECT_TRUE(static_config.has_acceleration_config());
  EXPECT_TRUE(static_config.has_boost_config());
  EXPECT_TRUE(static_config.has_lift_drag_config());
  EXPECT_TRUE(static_config.has_body_config());
  EXPECT_FALSE(static_config.has_hit_config());
}

TEST(ProtobufTest, LoadProtobufTextSimulationConfigFileTest) {
  const std::string kSimulationConfigFile =
      "../micromissiles-configs-data+/Simulations/7_quadcopters.pbtxt";
  const auto static_config =
      LoadProtobufTextFile<configs::SimulationConfig>(kSimulationConfigFile);
  EXPECT_EQ(static_config.interceptor_swarm_configs().size(), 1);
  EXPECT_EQ(static_config.threat_swarm_configs().size(), 1);
}

TEST(ProtobufTest, SerializeToBufferTest) {
  const std::string kStaticConfigFile =
      "../micromissiles-configs-data+/Models/micromissile.pbtxt";
  const auto static_config =
      LoadProtobufTextFile<configs::StaticConfig>(kStaticConfigFile);
  std::vector<uint8_t> buffer(1024);
  EXPECT_NO_THROW(
      SerializeToBuffer(static_config, buffer.data(), buffer.size()));
}

TEST(ProtobufTest, SerializeToBufferInsufficientSizeTest) {
  const std::string kStaticConfigFile =
      "../micromissiles-configs-data+/Models/micromissile.pbtxt";
  const auto static_config =
      LoadProtobufTextFile<configs::StaticConfig>(kStaticConfigFile);
  std::vector<uint8_t> buffer(1);
  EXPECT_THROW(SerializeToBuffer(static_config, buffer.data(), buffer.size()),
               std::runtime_error);
}

}  // namespace
}  // namespace protobuf
