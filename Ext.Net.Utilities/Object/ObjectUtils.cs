﻿/*
 * @version   : 2.3.0
 * @author    : Ext.NET, Inc. http://www.ext.net/
 * @date      : 2013-10-04
 * @copyright : Copyright (c) 2008-2013, Ext.NET, Inc. (http://www.ext.net/). All rights reserved.
 * @license   : See license.txt and http://www.ext.net/license/. 
 * @website   : http://www.ext.net/
 */

using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Web.UI.WebControls;

namespace Ext.Net.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public static class ObjectUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="valueIfNull"></param>
        /// <returns></returns>
        public static T IfNull<T>(this T value, T valueIfNull)
        {
            return value.If<T>(delegate() { return value.IsNull(); }, valueIfNull, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="test"></param>
        /// <param name="valueIfTrue"></param>
        /// <param name="valueIfFalse"></param>
        /// <returns></returns>
        public static T If<T>(this T value, Func<bool> test, T valueIfTrue, T valueIfFalse)
        {
            return test() ? valueIfTrue : valueIfFalse;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="test"></param>
        /// <param name="valueIfTrue"></param>
        /// <param name="valueIfFalse"></param>
        /// <returns></returns>
        public static T IfNot<T>(this T value, Func<bool> test, T valueIfTrue, T valueIfFalse)
        {
            return !test() ? valueIfTrue : valueIfFalse;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static bool IsNull(this object instance)
        {
            return instance == null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <returns></returns>
        public static T Apply<T>(object to, object from) where T : IComponent
        {
            return (T)ObjectUtils.Apply(to, from);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <returns></returns>
        public static object Apply(object to, object from)
        {
            return ObjectUtils.Apply(to, from, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <param name="ignoreDefaultValues"></param>
        /// <returns></returns>
        public static object Apply(object to, object from, bool ignoreDefaultValues)
        {
            System.Reflection.PropertyInfo toProperty;

            object fromValue = null;
            object defaultValue = null;

            foreach (PropertyInfo fromProperty in from.GetType().GetProperties())
            {
                if (fromProperty.CanRead)
                {
                    fromValue = fromProperty.GetValue(from, null);

                    if (ignoreDefaultValues)
                    {
                        defaultValue = Ext.Net.Utilities.ReflectionUtils.GetDefaultValue(fromProperty);

                        if (fromValue != null && fromValue.Equals(defaultValue))
                        {
                            continue;
                        }
                    }

                    if (fromValue != null)
                    {
                        toProperty = to.GetType().GetProperty(fromProperty.Name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                        if (toProperty != null && toProperty.GetType().Equals(fromProperty.GetType()))
                        {
                            Type toPropertyType = toProperty.PropertyType;

                            if (toProperty.CanWrite)
                            {
                                if (toPropertyType == typeof(Unit) && fromValue is int)
                                {
                                    fromValue = Unit.Pixel((int)fromValue);
                                }

                                toProperty.SetValue(to, fromValue, null);
                            }
                            else if (toProperty.PropertyType.GetInterface("IList") != null)
                            {
                                IList toList = toProperty.GetValue(to, null) as IList;
                                IList fromList = fromProperty.GetValue(from, null) as IList;

                                if (fromList.Count == 0)
                                {
                                    continue;
                                }

                                // method Add can be shadowed 
                                // shadowed method will not be called via IList reference therefore we have to find last shadowed method
                                Type genericItemType = typeof(object);
                                if (toPropertyType.IsGenericType)
                                {
                                    Type[] type = toPropertyType.GetGenericArguments();
                                    if (type != null && type.Length == 1)
                                    {
                                        genericItemType = type[0];
                                    }
                                }
                                else
                                {
                                    foreach (Type intType in toPropertyType.GetInterfaces())
                                    {
                                        if (intType.IsGenericType
                                            && intType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                        {
                                            genericItemType = intType.GetGenericArguments()[0];
                                            break;
                                        }
                                    }
                                }

                                MethodInfo method = null;
                                toPropertyType.GetMethods().Each(m =>
                                {
                                    if (m.Name == "Add")
                                    {
                                        ParameterInfo[] prms = m.GetParameters();
                                        if (prms != null && prms.Length == 1 && prms[0].ParameterType.IsAssignableFrom(genericItemType))
                                        {
                                            if (method == null || m.DeclaringType.IsSubclassOf(method.DeclaringType))
                                            {
                                                method = m;
                                            }
                                        }
                                    }
                                });

                                foreach (object item in fromList)
                                {
                                    if (method != null)
                                    {
                                        method.Invoke(toList, new object[] { item });
                                    }
                                    else
                                    {
                                        toList.Add(item);
                                    }
                                }
                            }
                            else if (toProperty.PropertyType.GetInterface("IDictionary") != null)
                            {
                                IDictionary toDict = toProperty.GetValue(to, null) as IDictionary;
                                IDictionary fromDict = fromProperty.GetValue(from, null) as IDictionary;

                                if (fromDict.Count == 0)
                                {
                                    continue;
                                }

                                foreach (DictionaryEntry item in fromDict)
                                {
                                    toDict.Add(item.Key, item.Value);
                                }
                            }
                        }
                    }
                }
            }

            return to;
        }
    }
}