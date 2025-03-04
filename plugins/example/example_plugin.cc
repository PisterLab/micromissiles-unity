// The example plugin demonstrates the plugin interface.

#include <algorithm>
#include <functional>
#include <vector>

#include "example/example.h"

extern "C" {
// Add two numbers.
int Example_Add(const int a, const int b) { return example::Add(a, b); }

// Calculate the sum of the floats.
float Example_Sum(const float* values, const int length) {
  std::vector<float> values_vec(values, values + length);
  return example::Sum(values_vec);
}

// Generate a range of integers from 0 to size - 1.
void Example_Range(const int size, int* array, int* length) {
  const std::vector<int> integers = example::Range(size);
  std::copy(integers.cbegin(), integers.cend(), array);
  *length = integers.size();
}

// Execute the function on the given input and return the result.
float Example_Execute(float (*function)(float), const float input) {
  std::function<float(float)> function_wrapper(function);
  return example::Execute(function_wrapper, input);
}
}
