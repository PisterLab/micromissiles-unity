using System;
using System.Runtime.InteropServices;

public static class Protobuf {
  // Load an attack behavior configuration from a Protobuf text file to binary format and return
  // the length of the serialized message as an output argument.
  [DllImport("protobuf")]
  public static extern Plugin.StatusCode Protobuf_AttackBehaviorConfig_LoadToBinary(
      string file, IntPtr buffer, int size, IntPtr serializedLength);

  // Load a simulation configuration from a Protobuf text file to binary format and return the
  // length of the serialized message as an output argument.
  [DllImport("protobuf")]
  public static extern Plugin.StatusCode Protobuf_SimulationConfig_LoadToBinary(
      string file, IntPtr buffer, int size, IntPtr serializedLength);

  // Load a simulator configuration from a Protobuf text file to binary format and return the length
  // of the serialized message as an output argument.
  [DllImport("protobuf")]
  public static extern Plugin.StatusCode Protobuf_SimulatorConfig_LoadToBinary(
      string file, IntPtr buffer, int size, IntPtr serializedLength);

  // Load a static configuration from a Protobuf text file to binary format and return the length of
  // the serialized message as an output argument.
  [DllImport("protobuf")]
  public static extern Plugin.StatusCode Protobuf_StaticConfig_LoadToBinary(
      string file, IntPtr buffer, int size, IntPtr serializedLength);
}
