// The example namespace contains some example functions to demonstrate the
// plugin interface.

#pragma once

#include <functional>
#include <vector>

namespace example {

// Add two numbers.
int Add(int a, int b);

// Calculate the sum of the floats.
float Sum(const std::vector<float>& values);

// Generate a range of integers from 0 to size - 1.
std::vector<int> Range(int size);

// Execute the function on the given input and return the result.
float Execute(const std::function<float(float)>& function, float input);

}  // namespace example
