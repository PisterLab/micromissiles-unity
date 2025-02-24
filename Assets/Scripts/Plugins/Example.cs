using System;
using System.Runtime.InteropServices;

// Example usage:
//
// Debug.Log($"{Example.Example_Add(5, 7)}");
//
// float[] values = new float[] {1.0f, 2.0f, 3.5f};
// Debug.Log($"{Example.Example_Sum(values, values.Length)}");
//
// int N = 5;
// int[] range = new int[N];
// int M = 0;
// fixed (int* ptr = range) {
//     Example.Example_Range(N, (IntPtr)ptr, ref M);
// }
// Debug.Log($"{range[0]} {range[1]} {range[4]} with length {M}");
//
// float value = 6.5f;
// Debug.Log($"{Example.Example_Execute(new Example.ExecuteDelegate(Example.Double), value)}");

public class Example {
  // Define a delegate for the functional.
  public delegate float ExecuteDelegate(float value);

  // Add two numbers.
  [DllImport("libexample")]
  public static extern int Example_Add(int a, int b);

  // Calculate the sum of the floats.
  [DllImport("libexample")]
  public static extern float Example_Sum(float[] values, int length);

  // Generate a range of integers from 0 to size - 1.
  [DllImport("libexample")]
  public static extern void Example_Range(int size, IntPtr array, ref int length);

  // Execute the function on the given input and return the result.
  [DllImport("libexample")]
  public static extern float Example_Execute(ExecuteDelegate function, float input);

  // Example functional to double the input value.
  public static float Double(float input) {
    return input * 2.0f;
  }
}
