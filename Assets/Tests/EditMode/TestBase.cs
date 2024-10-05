using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public abstract class TestBase
{
    protected void SetPrivateField<T>(object obj, string fieldName, T value)
    {
        FieldInfo field = GetFieldInfo(obj, fieldName);
        field.SetValue(obj, value);
    }

    protected T GetPrivateField<T>(object obj, string fieldName)
    {
        FieldInfo field = GetFieldInfo(obj, fieldName);
        return (T)field.GetValue(obj);
    }

    protected T InvokePrivateMethod<T>(object obj, string methodName, params object[] parameters)
    {
        MethodInfo method = GetMethodInfo(obj, methodName);
        return (T)method.Invoke(obj, parameters);
    }

    protected void InvokePrivateMethod(object obj, string methodName, params object[] parameters)
    {
        MethodInfo method = GetMethodInfo(obj, methodName);
        method.Invoke(obj, parameters);
    }

    private FieldInfo GetFieldInfo(object obj, string fieldName)
    {
        var type = obj.GetType();
        var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            throw new Exception($"Field '{fieldName}' not found in type '{type.FullName}'.");
        }
        return field;
    }

    private MethodInfo GetMethodInfo(object obj, string methodName)
    {
        var type = obj.GetType();
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            throw new Exception($"Method '{methodName}' not found in type '{type.FullName}'.");
        }
        return method;
    }
}