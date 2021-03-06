﻿using GDAPI.Objects.KeyedObjects;
using System;
using System.Reflection;

namespace GDAPI.Objects.Reflection
{
    /// <summary>Contains cached information about a <seealso cref="Type"/>.</summary>
    /// <typeparam name="TPropertyKey">The type of the key of the properties of this <seealso cref="Type"/>.</typeparam>
    public abstract class CachedTypeInfoBase<TPropertyKey> : IKeyedObject<Type>
    {
        /// <summary>The object type properties.</summary>
        protected PropertyInfo[] ObjectTypeProperties { get; }

        /// <summary>The object type whose info is being stored in this object.</summary>
        public Type ObjectType { get; protected set; }

        /// <summary>The default parameterless constructor of the type.</summary>
        public ConstructorInfo Constructor { get; protected set; }
        /// <summary>The properties of this type.</summary>
        public KeyedPropertyInfoDictionary<TPropertyKey> Properties { get; protected set; }

        /// <summary>The <seealso cref="Type"/> key by which this object type info is being addressed.</summary>
        Type IKeyedObject<Type>.Key => ObjectType;

        /// <summary>Initializes a new instance of the <seealso cref="CachedTypeInfoBase{TPropertyKey}"/> class.</summary>
        /// <param name="objectType">The object type whose info is being stored in this object.</param>
        public CachedTypeInfoBase(Type objectType)
        {
            ObjectType = objectType;
            Constructor = objectType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            ObjectTypeProperties = objectType.GetProperties();
            Properties = new KeyedPropertyInfoDictionary<TPropertyKey>();
            for (int i = 0; i < ObjectTypeProperties.Length; i++)
            {
                var p = CreateProperty(ObjectTypeProperties[i]);
                if (p.Key != null && !Properties.ContainsValue(p))
                    Properties.Add(p);
            }
        }

        /// <summary>Returns a <seealso cref="KeyedPropertyInfo{TKey}"/> instance from a <seealso cref="PropertyInfo"/> object.</summary>
        /// <param name="p">The <seealso cref="PropertyInfo"/> object based on which to create the returning <seealso cref="KeyedPropertyInfo{TKey}"/>.</param>
        protected abstract KeyedPropertyInfo<TPropertyKey> CreateProperty(PropertyInfo p);
    }
}
