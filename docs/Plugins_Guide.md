# Plugins Guide

This guide explains how to build and develop the C++ plugins that provide core functionality for the micromissiles simulator and allow C++ code to be executed within Unity.

## Table of Contents

[[toc]]

## Prerequisites

- A C++ compiler (GCC, Clang, or MSVC)
- Git
- Python (for Bazel)
- Bazel 8 (or Bazelisk)

## Installing Bazel

This project requires Bazel version 8.

For detailed installation instructions for your platform (Windows, macOS, Linux), please refer to the [official Bazel documentation](https://bazel.build/install).

A few important notes:

- Bazelisk is recommended as it automatically manages Bazel versions based on the `.bazelversion` file.
- On Windows, you will need additional components like MSYS2 and Visual Studio Build Tools.
- You can verify your installation with `bazel version`.

## Building the Plugins

1. Change into the `plugins` directory.
   ```bash
   cd plugins
   ```

2. Build all plugins:
   ```bash
   bazel build //...
   ```

3. Build specific plugins:
   ```bash
   # Build the assignment plugin.
   bazel build //:assignment

   # Build the example plugin.
   bazel build //:example

   # Build and package all plugins into a tarball.
   bazel build //:plugins

   # Build and package all plugins into a tarball with optimization enabled.
   bazel build -c opt //:plugins
   ```

   After building, the compiled shared libraries can be found in the `bazel-bin` directory.
   The packaged plugins tarball are located at `bazel-bin/plugins.tar.gz`.

4. Run tests:
   ```bash
   bazel test //...
   ```

## Integrating with Unity

To use these plugins with the Unity project:

1. Build the plugins using the instructions above. Ensure that you have optimization enabled to reduce the shared library size.
2. Extract the contents of `bazel-bin/plugins.tar.gz`.
3. Copy the shared libraries (`.dll`, `.dylib`, or `.so` files) to the appropriate plugins directory in the Unity project.

### Ubuntu Compatibility

Ensure that the plugin is compiled on the same Ubuntu version (e.g., 2022.04) on which the Unity project will run to maintain compability with the `glibc` and `libstdc++` standard libraries.
Unity will fail load the plugin if the plugin was compiled against a newer version of `glibc` or `libstdc++` that is not present on the current system.

Currently, the plugin is compatible with Ubuntu 22.04 or newer.

## Development

### Project Structure

All plugin-related code can be found under the `plugins/` directory.
Currently, there are two plugins:
- `assignment/`: Implements different assignment algorithms using Google's OR-Tools library to assign interceptors to threats.
- `protobuf/`: Implements loading Protobuf messages from Protobuf text files.

Other useful code can be found in the other directories:
- `base/`: Common base utilities.
- `example/`: Contains an example plugin implementation.
- `experimental/`: Experimental code, including toy examples to demonstrate various packages.

### Status Codes

Plugins should never throw exceptions as these cannot be caught by Unity and will cause the simulation to crash.
Instead, plugins should always return a status code that Unity can check and handle appropriately.

The complete list of status codes is defined in [`status.proto`](https://github.com/PisterLab/micromissiles-unity/blob/master/Assets/Proto/Plugin/status.proto) and is adapted from [Google's Abseil status codes](https://github.com/abseil/abseil-cpp/blob/master/absl/status/status.h).
Defining a Protobuf enumeration for status codes allows both the C++ plugins and the Unity C# code to reference the same set of values.
For details on the meaning and usage of each status code, refer to the [Abseil status documentation](https://github.com/abseil/abseil-cpp/blob/master/absl/status/status.h).

### Example

Because plugins are designed to return a status code rather than throw exceptions, they rely on output arguments to pass data back to Unity.
For example, the `Assignment` plugin exposes the following C API for performing an even assignment:

```cpp
// Assign the agents to the tasks using an even assignment.
plugin::StatusCode Assignment_EvenAssignment_Assign(
    const int num_agents, const int num_tasks, const float* costs,
    int* assigned_agents, int* assigned_tasks, int* num_assignments)
```

The corresponding Unity C# declaration is:
```cs
// Assign the agents to the tasks using an even assignment.
[DllImport("assignment")]
public static extern Plugin.StatusCode Assignment_EvenAssignment_Assign(
    int numAgents, int numTasks, float[] costs, int[] assignedAgents, int[] assignedTasks,
    out int numAssignments);
```

In this example:
- `costs` is a float array passed from Unity to the plugin.
- `assigned_agents`/`assignedAgents` and `assigned_tasks`/`assignedTasks` are integer arrays used as output arguments.
- `num_assignments`/`numAssignments` is an integer output argument of the plugin.

```cs
// Solve the assignment problem.
int[] assignedInterceptorIndices = new int[assignableInterceptors.Count];
int[] assignedThreatIndices = new int[assignableInterceptors.Count];
int numAssignments = 0;
Plugin.StatusCode status = Assignment.Assignment_EvenAssignment_Assign(
    assignableInterceptors.Count, activeThreats.Count, assignmentCosts,
    assignedInterceptorIndices, assignedThreatIndices, out numAssignments);
```

After the call, the returned status code should always be checked and logged if necessary.
```cs
if (status != Plugin.StatusCode.StatusOk) {
  Debug.Log($"Failed to assign the interceptors to the threats with status code {status}.");
  return assignments;
}
```
