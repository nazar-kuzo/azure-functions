using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AzureFunctions.Middleware.Extensions
{
    internal static class TypeExtensions
    {
        private static readonly ConcurrentDictionary<(Type Type, string Method), MethodInfo> TypeMethods = new();
        private static readonly ConcurrentDictionary<Type, MemberInfo[]> TypeMembers = new();

        /// <summary>
        /// Gets object's property by name through Reflection.
        /// </summary>
        /// <typeparam name="TValue">Value Type.</typeparam>
        /// <param name="instance">Object instance.</param>
        /// <param name="propertyName">Name of property.</param>
        /// <returns>Retrieved value.</returns>
        public static TValue GetPropertyValue<TValue>(this object instance, string propertyName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var propertyInfo = TypeMembersProvider(instance.GetType())
                .OfType<PropertyInfo>()
                .FirstOrDefault(property => property.Name == propertyName);

            return (TValue) propertyInfo?.GetValue(instance);
        }

        /// <summary>
        /// Gets object's field by name through Reflection.
        /// </summary>
        /// <typeparam name="TValue">Value Type.</typeparam>
        /// <param name="instance">Object instance.</param>
        /// <param name="fieldName">Name of field.</param>
        /// <returns>Retrieved value.</returns>
        public static TValue GetFieldValue<TValue>(this object instance, string fieldName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var fieldInfo = TypeMembersProvider(instance.GetType())
                .OfType<FieldInfo>()
                .FirstOrDefault(field => field.Name == fieldName);

            return (TValue) fieldInfo?.GetValue(instance);
        }

        /// <summary>
        /// Sets object's field by name through Reflection.
        /// </summary>
        /// <typeparam name="TValue">Value Type.</typeparam>
        /// <param name="instance">Object instance.</param>
        /// <param name="fieldName">Name of field.</param>
        /// <param name="value">Field value.</param>
        public static void SetFieldValue<TValue>(this object instance, string fieldName, TValue value)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var fieldInfo = TypeMembersProvider(instance.GetType())
                .OfType<FieldInfo>()
                .FirstOrDefault(field => field.Name == fieldName);

            fieldInfo?.SetValue(instance, value);
        }


        /// <summary>
        /// Invokes object's method by specified name with specified parameters
        /// </summary>
        /// <param name="instance">Object to invoke method on</param>
        /// <param name="methodName">Method name</param>
        /// <param name="parameters">Method parameters</param>
        public static void InvokeMethod(this object instance, string methodName, params object[] parameters)
        {
            var concreteType = instance.GetType();

            var method = concreteType.GetPrivateMethod(methodName)
                ?? concreteType.BaseType.GetPrivateMethod(methodName);

            method.Invoke(instance, parameters);
        }

        /// <summary>
        /// Invokes object's method by specified name with specified parameters
        /// </summary>
        /// <typeparam name="TResult">Result Type</typeparam>
        /// <param name="instance">Object to invoke method on</param>
        /// <param name="methodName">Method name</param>
        /// <param name="parameters">Method parameters</param>
        /// <returns>Result of executed method</returns>
        public static TResult InvokeMethod<TResult>(this object instance, string methodName, params object[] parameters)
        {
            var concreteType = instance.GetType();

            var method = concreteType.GetPrivateMethod(methodName)
                ?? concreteType.BaseType.GetPrivateMethod(methodName);

            return (TResult) method.Invoke(instance, parameters);
        }

        private static MemberInfo[] TypeMembersProvider(Type instanceType)
        {
            return TypeMembers.GetOrAdd(
                instanceType,
                type =>
                {
                    var types = new List<Type> { instanceType };

                    if (instanceType.BaseType != null)
                    {
                        types.Add(instanceType.BaseType);
                    }

                    return types
                        .SelectMany(type => type
                            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .OfType<MemberInfo>()
                            .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)))
                        .ToArray();
                });
        }

        private static MethodInfo GetPrivateMethod(this Type type, string methodName)
        {
            return TypeMethods.GetOrAdd(
                (type, methodName),
                _ =>
                {
                    return type.GetMethod(
                        methodName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                });
        }
    }
}
