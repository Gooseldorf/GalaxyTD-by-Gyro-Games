using System;
using System.Reflection;

namespace TestingAgent.Editor.Utils
{
    public static class Reflection
    {
        public static T GetFieldValue<T>(this object instance, string fieldName)
        {
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldName, ~BindingFlags.Default);
            
            if (field == null)
                field = type.BaseType?.GetField(fieldName, ~BindingFlags.Default);
            
            object value = field?.GetValue(instance);

            if (value != null)
                return (T)value;

            return default;
        }
        
        public static void SetFieldValue(this object instance, string fieldName, object value)
        {
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldName, ~BindingFlags.Default);
            field?.SetValue(instance, value);
        }

        public static void InvokeMethod(this object instance, string methodName, params object[] args)
        {
            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodName, ~BindingFlags.Default);
            methodInfo?.Invoke(instance, args);
        }
        
        public static T InvokeMethod<T>(this object instance, string methodName, params object[] args)
        {
            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodName, ~BindingFlags.Default);
            object invoke = methodInfo?.Invoke(instance, args);
            
            
            return (T)invoke;
        }
    }
}