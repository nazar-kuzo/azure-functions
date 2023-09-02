using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AzureFunctions.Authentication.Extensions
{
    internal static class TypeExtensions
    {
        private static readonly ConcurrentDictionary<Type, MemberInfo[]> TypeMembers = new ConcurrentDictionary<Type, MemberInfo[]>();

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
    }
}
