#include "protobuf/protobuf.h"

#include <gtest/gtest.h>

#include <cstddef>
#include <cstdint>
#include <filesystem>
#include <vector>

#include "Configs/attack_behavior_config.pb.h"
#include "Configs/simulation_config.pb.h"
#include "Configs/static_config.pb.h"
#include "Plugin/status.pb.h"

namespace protobuf {
namespace {

TEST(ProtobufTest, LoadProtobufTextAttackBehaviorConfigFileTest) {
  const auto kAttackBehaviorConfigFile =
      std::filesystem::path("..") / "micromissiles-configs-data+" / "Attacks" /
      "default_direct_attack.pbtxt";
  configs::AttackBehaviorConfig attack_behavior_config;
  const auto status = LoadProtobufTextFile<configs::AttackBehaviorConfig>(
      kAttackBehaviorConfigFile, &attack_behavior_config);
  EXPECT_EQ(status, plugin::STATUS_OK);
  EXPECT_TRUE(attack_behavior_config.has_flight_plan());
  EXPECT_EQ(attack_behavior_config.flight_plan().waypoints().size(), 3);
}

TEST(ProtobufTest, LoadProtobufTextStaticConfigFileTest) {
  const auto kStaticConfigFile = std::filesystem::path("..") /
                                 "micromissiles-configs-data+" / "Models" /
                                 "micromissile.pbtxt";
  configs::StaticConfig static_config;
  const auto status = LoadProtobufTextFile<configs::StaticConfig>(
      kStaticConfigFile, &static_config);
  EXPECT_EQ(status, plugin::STATUS_OK);
  EXPECT_TRUE(static_config.has_acceleration_config());
  EXPECT_TRUE(static_config.has_boost_config());
  EXPECT_TRUE(static_config.has_lift_drag_config());
  EXPECT_TRUE(static_config.has_body_config());
  EXPECT_FALSE(static_config.has_hit_config());
}

TEST(ProtobufTest, LoadProtobufTextSimulationConfigFileTest) {
  const auto kSimulationConfigFile = std::filesystem::path("..") /
                                     "micromissiles-configs-data+" /
                                     "Simulations" / "7_quadcopters.pbtxt";
  configs::SimulationConfig simulation_config;
  const auto status = LoadProtobufTextFile<configs::SimulationConfig>(
      kSimulationConfigFile, &simulation_config);
  EXPECT_EQ(status, plugin::STATUS_OK);
  EXPECT_EQ(simulation_config.interceptor_swarm_configs().size(), 1);
  EXPECT_EQ(simulation_config.threat_swarm_configs().size(), 1);
}

TEST(ProtobufTest, LoadProtobufTextInvalidFileTest) {
  const auto kSimulationConfigFile = std::filesystem::path("..") /
                                     "micromissiles-configs-data+" /
                                     "Simulations" / "invalid.pbtxt";
  configs::SimulationConfig simulation_config;
  const auto status = LoadProtobufTextFile<configs::SimulationConfig>(
      kSimulationConfigFile, &simulation_config);
  EXPECT_NE(status, plugin::STATUS_OK);
}

TEST(ProtobufTest, SerializeToBufferTest) {
  const auto kStaticConfigFile = std::filesystem::path("..") /
                                 "micromissiles-configs-data+" / "Models" /
                                 "micromissile.pbtxt";
  configs::StaticConfig static_config;
  const auto load_status = LoadProtobufTextFile<configs::StaticConfig>(
      kStaticConfigFile, &static_config);
  EXPECT_EQ(load_status, plugin::STATUS_OK);
  std::vector<uint8_t> buffer(1024);
  std::size_t serialized_length = 0;
  const auto serialize_status = SerializeToBuffer(
      static_config, buffer.data(), buffer.size(), &serialized_length);
  EXPECT_EQ(serialize_status, plugin::STATUS_OK);
  EXPECT_EQ(serialized_length, static_config.ByteSizeLong());
}

TEST(ProtobufTest, SerializeToBufferInsufficientSizeTest) {
  const auto kStaticConfigFile = std::filesystem::path("..") /
                                 "micromissiles-configs-data+" / "Models" /
                                 "micromissile.pbtxt";
  configs::StaticConfig static_config;
  const auto load_status = LoadProtobufTextFile<configs::StaticConfig>(
      kStaticConfigFile, &static_config);
  EXPECT_EQ(load_status, plugin::STATUS_OK);
  std::vector<uint8_t> buffer(1);
  std::size_t serialized_length = 0;
  EXPECT_NE(SerializeToBuffer(static_config, buffer.data(), buffer.size(),
                              &serialized_length),
            plugin::STATUS_OK);
}

}  // namespace
}  // namespace protobuf
