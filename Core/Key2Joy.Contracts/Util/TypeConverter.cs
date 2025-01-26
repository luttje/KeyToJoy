using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Key2Joy.Contracts.Plugins;

namespace Key2Joy.Contracts.Util;

public static class TypeConverter
{
    /// <summary>
    /// Copies a given array to a new array of the target type, e.g: from object[] to string[].
    /// </summary>
    /// <param name="originalArray"></param>
    /// <param name="elementType"></param>
    /// <returns></returns>
    public static Array CopyArrayToNewType(this object[] originalArray, Type elementType)
    {
        var newArray = Array.CreateInstance(elementType, originalArray.Length);

        for (var i = 0; i < originalArray.Length; i++)
        {
            newArray.SetValue(Convert.ChangeType(originalArray[i], elementType), i);
        }

        return newArray;

        // Explored alternatives:
        // B: This doesn't actually cast it tot he correct type (but leaves it an object[]
        //var convertedArray = (Array)Array.ConvertAll(originalArray, item => Convert.ChangeType(item, elementType));

        //return convertedArray;

        // C: Dynamically invoke Array.ConvertAll so that it returns an array of type elementType[]
        //var convertAllMethod = typeof(Array).GetMethod("ConvertAll", BindingFlags.Public | BindingFlags.Static);
        //var genericConvertAllMethod = convertAllMethod.MakeGenericMethod(typeof(object), elementType);
        //var converterDelegateType = typeof(Func<,>).MakeGenericType(typeof(object), elementType);

        //return (Array)genericConvertAllMethod.Invoke(
        //    null,
        //    new object[] {
        //        originalArray,
        //        Delegate.CreateDelegate(converterDelegateType, typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) }))
        //    });
    }

    /// <summary>
    /// Converts the given object to the desired type
    /// </summary>
    /// <param name="value"></param>
    /// <param name="desiredType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static object ConvertToType(object value, Type desiredType)
    {
        var genericTypeDefinition = desiredType.IsGenericType ? desiredType.GetGenericTypeDefinition() : null;
        desiredType = Nullable.GetUnderlyingType(desiredType) ?? desiredType;

        if (value == null)
        {
            value = null;
        }
        else if (desiredType.IsEnum)
        {
            try
            {
                value = Enum.Parse(desiredType, value.ToString());
            }
            catch (ArgumentException)
            {
                Output.WriteLine($"Could not parse value '{value}' to enum '{desiredType}'. Using first value instead.");
                value = Enum.GetValues(desiredType).GetValue(0);
            }
        }
        else if (desiredType == typeof(DateTime))
        {
            value = DateTime.Parse((string)value);
        }
        else if (desiredType == typeof(TimeSpan))
        {
            value = TimeSpan.Parse((string)value);
        }
        else if (desiredType == typeof(short))
        {
            value = Convert.ToInt16(value);
        }
        else if (desiredType.IsGenericType
            && (genericTypeDefinition == typeof(List<>) || genericTypeDefinition == typeof(IList<>)))
        {
            var constructedListType = typeof(List<>).MakeGenericType(desiredType.GetGenericArguments());
            var instance = Activator.CreateInstance(constructedListType);

            if (value is List<object> list)
            {
                var addMethod = constructedListType.GetMethod("Add");

                foreach (var item in list)
                {
                    addMethod.Invoke(instance, new object[] { item });
                }

                value = instance;
            }
            else
            {
                throw new ArgumentException($"Expected value to be of type List<> to parse. But was: {value.GetType()}");
            }
        }
        else if (value is object[] objectArrayParameter && desiredType.IsArray)
        {
            // TODO: This breaks the reference to the original array
            // TODO: Inform plugin creators that if they want to keep the original reference, they should
            //       use the 'object' type and cast the array to the correct type themselves.
            value = objectArrayParameter.CopyArrayToNewType(desiredType.GetElementType());
        }
        // If a local action is using a callback, it'll use CallbackAction instead of the wrapper,
        // we'll convert it to that. Otherwise we leave it wrapped so it can be called across the
        // domain boundary.
        else if (desiredType == typeof(CallbackAction)
            && value is CallbackActionWrapper callbackActionWrapper)
        {
            value = callbackActionWrapper.AsCallbackAction();
        }
        // We don't touch this, since we can't convert it anyway (it's across the domain boundary)
        else if (value is MarshalByRefObject or ISerializable)
        {
            return value;
        }
        //else if (value.GetType().IsSerializable)
        //{
        //    return value;
        //}
        else
        {
            value = Convert.ChangeType(value, desiredType);
        }

        return value;
    }
}
