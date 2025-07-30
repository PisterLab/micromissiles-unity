using System;
using System.Runtime.InteropServices;

public class Protobuf {
  // Load a static configuration from a Protobuf text file to binary format and return the length of
  // the serialized message.
  [DllImport("protobuf")]
  public static extern int Protobuf_StaticConfig_LoadToBinary(string file, IntPtr buffer, int size);

  // Load a simulator configuration from a Protobuf text file to binary format and return the length
  // of the serialized message.
  [DllImport("protobuf")]
  public static extern int Protobuf_SimulatorConfig_LoadToBinary(string file, IntPtr buffer,
                                                                 int size);
}
