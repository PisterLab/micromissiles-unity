#include "example/example.h"

#include <cstddef>
#include <functional>
#include <numeric>
#include <vector>

namespace example {

int Add(const int a, const int b) { return a + b; }

float Sum(const std::vector<float>& values) {
  return std::reduce(values.cbegin(), values.cend());
}

std::vector<int> Range(const int size) {
  if (size <= 0) {
    return std::vector<int>();
  }
  std::vector<int> integers(size);
  std::iota(integers.begin(), integers.end(), 0);
  return integers;
}

float Execute(const std::function<float(float)>& function, const float input) {
  return function(input);
}

}  // namespace example
