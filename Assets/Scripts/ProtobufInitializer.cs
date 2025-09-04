using Google.Protobuf;
using Google.Protobuf.Reflection;
using System;
using System.Collections;

// In the generated Protobuf C# code, messages are nullable reference types, so accessing missing
// message fields returns null. The Protobuf initializer iterates through all message fields and
// replaces any null message fields with empty Protobuf messages.
public static class ProtobufInitializer {
  /// <summary>
  /// Recursively initialize all null message fields in a Protobuf message to empty messages.
  /// </summary>
  public static void Initialize(IMessage message) {
    if (message == null) {
      return;
    }

    var descriptor = message.Descriptor;
    foreach (var field in descriptor.Fields.InFieldNumberOrder()) {
      if (field.FieldType != FieldType.Message) {
        continue;
      }

      var value = field.Accessor.GetValue(message);

      // Handle repeated message fields.
      if (field.IsRepeated) {
        foreach (var item in (IEnumerable)value) {
          Initialize(item as IMessage);
        }
        continue;
      }

      // If null, create a new empty message.
      if (value == null) {
        var newMessage = (IMessage)Activator.CreateInstance(field.MessageType.ClrType);
        field.Accessor.SetValue(message, newMessage);
        Initialize(newMessage);
      } else {
        Initialize(value as IMessage);
      }
    }
  }
}
