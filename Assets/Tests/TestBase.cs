using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;

public abstract class TestBase {
  protected void SetPrivateField<T>(object obj, string fieldName, T value) {
    FieldInfo field = GetFieldInfo(obj, fieldName);
    field.SetValue(obj, value);
  }

  protected T GetPrivateField<T>(object obj, string fieldName) {
    FieldInfo field = GetFieldInfo(obj, fieldName);
    return (T)field.GetValue(obj);
  }

  protected T InvokePrivateMethod<T>(object obj, string methodName, params object[] parameters) {
    MethodInfo method = GetMethodInfo(obj, methodName);
    return (T)method.Invoke(obj, parameters);
  }

  protected void InvokePrivateMethod(object obj, string methodName, params object[] parameters) {
    MethodInfo method = GetMethodInfo(obj, methodName);
    method.Invoke(obj, parameters);
  }

  protected void SetPrivateProperty<T>(object obj, string propertyName, T value) {
    PropertyInfo property = GetPropertyInfo(obj, propertyName);
    property.SetValue(obj, value);
  }

  protected T GetPrivateProperty<T>(object obj, string propertyName) {
    PropertyInfo property = GetPropertyInfo(obj, propertyName);
    return (T)property.GetValue(obj);
  }

  private FieldInfo GetFieldInfo(object obj, string fieldName) {
    var type = obj.GetType();
    var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

    // If not found in the immediate type, search the inheritance hierarchy.
    while (field == null && type.BaseType != null) {
      type = type.BaseType;
      field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
    }

    if (field == null) {
      throw new Exception(
          $"Field '{fieldName}' not found in type hierarchy of '{obj.GetType().FullName}'.");
    }
    return field;
  }

  private MethodInfo GetMethodInfo(object obj, string methodName) {
    var type = obj.GetType();
    var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
    if (method == null) {
      throw new Exception($"Method '{methodName}' not found in type '{type.FullName}'.");
    }
    return method;
  }

  private PropertyInfo GetPropertyInfo(object obj, string propertyName) {
    var type = obj.GetType();
    var property = type.GetProperty(
        propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    // If not found in the immediate type, search the inheritance hierarchy.
    while (property == null && type.BaseType != null) {
      type = type.BaseType;
      property = type.GetProperty(
          propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }

    if (property == null) {
      throw new Exception(
          $"Property '{propertyName}' not found in type hierarchy of '{obj.GetType().FullName}'.");
    }
    return property;
  }
}
