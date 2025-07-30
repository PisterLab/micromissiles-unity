#include "utils/protobuf.h"

#include <gtest/gtest.h>

#include <string>

#include "configs/proto/static_config.pb.h"

namespace utils {
namespace {

TEST(ProtobufTest, LoadProtobufTextFileTest) {
  const std::string kStaticConfigFile =
      "configs/interceptors/micromissile.pbtxt";
  const auto static_config =
      LoadProtobufTextFile<micromissiles::StaticConfig>(kStaticConfigFile);
  EXPECT_TRUE(static_config.has_acceleration_config());
  EXPECT_TRUE(static_config.has_boost_config());
  EXPECT_TRUE(static_config.has_lift_drag_config());
  EXPECT_TRUE(static_config.has_body_config());
  EXPECT_FALSE(static_config.has_hit_config());
}

}  // namespace
}  // namespace utils
