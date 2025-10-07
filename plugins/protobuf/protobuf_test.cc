#include "protobuf/protobuf.h"

#include <gtest/gtest.h>

#include <cstddef>
#include <cstdint>
#include <filesystem>
#include <fstream>
#include <memory>
#include <string>
#include <vector>

#include "Configs/attack_behavior_config.pb.h"
#include "Configs/simulation_config.pb.h"
#include "Configs/static_config.pb.h"
#include "Plugin/status.pb.h"
#include "tools/cpp/runfiles/runfiles.h"

namespace protobuf {
namespace {

// Return the full runfiles path.
std::filesystem::path GetRunfilesPath(const std::filesystem::path& file) {
  std::string error;
  auto runfiles = bazel::tools::cpp::runfiles::Runfiles::CreateForTest(&error);
  if (runfiles == nullptr) {
    ADD_FAILURE() << "Runfiles error: " << error << ".";
    return std::filesystem::path();
  }
  return std::filesystem::path(runfiles->Rlocation(file.string()));
}

TEST(ProtobufTest, LoadProtobufTextFileAttackBehaviorConfig) {
  const auto kAttackBehaviorConfigFile = GetRunfilesPath(
      "micromissiles-configs-data/Attacks/default_direct_attack.pbtxt");
  configs::AttackBehaviorConfig attack_behavior_config;
  EXPECT_EQ(LoadProtobufTextFile<configs::AttackBehaviorConfig>(
                kAttackBehaviorConfigFile, &attack_behavior_config),
            plugin::STATUS_OK);
  EXPECT_TRUE(attack_behavior_config.has_flight_plan());
  EXPECT_EQ(attack_behavior_config.flight_plan().waypoints().size(), 3);
}

TEST(ProtobufTest, LoadProtobufTextFileStaticConfig) {
  const auto kStaticConfigFile =
      GetRunfilesPath("micromissiles-configs-data/Models/micromissile.pbtxt");
  configs::StaticConfig static_config;
  EXPECT_EQ(LoadProtobufTextFile<configs::StaticConfig>(kStaticConfigFile,
                                                        &static_config),
            plugin::STATUS_OK);
  EXPECT_TRUE(static_config.has_acceleration_config());
  EXPECT_TRUE(static_config.has_boost_config());
  EXPECT_TRUE(static_config.has_lift_drag_config());
  EXPECT_TRUE(static_config.has_body_config());
  EXPECT_FALSE(static_config.has_hit_config());
}

TEST(ProtobufTest, LoadProtobufTextFileSimulationConfig) {
  const auto kSimulationConfigFile = GetRunfilesPath(
      "micromissiles-configs-data/Simulations/7_quadcopters.pbtxt");
  configs::SimulationConfig simulation_config;
  EXPECT_EQ(LoadProtobufTextFile<configs::SimulationConfig>(
                kSimulationConfigFile, &simulation_config),
            plugin::STATUS_OK);
  EXPECT_EQ(simulation_config.interceptor_swarm_configs().size(), 1);
  EXPECT_EQ(simulation_config.threat_swarm_configs().size(), 1);
}

TEST(ProtobufTest, LoadProtobufTextFileNullMessage) {
  const auto kSimulationConfigFile = GetRunfilesPath(
      "micromissiles-configs-data/Simulations/7_quadcopters.pbtxt");
  EXPECT_EQ(LoadProtobufTextFile<configs::SimulationConfig>(
                kSimulationConfigFile, nullptr),
            plugin::STATUS_INVALID_ARGUMENT);
}

TEST(ProtobufTest, LoadProtobufTextFileNotFound) {
  const auto kSimulationConfigFile = GetRunfilesPath(
      "micromissiles-configs-data/Simulations/nonexistent.pbtxt");
  configs::SimulationConfig simulation_config;
  EXPECT_EQ(LoadProtobufTextFile<configs::SimulationConfig>(
                kSimulationConfigFile, &simulation_config),
            plugin::STATUS_NOT_FOUND);
}

TEST(ProtobufTest, LoadProtobufTextFileInvalid) {
  const auto kSimulationConfigFile =
      std::filesystem::temp_directory_path() / "invalid.pbtxt";
  std::ofstream ofs(kSimulationConfigFile);
  ofs << "interceptor_swarm_configs { invalid: true }";
  ofs.close();

  configs::SimulationConfig simulation_config;
  EXPECT_EQ(LoadProtobufTextFile<configs::SimulationConfig>(
                kSimulationConfigFile, &simulation_config),
            plugin::STATUS_INTERNAL);
}

TEST(ProtobufTest, SerializeToBuffer) {
  const auto kStaticConfigFile =
      GetRunfilesPath("micromissiles-configs-data/Models/micromissile.pbtxt");
  configs::StaticConfig static_config;
  ASSERT_EQ(LoadProtobufTextFile<configs::StaticConfig>(kStaticConfigFile,
                                                        &static_config),
            plugin::STATUS_OK);
  std::vector<uint8_t> buffer(1024);
  std::size_t serialized_length = 0;
  EXPECT_EQ(SerializeToBuffer(static_config, buffer.data(), buffer.size(),
                              &serialized_length),
            plugin::STATUS_OK);
  EXPECT_EQ(serialized_length, static_config.ByteSizeLong());
}

TEST(ProtobufTest, SerializeToBufferNullBuffer) {
  const auto kStaticConfigFile =
      GetRunfilesPath("micromissiles-configs-data/Models/micromissile.pbtxt");
  configs::StaticConfig static_config;
  ASSERT_EQ(LoadProtobufTextFile<configs::StaticConfig>(kStaticConfigFile,
                                                        &static_config),
            plugin::STATUS_OK);
  std::vector<uint8_t> buffer(1024);
  std::size_t serialized_length = 0;
  EXPECT_EQ(SerializeToBuffer(static_config, nullptr, buffer.size(),
                              &serialized_length),
            plugin::STATUS_INVALID_ARGUMENT);
}

TEST(ProtobufTest, SerializeToBufferNullSerializedLength) {
  const auto kStaticConfigFile =
      GetRunfilesPath("micromissiles-configs-data/Models/micromissile.pbtxt");
  configs::StaticConfig static_config;
  ASSERT_EQ(LoadProtobufTextFile<configs::StaticConfig>(kStaticConfigFile,
                                                        &static_config),
            plugin::STATUS_OK);
  std::vector<uint8_t> buffer(1024);
  EXPECT_EQ(
      SerializeToBuffer(static_config, buffer.data(), buffer.size(), nullptr),
      plugin::STATUS_INVALID_ARGUMENT);
}

TEST(ProtobufTest, SerializeToBufferInsufficientSize) {
  const auto kStaticConfigFile =
      GetRunfilesPath("micromissiles-configs-data/Models/micromissile.pbtxt");
  configs::StaticConfig static_config;
  ASSERT_EQ(LoadProtobufTextFile<configs::StaticConfig>(kStaticConfigFile,
                                                        &static_config),
            plugin::STATUS_OK);
  std::vector<uint8_t> buffer(1);
  std::size_t serialized_length = 0;
  EXPECT_EQ(SerializeToBuffer(static_config, buffer.data(), buffer.size(),
                              &serialized_length),
            plugin::STATUS_FAILED_PRECONDITION);
}

}  // namespace
}  // namespace protobuf
