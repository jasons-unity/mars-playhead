using System;
using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public static class IConditionExtensions
    {
        public static Type GetDataType<T>(this T condition)
            where T: ICondition
        {
            var type = condition.GetType();
            var typeInterfaces = type.GetInterfaces();
            foreach (var typeInterface in typeInterfaces)
            {
                if (typeInterface.IsGenericType)
                {
                    if (typeInterface.GetGenericTypeDefinition() != typeof(ICondition<>))
                        continue;

                    // any type that implements ICondition<T> is guaranteed to have one generic type arg
                    var conditionDataType = typeInterface.GenericTypeArguments[0];
                    return conditionDataType;
                }
            }

            return null;
        }
    }
}
