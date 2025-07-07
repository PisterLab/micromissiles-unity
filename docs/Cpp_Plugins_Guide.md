# C++ Plugins Guide

This guide explains how to build and develop the C++ plugins that provide core functionality for the micromissiles simulator.

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

4. Run tests:
   ```bash
   bazel test //...
   ```

## Project Structure

- `/assignment`: Contains the assignment plugin implementation.
- `/example`: Contains an example plugin implementation.
- `/base`: Common base utilities.

## Output Files

After building, the compiled shared libraries can be found in the `bazel-bin` directory. The packaged plugins tarball will be at `bazel-bin/plugins.tar.gz`.

## Integrating with Unity

To use these plugins with the micromissiles Unity project:

1. Build the plugins using the instructions above. Ensure that you have optimization enabled to reduce the shared library size.
2. Extract the contents of `bazel-bin/plugins.tar.gz`.
3. Copy the shared libraries (`.dll`, `.dylib`, or `.so` files) to the appropriate plugins directory in the Unity project.

### Ubuntu Compatibility

Ensure that the plugin is compiled on the same Ubuntu version (e.g., 2022.04) on which the Unity project will run to maintain compability with the `glibc` and `libstdc++` standard libraries.
Unity will fail load the plugin if the plugin was compiled against a newer version of `glibc` or `libstdc++` that is not present on the current system.

Currently, the plugin is compatible with Ubuntu 22.04 or newer.
