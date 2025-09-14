using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// Enumerations.
[JsonConverter(typeof(StringEnumConverter))]
public enum AgentClass { NONE, FIXEDWING, ROTARYWING, BALLISTIC }
[JsonConverter(typeof(StringEnumConverter))]
public enum ConfigColor { BLUE, GREEN, RED }
[JsonConverter(typeof(StringEnumConverter))]
public enum LineStyle { DOTTED, SOLID }
[JsonConverter(typeof(StringEnumConverter))]
public enum Marker { TRIANGLE_UP, TRIANGLE_DOWN, SQUARE }
[JsonConverter(typeof(StringEnumConverter))]
public enum SensorType { IDEAL }
