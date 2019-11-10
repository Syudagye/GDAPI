﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GDAPI.Enumerations.GeometryDash;
using GDAPI.Functions.Extensions;
using GDAPI.Information.GeometryDash;
using GDAPI.Objects.DataStructures;
using GDAPI.Objects.GeometryDash.General;
using GDAPI.Objects.GeometryDash.LevelObjects.Triggers;
using GDAPI.Objects.GeometryDash.Reflection;

namespace GDAPI.Objects.GeometryDash.LevelObjects
{
    /// <summary>Represents a collection of level objects.</summary>
    public class LevelObjectCollection : IEnumerable<GeneralObject>
    {
        private int triggerCount = -1;
        private int colorTriggerCount = -1;

        private PropertyAccessInfoDictionary commonProperties;
        private PropertyAccessInfoDictionary allAvailableProperties;

        private int commonPropertiesUnevaluatedIndex;
        private int allAvailablePropertiesUnevaluatedIndex;
        private NestedLists<GeneralObject> unevaluatedObjects = new NestedLists<GeneralObject>();

        private List<GeneralObject> objects;

        /// <summary>The count of the level objects in the collection.</summary>
        public int Count => objects.Count;

        /// <summary>The list of objects in the collection.</summary>
        public List<GeneralObject> Objects
        {
            get => objects;
            set
            {
                ResetCounters();
                objects = value;
                ObjectCounts.Clear();
                GroupCounts.Clear();
                ClearPropertyCache();
            }
        }

        /// <summary>The count of all the triggers in the collection (excludes Start Pos).</summary>
        public int TriggerCount
        {
            get
            {
                if (triggerCount == -1)
                {
                    triggerCount = 0;
                    foreach (var kvp in ObjectCounts)
                        if (ObjectLists.TriggerList.Contains(kvp.Key))
                            triggerCount += kvp.Value;
                }
                return triggerCount;
            }
        }
        /// <summary>The count of all the color triggers in the collection.</summary>
        public int ColorTriggerCount
        {
            get
            {
                // TODO: Simplify this like the TriggerCount property
                if (colorTriggerCount == -1)
                {
                    colorTriggerCount = ObjectCounts.ValueOrDefault((int)TriggerType.Color);
                    colorTriggerCount += ObjectCounts.ValueOrDefault((int)TriggerType.BG);
                    colorTriggerCount += ObjectCounts.ValueOrDefault((int)TriggerType.GRND);
                    colorTriggerCount += ObjectCounts.ValueOrDefault((int)TriggerType.GRND2);
                    colorTriggerCount += ObjectCounts.ValueOrDefault((int)TriggerType.Line);
                    colorTriggerCount += ObjectCounts.ValueOrDefault((int)TriggerType.Obj);
                    colorTriggerCount += ObjectCounts.ValueOrDefault((int)TriggerType.ThreeDL);
                    colorTriggerCount += ObjectCounts.ValueOrDefault((int)TriggerType.Color1);
                    colorTriggerCount += ObjectCounts.ValueOrDefault((int)TriggerType.Color2);
                    colorTriggerCount += ObjectCounts.ValueOrDefault((int)TriggerType.Color3);
                    colorTriggerCount += ObjectCounts.ValueOrDefault((int)TriggerType.Color4);
                }
                return colorTriggerCount;
            }
        }
        /// <summary>Contains the count of objects per object ID in the collection.</summary>
        public Dictionary<int, int> ObjectCounts { get; private set; } = new Dictionary<int, int>();
        /// <summary>Contains the count of groups per object ID in the collection.</summary>
        public Dictionary<int, int> GroupCounts { get; private set; } = new Dictionary<int, int>();
        /// <summary>The different object IDs in the collection.</summary>
        public int DifferentObjectIDCount => ObjectCounts.Keys.Count;
        /// <summary>The different object IDs in the collection.</summary>
        public int[] DifferentObjectIDs => ObjectCounts.Keys.ToArray();
        /// <summary>The group IDs in the collection.</summary>
        public int[] UsedGroupIDs => GroupCounts.Keys.ToArray();
        #region Trigger info
        public int MoveTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Move);
        public int StopTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Stop);
        public int PulseTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Pulse);
        public int AlphaTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Alpha);
        public int ToggleTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Toggle);
        public int SpawnTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Spawn);
        public int CountTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Count);
        public int InstantCountTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.InstantCount);
        public int PickupTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Pickup);
        public int FollowTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Follow);
        public int FollowPlayerYTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.FollowPlayerY);
        public int TouchTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Touch);
        public int AnimateTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Animate);
        public int RotateTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Rotate);
        public int ShakeTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Shake);
        public int CollisionTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.Collision);
        public int OnDeathTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.OnDeath);
        public int HidePlayerTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.HidePlayer);
        public int ShowPlayerTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.ShowPlayer);
        public int DisableTrailTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.DisableTrail);
        public int EnableTrailTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.EnableTrail);
        public int BGEffectOnTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.BGEffectOn);
        public int BGEffectOffTriggersCount => ObjectCounts.ValueOrDefault((int)TriggerType.BGEffectOff);
        #endregion

        /// <summary>Initializes a new instance of the <seealso cref="LevelObjectCollection"/> class.</summary>
        public LevelObjectCollection()
        {
            Objects = new List<GeneralObject>();
        }
        /// <summary>Initializes a new instance of the <seealso cref="LevelObjectCollection"/> class.</summary>
        /// <param name="obj">The object to use.</param>
        public LevelObjectCollection(GeneralObject obj)
        {
            Objects = new List<GeneralObject> { obj };
        }
        /// <summary>Initializes a new instance of the <seealso cref="LevelObjectCollection"/> class.</summary>
        /// <param name="objects">The list of objects to use.</param>
        public LevelObjectCollection(List<GeneralObject> objects)
        {
            Objects = objects;
        }

        /// <summary>Adds an object to the <seealso cref="LevelObjectCollection"/>.</summary>
        /// <param name="o">The object to add.</param>
        public LevelObjectCollection Add(GeneralObject o)
        {
            AddToCounters(o);
            objects.Add(o);
            RegisterUnevaluatedObject(o);
            return this;
        }
        /// <summary>Adds a collection of objects from the <seealso cref="LevelObjectCollection"/>.</summary>
        /// <param name="addedObjects">The objects to add.</param>
        public LevelObjectCollection AddRange(IEnumerable<GeneralObject> addedObjects)
        {
            AddToCounters(addedObjects);
            objects.AddRange(addedObjects);
            RegisterUnevaluatedObjects(addedObjects);
            return this;
        }
        /// <summary>Adds a collection of objects from the <seealso cref="LevelObjectCollection"/>.</summary>
        /// <param name="objects">The objects to add.</param>
        public LevelObjectCollection AddRange(LevelObjectCollection objects) => AddRange(objects.Objects);
        /// <summary>Adds a collection of objects from the <seealso cref="LevelObjectCollection"/>.</summary>
        /// <param name="objects">The objects to add.</param>
        public LevelObjectCollection AddRange(params GeneralObject[] objects) => AddRange((IEnumerable<GeneralObject>)objects);
        /// <summary>Inserts an object to the <seealso cref="LevelObjectCollection"/>.</summary>
        /// <param name="index">The index to insert the object at.</param>
        /// <param name="o">The object to insert.</param>
        public LevelObjectCollection Insert(int index, GeneralObject o)
        {
            AddToCounters(o);
            objects.Insert(index, o);
            RegisterUnevaluatedObject(o);
            return this;
        }
        /// <summary>Inserts a collection of objects to the <seealso cref="LevelObjectCollection"/>.</summary>
        /// <param name="index">The index of the first object to insert at.</param>
        /// <param name="insertedObjects">The objects to insert.</param>
        public LevelObjectCollection InsertRange(int index, List<GeneralObject> insertedObjects)
        {
            AddToCounters(insertedObjects);
            objects.InsertRange(index, insertedObjects);
            RegisterUnevaluatedObjects(insertedObjects);
            return this;
        }
        /// <summary>Inserts a collection of objects to the <seealso cref="LevelObjectCollection"/>.</summary>
        /// <param name="index">The index of the first object to insert at.</param>
        /// <param name="objects">The objects to insert.</param>
        public LevelObjectCollection InsertRange(int index, LevelObjectCollection objects) => InsertRange(index, objects.Objects);
        /// <summary>Removes an object from the <seealso cref="LevelObjectCollection"/>.</summary>
        /// <param name="o">The object to remove.</param>
        public LevelObjectCollection Remove(GeneralObject o)
        {
            RemoveFromCounters(o);
            objects.Remove(o);
            ClearPropertyCache();
            return this;
        }
        /// <summary>Removes an object from the <seealso cref="LevelObjectCollection"/>.</summary>
        /// <param name="index">The index of the object to remove.</param>
        public LevelObjectCollection RemoveAt(int index)
        {
            RemoveFromCounters(objects[index]);
            objects.RemoveAt(index);
            ClearPropertyCache();
            return this;
        }
        /// <summary>Removes a collection of objects from the <seealso cref="LevelObjectCollection"/>.</summary>
        /// <param name="removedObjects">The objects to remove.</param>
        public LevelObjectCollection RemoveRange(List<GeneralObject> removedObjects)
        {
            foreach (var o in removedObjects)
            {
                RemoveFromCounters(o);
                objects.Remove(o);
            }
            ClearPropertyCache();
            return this;
        }
        /// <summary>Removes a collection of objects from the <seealso cref="LevelObjectCollection"/>.</summary>
        /// <param name="objects">The objects to remove.</param>
        public LevelObjectCollection RemoveRange(LevelObjectCollection objects) => RemoveRange(objects.Objects);
        /// <summary>Clears the <seealso cref="LevelObjectCollection"/>.</summary>
        public LevelObjectCollection Clear()
        {
            ObjectCounts.Clear();
            GroupCounts.Clear();
            objects.Clear();
            SetPropertyCacheToDefault();
            return this;
        }
        /// <summary>Clones the <seealso cref="LevelObjectCollection"/> and returns the cloned instance.</summary>
        public LevelObjectCollection Clone()
        {
            var result = new LevelObjectCollection();
            result.ObjectCounts = ObjectCounts.Clone();
            result.GroupCounts = GroupCounts.Clone();
            result.objects = objects.Clone();
            result.allAvailableProperties = new PropertyAccessInfoDictionary(allAvailableProperties);
            result.commonProperties = new PropertyAccessInfoDictionary(commonProperties);
            return result;
        }

        /// <summary>Attempts to get the common value of an object property from this collection of objects given its ID.</summary>
        /// <typeparam name="T">The value type of the property.</typeparam>
        /// <param name="ID">The ID of the property.</param>
        /// <param name="common">The common value of the property.</param>
        public bool TryGetCommonPropertyWithID<T>(int ID, out T common) => GeneralObject.TryGetCommonPropertyWithID(this, ID, out common);
        /// <summary>Attempts to set the common value of an object property from this collection of objects given its ID.</summary>
        /// <typeparam name="T">The value type of the property.</typeparam>
        /// <param name="ID">The ID of the property.</param>
        /// <param name="newValue">The new value of the property to set to all the objects.</param>
        public bool TrySetCommonPropertyWithID<T>(int ID, T newValue) => GeneralObject.TrySetCommonPropertyWithID(this, ID, newValue);
        /// <summary>Attempts to get the common value of an object property from this collection of objects given its ID.</summary>
        /// <typeparam name="T">The value type of the property.</typeparam>
        /// <param name="ID">The ID of the property.</param>
        /// <param name="common">The common value of the property.</param>
        public bool TryGetCommonPropertyWithID<T>(ObjectProperty ID, out T common) => TryGetCommonPropertyWithID((int)ID, out common);
        /// <summary>Attempts to set the common value of an object property from this collection of objects given its ID.</summary>
        /// <typeparam name="T">The value type of the property.</typeparam>
        /// <param name="ID">The ID of the property.</param>
        /// <param name="newValue">The new value of the property to set to all the objects.</param>
        public bool TrySetCommonPropertyWithID<T>(ObjectProperty ID, T newValue) => TrySetCommonPropertyWithID((int)ID, newValue);
        /// <summary>Gets the common value of an object property from this collection of objects given its ID. Throws an <seealso cref="InvalidOperationException"/> if the retrieval failed.</summary>
        /// <typeparam name="T">The value type of the property.</typeparam>
        /// <param name="ID">The ID of the property.</param>
        /// <exception cref="InvalidOperationException"/>
        public T GetCommonPropertyWithID<T>(int ID)
        {
            if (TryGetCommonPropertyWithID(ID, out T result))
                return result;
            throw new InvalidOperationException($"Cannot get the common property with ID {ID}.");
        }
        /// <summary>Sets the common value of an object property from this collection of objects given its ID. Throws an <seealso cref="InvalidOperationException"/> if the retrieval failed.</summary>
        /// <typeparam name="T">The value type of the property.</typeparam>
        /// <param name="ID">The ID of the property.</param>
        /// <param name="newValue">The new value of the property to set to all the objects.</param>
        /// <exception cref="InvalidOperationException"/>
        public void SetCommonPropertyWithID<T>(int ID, T newValue)
        {
            if (!TrySetCommonPropertyWithID(ID, newValue))
                throw new InvalidOperationException($"Cannot set the common property with ID {ID}.");
        }
        /// <summary>Gets the common value of an object property from this collection of objects given its ID. Throws an <seealso cref="InvalidOperationException"/> if the retrieval failed.</summary>
        /// <typeparam name="T">The value type of the property.</typeparam>
        /// <param name="ID">The ID of the property.</param>
        /// <exception cref="InvalidOperationException"/>
        public T GetCommonPropertyWithID<T>(ObjectProperty ID) => GetCommonPropertyWithID<T>((int)ID);
        /// <summary>Sets the common value of an object property from this collection of objects given its ID. Throws an <seealso cref="InvalidOperationException"/> if the retrieval failed.</summary>
        /// <typeparam name="T">The value type of the property.</typeparam>
        /// <param name="ID">The ID of the property.</param>
        /// <param name="newValue">The new value of the property to set to all the objects.</param>
        /// <exception cref="InvalidOperationException"/>
        public void SetCommonPropertyWithID<T>(ObjectProperty ID, T newValue) => SetCommonPropertyWithID((int)ID, newValue);

        #region Object Properties
        // The code below was proudly automatically generated (no, the documentation will not be manually touched by me because fuck you)
        /// <summary>Gets or sets the common ID property of the objects in this collection.</summary>
        public int CommonID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.ID);
            set => SetCommonPropertyWithID(ObjectProperty.ID, value);
        }
        /// <summary>Gets or sets the common X property of the objects in this collection.</summary>
        public double CommonX
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.X);
            set => SetCommonPropertyWithID(ObjectProperty.X, value);
        }
        /// <summary>Gets or sets the common Y property of the objects in this collection.</summary>
        public double CommonY
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.Y);
            set => SetCommonPropertyWithID(ObjectProperty.Y, value);
        }
        /// <summary>Gets or sets the common FlippedHorizontally property of the objects in this collection.</summary>
        public bool CommonFlippedHorizontally
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.FlippedHorizontally);
            set => SetCommonPropertyWithID(ObjectProperty.FlippedHorizontally, value);
        }
        /// <summary>Gets or sets the common FlippedVertically property of the objects in this collection.</summary>
        public bool CommonFlippedVertically
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.FlippedVertically);
            set => SetCommonPropertyWithID(ObjectProperty.FlippedVertically, value);
        }
        /// <summary>Gets or sets the common Rotation property of the objects in this collection.</summary>
        public double CommonRotation
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.Rotation);
            set => SetCommonPropertyWithID(ObjectProperty.Rotation, value);
        }
        /// <summary>Gets or sets the common Red property of the objects in this collection.</summary>
        public int CommonRed
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.Red);
            set => SetCommonPropertyWithID(ObjectProperty.Red, value);
        }
        /// <summary>Gets or sets the common Green property of the objects in this collection.</summary>
        public int CommonGreen
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.Green);
            set => SetCommonPropertyWithID(ObjectProperty.Green, value);
        }
        /// <summary>Gets or sets the common Blue property of the objects in this collection.</summary>
        public int CommonBlue
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.Blue);
            set => SetCommonPropertyWithID(ObjectProperty.Blue, value);
        }
        /// <summary>Gets or sets the common Duration property of the objects in this collection.</summary>
        public double CommonDuration
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.Duration);
            set => SetCommonPropertyWithID(ObjectProperty.Duration, value);
        }
        /// <summary>Gets or sets the common TouchTriggered property of the objects in this collection.</summary>
        public bool CommonTouchTriggered
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.TouchTriggered);
            set => SetCommonPropertyWithID(ObjectProperty.TouchTriggered, value);
        }
        /// <summary>Gets or sets the common SecretCoinID property of the objects in this collection.</summary>
        public int CommonSecretCoinID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.SecretCoinID);
            set => SetCommonPropertyWithID(ObjectProperty.SecretCoinID, value);
        }
        /// <summary>Gets or sets the common SpecialObjectChecked property of the objects in this collection.</summary>
        public bool CommonSpecialObjectChecked
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.SpecialObjectChecked);
            set => SetCommonPropertyWithID(ObjectProperty.SpecialObjectChecked, value);
        }
        /// <summary>Gets or sets the common TintGround property of the objects in this collection.</summary>
        public bool CommonTintGround
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.TintGround);
            set => SetCommonPropertyWithID(ObjectProperty.TintGround, value);
        }
        /// <summary>Gets or sets the common SetColorToPlayerColor1 property of the objects in this collection.</summary>
        public bool CommonSetColorToPlayerColor1
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.SetColorToPlayerColor1);
            set => SetCommonPropertyWithID(ObjectProperty.SetColorToPlayerColor1, value);
        }
        /// <summary>Gets or sets the common SetColorToPlayerColor2 property of the objects in this collection.</summary>
        public bool CommonSetColorToPlayerColor2
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.SetColorToPlayerColor2);
            set => SetCommonPropertyWithID(ObjectProperty.SetColorToPlayerColor2, value);
        }
        /// <summary>Gets or sets the common Blending property of the objects in this collection.</summary>
        public bool CommonBlending
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.Blending);
            set => SetCommonPropertyWithID(ObjectProperty.Blending, value);
        }
        /// <summary>Gets or sets the common EL1 property of the objects in this collection.</summary>
        public int CommonEL1
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.EL1);
            set => SetCommonPropertyWithID(ObjectProperty.EL1, value);
        }
        /// <summary>Gets or sets the common Color1 property of the objects in this collection.</summary>
        public int CommonColor1
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.Color1);
            set => SetCommonPropertyWithID(ObjectProperty.Color1, value);
        }
        /// <summary>Gets or sets the common Color2 property of the objects in this collection.</summary>
        public int CommonColor2
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.Color2);
            set => SetCommonPropertyWithID(ObjectProperty.Color2, value);
        }
        /// <summary>Gets or sets the common TargetColorID property of the objects in this collection.</summary>
        public int CommonTargetColorID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.TargetColorID);
            set => SetCommonPropertyWithID(ObjectProperty.TargetColorID, value);
        }
        /// <summary>Gets or sets the common ZLayer property of the objects in this collection.</summary>
        public int CommonZLayer
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.ZLayer);
            set => SetCommonPropertyWithID(ObjectProperty.ZLayer, value);
        }
        /// <summary>Gets or sets the common ZOrder property of the objects in this collection.</summary>
        public int CommonZOrder
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.ZOrder);
            set => SetCommonPropertyWithID(ObjectProperty.ZOrder, value);
        }
        /// <summary>Gets or sets the common OffsetX property of the objects in this collection.</summary>
        public double CommonOffsetX
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.OffsetX);
            set => SetCommonPropertyWithID(ObjectProperty.OffsetX, value);
        }
        /// <summary>Gets or sets the common OffsetY property of the objects in this collection.</summary>
        public double CommonOffsetY
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.OffsetY);
            set => SetCommonPropertyWithID(ObjectProperty.OffsetY, value);
        }
        /// <summary>Gets or sets the common Easing property of the objects in this collection.</summary>
        public Easing CommonEasing
        {
            get => GetCommonPropertyWithID<Easing>(ObjectProperty.Easing);
            set => SetCommonPropertyWithID(ObjectProperty.Easing, value);
        }
        /// <summary>Gets or sets the common TextObjectText property of the objects in this collection.</summary>
        public string CommonTextObjectText
        {
            get => GetCommonPropertyWithID<string>(ObjectProperty.TextObjectText);
            set => SetCommonPropertyWithID(ObjectProperty.TextObjectText, value);
        }
        /// <summary>Gets or sets the common Scaling property of the objects in this collection.</summary>
        public double CommonScaling
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.Scaling);
            set => SetCommonPropertyWithID(ObjectProperty.Scaling, value);
        }
        /// <summary>Gets or sets the common GroupParent property of the objects in this collection.</summary>
        public bool CommonGroupParent
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.GroupParent);
            set => SetCommonPropertyWithID(ObjectProperty.GroupParent, value);
        }
        /// <summary>Gets or sets the common Opacity property of the objects in this collection.</summary>
        public double CommonOpacity
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.Opacity);
            set => SetCommonPropertyWithID(ObjectProperty.Opacity, value);
        }
        /// <summary>Gets or sets the common UnknownFeature36 property of the objects in this collection.</summary>
        public bool CommonUnknownFeature36
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.UnknownFeature36);
            set => SetCommonPropertyWithID(ObjectProperty.UnknownFeature36, value);
        }
        /// <summary>Gets or sets the common Color1HSVEnabled property of the objects in this collection.</summary>
        public bool CommonColor1HSVEnabled
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.Color1HSVEnabled);
            set => SetCommonPropertyWithID(ObjectProperty.Color1HSVEnabled, value);
        }
        /// <summary>Gets or sets the common Color2HSVEnabled property of the objects in this collection.</summary>
        public bool CommonColor2HSVEnabled
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.Color2HSVEnabled);
            set => SetCommonPropertyWithID(ObjectProperty.Color2HSVEnabled, value);
        }
        /// <summary>Gets or sets the common Color1HSVValues property of the objects in this collection.</summary>
        public HSVAdjustment CommonColor1HSVValues
        {
            get => GetCommonPropertyWithID<HSVAdjustment>(ObjectProperty.Color1HSVValues);
            set => SetCommonPropertyWithID(ObjectProperty.Color1HSVValues, value);
        }
        /// <summary>Gets or sets the common Color2HSVValues property of the objects in this collection.</summary>
        public HSVAdjustment CommonColor2HSVValues
        {
            get => GetCommonPropertyWithID<HSVAdjustment>(ObjectProperty.Color2HSVValues);
            set => SetCommonPropertyWithID(ObjectProperty.Color2HSVValues, value);
        }
        /// <summary>Gets or sets the common FadeIn property of the objects in this collection.</summary>
        public double CommonFadeIn
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.FadeIn);
            set => SetCommonPropertyWithID(ObjectProperty.FadeIn, value);
        }
        /// <summary>Gets or sets the common Hold property of the objects in this collection.</summary>
        public double CommonHold
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.Hold);
            set => SetCommonPropertyWithID(ObjectProperty.Hold, value);
        }
        /// <summary>Gets or sets the common FadeOut property of the objects in this collection.</summary>
        public double CommonFadeOut
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.FadeOut);
            set => SetCommonPropertyWithID(ObjectProperty.FadeOut, value);
        }
        /// <summary>Gets or sets the common PulseMode property of the objects in this collection.</summary>
        public PulseMode CommonPulseMode
        {
            get => GetCommonPropertyWithID<PulseMode>(ObjectProperty.PulseMode);
            set => SetCommonPropertyWithID(ObjectProperty.PulseMode, value);
        }
        /// <summary>Gets or sets the common CopiedColorHSVValues property of the objects in this collection.</summary>
        public HSVAdjustment CommonCopiedColorHSVValues
        {
            get => GetCommonPropertyWithID<HSVAdjustment>(ObjectProperty.CopiedColorHSVValues);
            set => SetCommonPropertyWithID(ObjectProperty.CopiedColorHSVValues, value);
        }
        /// <summary>Gets or sets the common CopiedColorID property of the objects in this collection.</summary>
        public int CommonCopiedColorID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.CopiedColorID);
            set => SetCommonPropertyWithID(ObjectProperty.CopiedColorID, value);
        }
        /// <summary>Gets or sets the common TargetGroupID property of the objects in this collection.</summary>
        public int CommonTargetGroupID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.TargetGroupID);
            set => SetCommonPropertyWithID(ObjectProperty.TargetGroupID, value);
        }
        /// <summary>Gets or sets the common TargetType property of the objects in this collection.</summary>
        public PulseTargetType CommonTargetType
        {
            get => GetCommonPropertyWithID<PulseTargetType>(ObjectProperty.TargetType);
            set => SetCommonPropertyWithID(ObjectProperty.TargetType, value);
        }
        /// <summary>Gets or sets the common YellowTeleportationPortalDistance property of the objects in this collection.</summary>
        public double CommonYellowTeleportationPortalDistance
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.YellowTeleportationPortalDistance);
            set => SetCommonPropertyWithID(ObjectProperty.YellowTeleportationPortalDistance, value);
        }
        /// <summary>Gets or sets the common ActivateGroup property of the objects in this collection.</summary>
        public bool CommonActivateGroup
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.ActivateGroup);
            set => SetCommonPropertyWithID(ObjectProperty.ActivateGroup, value);
        }
        /// <summary>Gets or sets the common GroupIDs property of the objects in this collection.</summary>
        public int[] CommonGroupIDs
        {
            get => GetCommonPropertyWithID<int[]>(ObjectProperty.GroupIDs);
            set => SetCommonPropertyWithID(ObjectProperty.GroupIDs, value);
        }
        /// <summary>Gets or sets the common LockToPlayerX property of the objects in this collection.</summary>
        public bool CommonLockToPlayerX
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.LockToPlayerX);
            set => SetCommonPropertyWithID(ObjectProperty.LockToPlayerX, value);
        }
        /// <summary>Gets or sets the common LockToPlayerY property of the objects in this collection.</summary>
        public bool CommonLockToPlayerY
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.LockToPlayerY);
            set => SetCommonPropertyWithID(ObjectProperty.LockToPlayerY, value);
        }
        /// <summary>Gets or sets the common CopyOpacity property of the objects in this collection.</summary>
        public bool CommonCopyOpacity
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.CopyOpacity);
            set => SetCommonPropertyWithID(ObjectProperty.CopyOpacity, value);
        }
        /// <summary>Gets or sets the common EL2 property of the objects in this collection.</summary>
        public int CommonEL2
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.EL2);
            set => SetCommonPropertyWithID(ObjectProperty.EL2, value);
        }
        /// <summary>Gets or sets the common SpawnTriggered property of the objects in this collection.</summary>
        public bool CommonSpawnTriggered
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.SpawnTriggered);
            set => SetCommonPropertyWithID(ObjectProperty.SpawnTriggered, value);
        }
        /// <summary>Gets or sets the common SpawnDelay property of the objects in this collection.</summary>
        public double CommonSpawnDelay
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.SpawnDelay);
            set => SetCommonPropertyWithID(ObjectProperty.SpawnDelay, value);
        }
        /// <summary>Gets or sets the common DontFade property of the objects in this collection.</summary>
        public bool CommonDontFade
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.DontFade);
            set => SetCommonPropertyWithID(ObjectProperty.DontFade, value);
        }
        /// <summary>Gets or sets the common MainOnly property of the objects in this collection.</summary>
        public bool CommonMainOnly
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.MainOnly);
            set => SetCommonPropertyWithID(ObjectProperty.MainOnly, value);
        }
        /// <summary>Gets or sets the common DetailOnly property of the objects in this collection.</summary>
        public bool CommonDetailOnly
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.DetailOnly);
            set => SetCommonPropertyWithID(ObjectProperty.DetailOnly, value);
        }
        /// <summary>Gets or sets the common DontEnter property of the objects in this collection.</summary>
        public bool CommonDontEnter
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.DontEnter);
            set => SetCommonPropertyWithID(ObjectProperty.DontEnter, value);
        }
        /// <summary>Gets or sets the common Degrees property of the objects in this collection.</summary>
        public int CommonDegrees
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.Degrees);
            set => SetCommonPropertyWithID(ObjectProperty.Degrees, value);
        }
        /// <summary>Gets or sets the common Times360 property of the objects in this collection.</summary>
        public int CommonTimes360
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.Times360);
            set => SetCommonPropertyWithID(ObjectProperty.Times360, value);
        }
        /// <summary>Gets or sets the common LockObjectRotation property of the objects in this collection.</summary>
        public bool CommonLockObjectRotation
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.LockObjectRotation);
            set => SetCommonPropertyWithID(ObjectProperty.LockObjectRotation, value);
        }
        /// <summary>Gets or sets the common FollowGroupID property of the objects in this collection.</summary>
        public int CommonFollowGroupID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.FollowGroupID);
            set => SetCommonPropertyWithID(ObjectProperty.FollowGroupID, value);
        }
        /// <summary>Gets or sets the common TargetPosGroupID property of the objects in this collection.</summary>
        public int CommonTargetPosGroupID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.TargetPosGroupID);
            set => SetCommonPropertyWithID(ObjectProperty.TargetPosGroupID, value);
        }
        /// <summary>Gets or sets the common CenterGroupID property of the objects in this collection.</summary>
        public int CommonCenterGroupID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.CenterGroupID);
            set => SetCommonPropertyWithID(ObjectProperty.CenterGroupID, value);
        }
        /// <summary>Gets or sets the common SecondaryGroupID property of the objects in this collection.</summary>
        public int CommonSecondaryGroupID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.SecondaryGroupID);
            set => SetCommonPropertyWithID(ObjectProperty.SecondaryGroupID, value);
        }
        /// <summary>Gets or sets the common XMod property of the objects in this collection.</summary>
        public double CommonXMod
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.XMod);
            set => SetCommonPropertyWithID(ObjectProperty.XMod, value);
        }
        /// <summary>Gets or sets the common YMod property of the objects in this collection.</summary>
        public double CommonYMod
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.YMod);
            set => SetCommonPropertyWithID(ObjectProperty.YMod, value);
        }
        /// <summary>Gets or sets the common Strength property of the objects in this collection.</summary>
        public double CommonStrength
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.Strength);
            set => SetCommonPropertyWithID(ObjectProperty.Strength, value);
        }
        /// <summary>Gets or sets the common AnimationID property of the objects in this collection.</summary>
        public int CommonAnimationID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.AnimationID);
            set => SetCommonPropertyWithID(ObjectProperty.AnimationID, value);
        }
        /// <summary>Gets or sets the common Count property of the objects in this collection.</summary>
        public int CommonCount
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.Count);
            set => SetCommonPropertyWithID(ObjectProperty.Count, value);
        }
        /// <summary>Gets or sets the common SubtractCount property of the objects in this collection.</summary>
        public bool CommonSubtractCount
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.SubtractCount);
            set => SetCommonPropertyWithID(ObjectProperty.SubtractCount, value);
        }
        /// <summary>Gets or sets the common PickupMode property of the objects in this collection.</summary>
        public PickupItemPickupMode CommonPickupMode
        {
            get => GetCommonPropertyWithID<PickupItemPickupMode>(ObjectProperty.PickupMode);
            set => SetCommonPropertyWithID(ObjectProperty.PickupMode, value);
        }
        /// <summary>Gets or sets the common ItemID property of the objects in this collection.</summary>
        public int CommonItemID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.ItemID);
            set => SetCommonPropertyWithID(ObjectProperty.ItemID, value);
        }
        /// <summary>Gets or sets the common BlockID property of the objects in this collection.</summary>
        public int CommonBlockID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.BlockID);
            set => SetCommonPropertyWithID(ObjectProperty.BlockID, value);
        }
        /// <summary>Gets or sets the common BlockAID property of the objects in this collection.</summary>
        public int CommonBlockAID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.BlockAID);
            set => SetCommonPropertyWithID(ObjectProperty.BlockAID, value);
        }
        /// <summary>Gets or sets the common HoldMode property of the objects in this collection.</summary>
        public bool CommonHoldMode
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.HoldMode);
            set => SetCommonPropertyWithID(ObjectProperty.HoldMode, value);
        }
        /// <summary>Gets or sets the common ToggleMode property of the objects in this collection.</summary>
        public TouchToggleMode CommonToggleMode
        {
            get => GetCommonPropertyWithID<TouchToggleMode>(ObjectProperty.ToggleMode);
            set => SetCommonPropertyWithID(ObjectProperty.ToggleMode, value);
        }
        /// <summary>Gets or sets the common Interval property of the objects in this collection.</summary>
        public double CommonInterval
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.Interval);
            set => SetCommonPropertyWithID(ObjectProperty.Interval, value);
        }
        /// <summary>Gets or sets the common EasingRate property of the objects in this collection.</summary>
        public double CommonEasingRate
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.EasingRate);
            set => SetCommonPropertyWithID(ObjectProperty.EasingRate, value);
        }
        /// <summary>Gets or sets the common Exclusive property of the objects in this collection.</summary>
        public bool CommonExclusive
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.Exclusive);
            set => SetCommonPropertyWithID(ObjectProperty.Exclusive, value);
        }
        /// <summary>Gets or sets the common MultiTrigger property of the objects in this collection.</summary>
        public bool CommonMultiTrigger
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.MultiTrigger);
            set => SetCommonPropertyWithID(ObjectProperty.MultiTrigger, value);
        }
        /// <summary>Gets or sets the common Comparison property of the objects in this collection.</summary>
        public InstantCountComparison CommonComparison
        {
            get => GetCommonPropertyWithID<InstantCountComparison>(ObjectProperty.Comparison);
            set => SetCommonPropertyWithID(ObjectProperty.Comparison, value);
        }
        /// <summary>Gets or sets the common DualMode property of the objects in this collection.</summary>
        public bool CommonDualMode
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.DualMode);
            set => SetCommonPropertyWithID(ObjectProperty.DualMode, value);
        }
        /// <summary>Gets or sets the common Speed property of the objects in this collection.</summary>
        public double CommonSpeed
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.Speed);
            set => SetCommonPropertyWithID(ObjectProperty.Speed, value);
        }
        /// <summary>Gets or sets the common FollowDelay property of the objects in this collection.</summary>
        public double CommonFollowDelay
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.FollowDelay);
            set => SetCommonPropertyWithID(ObjectProperty.FollowDelay, value);
        }
        /// <summary>Gets or sets the common YOffset property of the objects in this collection.</summary>
        public double CommonYOffset
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.YOffset);
            set => SetCommonPropertyWithID(ObjectProperty.YOffset, value);
        }
        /// <summary>Gets or sets the common TriggerOnExit property of the objects in this collection.</summary>
        public bool CommonTriggerOnExit
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.TriggerOnExit);
            set => SetCommonPropertyWithID(ObjectProperty.TriggerOnExit, value);
        }
        /// <summary>Gets or sets the common DynamicBlock property of the objects in this collection.</summary>
        public bool CommonDynamicBlock
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.DynamicBlock);
            set => SetCommonPropertyWithID(ObjectProperty.DynamicBlock, value);
        }
        /// <summary>Gets or sets the common BlockBID property of the objects in this collection.</summary>
        public int CommonBlockBID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.BlockBID);
            set => SetCommonPropertyWithID(ObjectProperty.BlockBID, value);
        }
        /// <summary>Gets or sets the common DisableGlow property of the objects in this collection.</summary>
        public bool CommonDisableGlow
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.DisableGlow);
            set => SetCommonPropertyWithID(ObjectProperty.DisableGlow, value);
        }
        /// <summary>Gets or sets the common CustomRotationSpeed property of the objects in this collection.</summary>
        public int CommonCustomRotationSpeed
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.CustomRotationSpeed);
            set => SetCommonPropertyWithID(ObjectProperty.CustomRotationSpeed, value);
        }
        /// <summary>Gets or sets the common DisableRotation property of the objects in this collection.</summary>
        public bool CommonDisableRotation
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.DisableRotation);
            set => SetCommonPropertyWithID(ObjectProperty.DisableRotation, value);
        }
        /// <summary>Gets or sets the common MultiActivate property of the objects in this collection.</summary>
        public bool CommonMultiActivate
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.MultiActivate);
            set => SetCommonPropertyWithID(ObjectProperty.MultiActivate, value);
        }
        /// <summary>Gets or sets the common EnableUseTarget property of the objects in this collection.</summary>
        public bool CommonEnableUseTarget
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.EnableUseTarget);
            set => SetCommonPropertyWithID(ObjectProperty.EnableUseTarget, value);
        }
        /// <summary>Gets or sets the common TargetPosCoordinates property of the objects in this collection.</summary>
        public TargetPosCoordinates CommonTargetPosCoordinates
        {
            get => GetCommonPropertyWithID<TargetPosCoordinates>(ObjectProperty.TargetPosCoordinates);
            set => SetCommonPropertyWithID(ObjectProperty.TargetPosCoordinates, value);
        }
        /// <summary>Gets or sets the common EditorDisable property of the objects in this collection.</summary>
        public bool CommonEditorDisable
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.EditorDisable);
            set => SetCommonPropertyWithID(ObjectProperty.EditorDisable, value);
        }
        /// <summary>Gets or sets the common HighDetail property of the objects in this collection.</summary>
        public bool CommonHighDetail
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.HighDetail);
            set => SetCommonPropertyWithID(ObjectProperty.HighDetail, value);
        }
        /// <summary>Gets or sets the common MaxSpeed property of the objects in this collection.</summary>
        public double CommonMaxSpeed
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.MaxSpeed);
            set => SetCommonPropertyWithID(ObjectProperty.MaxSpeed, value);
        }
        /// <summary>Gets or sets the common RandomizeStart property of the objects in this collection.</summary>
        public bool CommonRandomizeStart
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.RandomizeStart);
            set => SetCommonPropertyWithID(ObjectProperty.RandomizeStart, value);
        }
        /// <summary>Gets or sets the common AnimationSpeed property of the objects in this collection.</summary>
        public double CommonAnimationSpeed
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.AnimationSpeed);
            set => SetCommonPropertyWithID(ObjectProperty.AnimationSpeed, value);
        }
        /// <summary>Gets or sets the common LinkedGroupID property of the objects in this collection.</summary>
        public int CommonLinkedGroupID
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.LinkedGroupID);
            set => SetCommonPropertyWithID(ObjectProperty.LinkedGroupID, value);
        }
        /// <summary>Gets or sets the common UnrevealedTextBoxFeature115 property of the objects in this collection.</summary>
        public int CommonUnrevealedTextBoxFeature115
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.UnrevealedTextBoxFeature115);
            set => SetCommonPropertyWithID(ObjectProperty.UnrevealedTextBoxFeature115, value);
        }
        /// <summary>Gets or sets the common SwitchPlayerDirection property of the objects in this collection.</summary>
        public bool CommonSwitchPlayerDirection
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.SwitchPlayerDirection);
            set => SetCommonPropertyWithID(ObjectProperty.SwitchPlayerDirection, value);
        }
        /// <summary>Gets or sets the common NoEffects property of the objects in this collection.</summary>
        public bool CommonNoEffects
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.NoEffects);
            set => SetCommonPropertyWithID(ObjectProperty.NoEffects, value);
        }
        /// <summary>Gets or sets the common IceBlock property of the objects in this collection.</summary>
        public bool CommonIceBlock
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.IceBlock);
            set => SetCommonPropertyWithID(ObjectProperty.IceBlock, value);
        }
        /// <summary>Gets or sets the common NonStick property of the objects in this collection.</summary>
        public bool CommonNonStick
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.NonStick);
            set => SetCommonPropertyWithID(ObjectProperty.NonStick, value);
        }
        /// <summary>Gets or sets the common Unstuckable property of the objects in this collection.</summary>
        public bool CommonUnstuckable
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.Unstuckable);
            set => SetCommonPropertyWithID(ObjectProperty.Unstuckable, value);
        }
        /// <summary>Gets or sets the common UnreadableProperty1 property of the objects in this collection.</summary>
        public bool CommonUnreadableProperty1
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.UnreadableProperty1);
            set => SetCommonPropertyWithID(ObjectProperty.UnreadableProperty1, value);
        }
        /// <summary>Gets or sets the common UnreadableProperty2 property of the objects in this collection.</summary>
        public bool CommonUnreadableProperty2
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.UnreadableProperty2);
            set => SetCommonPropertyWithID(ObjectProperty.UnreadableProperty2, value);
        }
        /// <summary>Gets or sets the common TransformationScalingX property of the objects in this collection.</summary>
        public double CommonTransformationScalingX
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.TransformationScalingX);
            set => SetCommonPropertyWithID(ObjectProperty.TransformationScalingX, value);
        }
        /// <summary>Gets or sets the common TransformationScalingY property of the objects in this collection.</summary>
        public double CommonTransformationScalingY
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.TransformationScalingY);
            set => SetCommonPropertyWithID(ObjectProperty.TransformationScalingY, value);
        }
        /// <summary>Gets or sets the common TransformationScalingCenterX property of the objects in this collection.</summary>
        public double CommonTransformationScalingCenterX
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.TransformationScalingCenterX);
            set => SetCommonPropertyWithID(ObjectProperty.TransformationScalingCenterX, value);
        }
        /// <summary>Gets or sets the common TransformationScalingCenterY property of the objects in this collection.</summary>
        public double CommonTransformationScalingCenterY
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.TransformationScalingCenterY);
            set => SetCommonPropertyWithID(ObjectProperty.TransformationScalingCenterY, value);
        }
        /// <summary>Gets or sets the common ExitStatic property of the objects in this collection.</summary>
        public bool CommonExitStatic
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.ExitStatic);
            set => SetCommonPropertyWithID(ObjectProperty.ExitStatic, value);
        }
        /// <summary>Gets or sets the common Reversed property of the objects in this collection.</summary>
        public bool CommonReversed
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.Reversed);
            set => SetCommonPropertyWithID(ObjectProperty.Reversed, value);
        }
        /// <summary>Gets or sets the common LockY property of the objects in this collection.</summary>
        public bool CommonLockY
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.LockY);
            set => SetCommonPropertyWithID(ObjectProperty.LockY, value);
        }
        /// <summary>Gets or sets the common Chance property of the objects in this collection.</summary>
        public double CommonChance
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.Chance);
            set => SetCommonPropertyWithID(ObjectProperty.Chance, value);
        }
        /// <summary>Gets or sets the common ChanceLots property of the objects in this collection.</summary>
        public object CommonChanceLots
        {
            get => GetCommonPropertyWithID<object>(ObjectProperty.ChanceLots);
            set => SetCommonPropertyWithID(ObjectProperty.ChanceLots, value);
        }
        /// <summary>Gets or sets the common ChanceLotGroups property of the objects in this collection.</summary>
        public int[] CommonChanceLotGroups
        {
            get => GetCommonPropertyWithID<int[]>(ObjectProperty.ChanceLotGroups);
            set => SetCommonPropertyWithID(ObjectProperty.ChanceLotGroups, value);
        }
        /// <summary>Gets or sets the common Zoom property of the objects in this collection.</summary>
        public int CommonZoom
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.Zoom);
            set => SetCommonPropertyWithID(ObjectProperty.Zoom, value);
        }
        /// <summary>Gets or sets the common Grouping property of the objects in this collection.</summary>
        public CustomParticleGrouping CommonGrouping
        {
            get => GetCommonPropertyWithID<CustomParticleGrouping>(ObjectProperty.Grouping);
            set => SetCommonPropertyWithID(ObjectProperty.Grouping, value);
        }
        /// <summary>Gets or sets the common Property1 property of the objects in this collection.</summary>
        public CustomParticleProperty1 CommonProperty1
        {
            get => GetCommonPropertyWithID<CustomParticleProperty1>(ObjectProperty.Property1);
            set => SetCommonPropertyWithID(ObjectProperty.Property1, value);
        }
        /// <summary>Gets or sets the common MaxParticles property of the objects in this collection.</summary>
        public int CommonMaxParticles
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.MaxParticles);
            set => SetCommonPropertyWithID(ObjectProperty.MaxParticles, value);
        }
        /// <summary>Gets or sets the common CustomParticleDuration property of the objects in this collection.</summary>
        public double CommonCustomParticleDuration
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.CustomParticleDuration);
            set => SetCommonPropertyWithID(ObjectProperty.CustomParticleDuration, value);
        }
        /// <summary>Gets or sets the common Lifetime property of the objects in this collection.</summary>
        public double CommonLifetime
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.Lifetime);
            set => SetCommonPropertyWithID(ObjectProperty.Lifetime, value);
        }
        /// <summary>Gets or sets the common LifetimeAdjustment property of the objects in this collection.</summary>
        public double CommonLifetimeAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.LifetimeAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.LifetimeAdjustment, value);
        }
        /// <summary>Gets or sets the common Emission property of the objects in this collection.</summary>
        public int CommonEmission
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.Emission);
            set => SetCommonPropertyWithID(ObjectProperty.Emission, value);
        }
        /// <summary>Gets or sets the common Angle property of the objects in this collection.</summary>
        public double CommonAngle
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.Angle);
            set => SetCommonPropertyWithID(ObjectProperty.Angle, value);
        }
        /// <summary>Gets or sets the common AngleAdjustment property of the objects in this collection.</summary>
        public double CommonAngleAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.AngleAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.AngleAdjustment, value);
        }
        /// <summary>Gets or sets the common CustomParticleSpeed property of the objects in this collection.</summary>
        public double CommonCustomParticleSpeed
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.CustomParticleSpeed);
            set => SetCommonPropertyWithID(ObjectProperty.CustomParticleSpeed, value);
        }
        /// <summary>Gets or sets the common SpeedAdjustment property of the objects in this collection.</summary>
        public double CommonSpeedAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.SpeedAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.SpeedAdjustment, value);
        }
        /// <summary>Gets or sets the common PosVarX property of the objects in this collection.</summary>
        public double CommonPosVarX
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.PosVarX);
            set => SetCommonPropertyWithID(ObjectProperty.PosVarX, value);
        }
        /// <summary>Gets or sets the common PosVarY property of the objects in this collection.</summary>
        public double CommonPosVarY
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.PosVarY);
            set => SetCommonPropertyWithID(ObjectProperty.PosVarY, value);
        }
        /// <summary>Gets or sets the common GravityX property of the objects in this collection.</summary>
        public double CommonGravityX
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.GravityX);
            set => SetCommonPropertyWithID(ObjectProperty.GravityX, value);
        }
        /// <summary>Gets or sets the common GravityY property of the objects in this collection.</summary>
        public double CommonGravityY
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.GravityY);
            set => SetCommonPropertyWithID(ObjectProperty.GravityY, value);
        }
        /// <summary>Gets or sets the common AccelRad property of the objects in this collection.</summary>
        public double CommonAccelRad
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.AccelRad);
            set => SetCommonPropertyWithID(ObjectProperty.AccelRad, value);
        }
        /// <summary>Gets or sets the common AccelRadAdjustment property of the objects in this collection.</summary>
        public double CommonAccelRadAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.AccelRadAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.AccelRadAdjustment, value);
        }
        /// <summary>Gets or sets the common AccelTan property of the objects in this collection.</summary>
        public double CommonAccelTan
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.AccelTan);
            set => SetCommonPropertyWithID(ObjectProperty.AccelTan, value);
        }
        /// <summary>Gets or sets the common AccelTanAdjustment property of the objects in this collection.</summary>
        public double CommonAccelTanAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.AccelTanAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.AccelTanAdjustment, value);
        }
        /// <summary>Gets or sets the common StartSize property of the objects in this collection.</summary>
        public int CommonStartSize
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.StartSize);
            set => SetCommonPropertyWithID(ObjectProperty.StartSize, value);
        }
        /// <summary>Gets or sets the common StartSizeAdjustment property of the objects in this collection.</summary>
        public int CommonStartSizeAdjustment
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.StartSizeAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.StartSizeAdjustment, value);
        }
        /// <summary>Gets or sets the common EndSize property of the objects in this collection.</summary>
        public int CommonEndSize
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.EndSize);
            set => SetCommonPropertyWithID(ObjectProperty.EndSize, value);
        }
        /// <summary>Gets or sets the common EndSizeAdjustment property of the objects in this collection.</summary>
        public int CommonEndSizeAdjustment
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.EndSizeAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.EndSizeAdjustment, value);
        }
        /// <summary>Gets or sets the common StartSpin property of the objects in this collection.</summary>
        public int CommonStartSpin
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.StartSpin);
            set => SetCommonPropertyWithID(ObjectProperty.StartSpin, value);
        }
        /// <summary>Gets or sets the common StartSpinAdjustment property of the objects in this collection.</summary>
        public int CommonStartSpinAdjustment
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.StartSpinAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.StartSpinAdjustment, value);
        }
        /// <summary>Gets or sets the common EndSpin property of the objects in this collection.</summary>
        public int CommonEndSpin
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.EndSpin);
            set => SetCommonPropertyWithID(ObjectProperty.EndSpin, value);
        }
        /// <summary>Gets or sets the common EndSpinAdjustment property of the objects in this collection.</summary>
        public int CommonEndSpinAdjustment
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.EndSpinAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.EndSpinAdjustment, value);
        }
        /// <summary>Gets or sets the common StartA property of the objects in this collection.</summary>
        public double CommonStartA
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.StartA);
            set => SetCommonPropertyWithID(ObjectProperty.StartA, value);
        }
        /// <summary>Gets or sets the common StartAAdjustment property of the objects in this collection.</summary>
        public double CommonStartAAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.StartAAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.StartAAdjustment, value);
        }
        /// <summary>Gets or sets the common StartR property of the objects in this collection.</summary>
        public double CommonStartR
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.StartR);
            set => SetCommonPropertyWithID(ObjectProperty.StartR, value);
        }
        /// <summary>Gets or sets the common StartRAdjustment property of the objects in this collection.</summary>
        public double CommonStartRAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.StartRAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.StartRAdjustment, value);
        }
        /// <summary>Gets or sets the common StartG property of the objects in this collection.</summary>
        public double CommonStartG
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.StartG);
            set => SetCommonPropertyWithID(ObjectProperty.StartG, value);
        }
        /// <summary>Gets or sets the common StartGAdjustment property of the objects in this collection.</summary>
        public double CommonStartGAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.StartGAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.StartGAdjustment, value);
        }
        /// <summary>Gets or sets the common StartB property of the objects in this collection.</summary>
        public double CommonStartB
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.StartB);
            set => SetCommonPropertyWithID(ObjectProperty.StartB, value);
        }
        /// <summary>Gets or sets the common StartBAdjustment property of the objects in this collection.</summary>
        public double CommonStartBAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.StartBAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.StartBAdjustment, value);
        }
        /// <summary>Gets or sets the common EndA property of the objects in this collection.</summary>
        public double CommonEndA
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.EndA);
            set => SetCommonPropertyWithID(ObjectProperty.EndA, value);
        }
        /// <summary>Gets or sets the common EndAAdjustment property of the objects in this collection.</summary>
        public double CommonEndAAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.EndAAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.EndAAdjustment, value);
        }
        /// <summary>Gets or sets the common EndR property of the objects in this collection.</summary>
        public double CommonEndR
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.EndR);
            set => SetCommonPropertyWithID(ObjectProperty.EndR, value);
        }
        /// <summary>Gets or sets the common EndRAdjustment property of the objects in this collection.</summary>
        public double CommonEndRAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.EndRAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.EndRAdjustment, value);
        }
        /// <summary>Gets or sets the common EndG property of the objects in this collection.</summary>
        public double CommonEndG
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.EndG);
            set => SetCommonPropertyWithID(ObjectProperty.EndG, value);
        }
        /// <summary>Gets or sets the common EndGAdjustment property of the objects in this collection.</summary>
        public double CommonEndGAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.EndGAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.EndGAdjustment, value);
        }
        /// <summary>Gets or sets the common EndB property of the objects in this collection.</summary>
        public double CommonEndB
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.EndB);
            set => SetCommonPropertyWithID(ObjectProperty.EndB, value);
        }
        /// <summary>Gets or sets the common EndBAdjustment property of the objects in this collection.</summary>
        public double CommonEndBAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.EndBAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.EndBAdjustment, value);
        }
        /// <summary>Gets or sets the common CustomParticleFadeIn property of the objects in this collection.</summary>
        public double CommonCustomParticleFadeIn
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.CustomParticleFadeIn);
            set => SetCommonPropertyWithID(ObjectProperty.CustomParticleFadeIn, value);
        }
        /// <summary>Gets or sets the common FadeInAdjustment property of the objects in this collection.</summary>
        public double CommonFadeInAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.FadeInAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.FadeInAdjustment, value);
        }
        /// <summary>Gets or sets the common CustomParticleFadeOut property of the objects in this collection.</summary>
        public double CommonCustomParticleFadeOut
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.CustomParticleFadeOut);
            set => SetCommonPropertyWithID(ObjectProperty.CustomParticleFadeOut, value);
        }
        /// <summary>Gets or sets the common FadeOutAdjustment property of the objects in this collection.</summary>
        public double CommonFadeOutAdjustment
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.FadeOutAdjustment);
            set => SetCommonPropertyWithID(ObjectProperty.FadeOutAdjustment, value);
        }
        /// <summary>Gets or sets the common Additive property of the objects in this collection.</summary>
        public bool CommonAdditive
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.Additive);
            set => SetCommonPropertyWithID(ObjectProperty.Additive, value);
        }
        /// <summary>Gets or sets the common StartSizeEqualsEnd property of the objects in this collection.</summary>
        public bool CommonStartSizeEqualsEnd
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.StartSizeEqualsEnd);
            set => SetCommonPropertyWithID(ObjectProperty.StartSizeEqualsEnd, value);
        }
        /// <summary>Gets or sets the common StartSpinEqualsEnd property of the objects in this collection.</summary>
        public bool CommonStartSpinEqualsEnd
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.StartSpinEqualsEnd);
            set => SetCommonPropertyWithID(ObjectProperty.StartSpinEqualsEnd, value);
        }
        /// <summary>Gets or sets the common StartRadiusEqualsEnd property of the objects in this collection.</summary>
        public bool CommonStartRadiusEqualsEnd
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.StartRadiusEqualsEnd);
            set => SetCommonPropertyWithID(ObjectProperty.StartRadiusEqualsEnd, value);
        }
        /// <summary>Gets or sets the common StartRotationIsDir property of the objects in this collection.</summary>
        public bool CommonStartRotationIsDir
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.StartRotationIsDir);
            set => SetCommonPropertyWithID(ObjectProperty.StartRotationIsDir, value);
        }
        /// <summary>Gets or sets the common DynamicRotation property of the objects in this collection.</summary>
        public bool CommonDynamicRotation
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.DynamicRotation);
            set => SetCommonPropertyWithID(ObjectProperty.DynamicRotation, value);
        }
        /// <summary>Gets or sets the common UseObjectColor property of the objects in this collection.</summary>
        public bool CommonUseObjectColor
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.UseObjectColor);
            set => SetCommonPropertyWithID(ObjectProperty.UseObjectColor, value);
        }
        /// <summary>Gets or sets the common UniformObjectColor property of the objects in this collection.</summary>
        public bool CommonUniformObjectColor
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.UniformObjectColor);
            set => SetCommonPropertyWithID(ObjectProperty.UniformObjectColor, value);
        }
        /// <summary>Gets or sets the common Texture property of the objects in this collection.</summary>
        public int CommonTexture
        {
            get => GetCommonPropertyWithID<int>(ObjectProperty.Texture);
            set => SetCommonPropertyWithID(ObjectProperty.Texture, value);
        }
        /// <summary>Gets or sets the common ScaleX property of the objects in this collection.</summary>
        public double CommonScaleX
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.ScaleX);
            set => SetCommonPropertyWithID(ObjectProperty.ScaleX, value);
        }
        /// <summary>Gets or sets the common ScaleY property of the objects in this collection.</summary>
        public double CommonScaleY
        {
            get => GetCommonPropertyWithID<double>(ObjectProperty.ScaleY);
            set => SetCommonPropertyWithID(ObjectProperty.ScaleY, value);
        }
        /// <summary>Gets or sets the common LockObjectScale property of the objects in this collection.</summary>
        public bool CommonLockObjectScale
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.LockObjectScale);
            set => SetCommonPropertyWithID(ObjectProperty.LockObjectScale, value);
        }
        /// <summary>Gets or sets the common OnlyMoveScale property of the objects in this collection.</summary>
        public bool CommonOnlyMoveScale
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.OnlyMoveScale);
            set => SetCommonPropertyWithID(ObjectProperty.OnlyMoveScale, value);
        }
        /// <summary>Gets or sets the common LockToCameraX property of the objects in this collection.</summary>
        public bool CommonLockToCameraX
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.LockToCameraX);
            set => SetCommonPropertyWithID(ObjectProperty.LockToCameraX, value);
        }
        /// <summary>Gets or sets the common LockToCameraY property of the objects in this collection.</summary>
        public bool CommonLockToCameraY
        {
            get => GetCommonPropertyWithID<bool>(ObjectProperty.LockToCameraY);
            set => SetCommonPropertyWithID(ObjectProperty.LockToCameraY, value);
        }
        // Please make a script because writing all this shit for 108 or so properties is disgusting
        #endregion

        /// <summary>Returns a <seealso cref="LevelObjectCollection"/> that contains the objects that have a group ID equal to the provided value.</summary>
        /// <param name="groupID">The group ID of the objects to look for.</param>
        public LevelObjectCollection GetObjectsByGroupID(int groupID)
        {
            var result = new LevelObjectCollection();
            foreach (var o in objects)
                if (o.GroupIDs.Contains(groupID))
                    result.Add(o);
            return result;
        }
        /// <summary>Returns a <seealso cref="LevelObjectCollection"/> that contains the objects that have a main or detail color ID equal to the provided value.</summary>
        /// <param name="colorID">The color ID of the objects to look for.</param>
        public LevelObjectCollection GetObjectsByColorID(int colorID)
        {
            var result = new LevelObjectCollection();
            foreach (var o in objects)
                if (o.Color1ID == colorID || o.Color2ID == colorID)
                    result.Add(o);
            return result;
        }

        #region Object Property Metadata
        /// <summary>Returns the common object properties found in this <seealso cref="LevelObjectCollection"/>.</summary>
        public PropertyAccessInfoDictionary GetCommonProperties()
        {
            if (commonProperties == null)
                commonProperties = GeneralObject.GetCommonProperties(this);
            else
            {
                for (; commonPropertiesUnevaluatedIndex < unevaluatedObjects.ListCount; commonPropertiesUnevaluatedIndex++)
                    commonProperties = GeneralObject.GetCommonProperties(unevaluatedObjects[commonPropertiesUnevaluatedIndex], commonProperties);
                RemoveEvaluatedObjects();
            }
            return commonProperties;
        }
        /// <summary>Returns all the available object properties found in this <seealso cref="LevelObjectCollection"/>.</summary>
        public PropertyAccessInfoDictionary GetAllAvailableProperties()
        {
            if (allAvailableProperties == null)
                allAvailableProperties = GeneralObject.GetAllAvailableProperties(this);
            else
            {
                for (; allAvailablePropertiesUnevaluatedIndex < unevaluatedObjects.ListCount; allAvailablePropertiesUnevaluatedIndex++)
                    allAvailableProperties = GeneralObject.GetAllAvailableProperties(unevaluatedObjects[allAvailablePropertiesUnevaluatedIndex], allAvailableProperties);
                RemoveEvaluatedObjects();
            }
            return allAvailableProperties;
        }

        private void RemoveEvaluatedObjects()
        {
            int count = Math.Min(commonPropertiesUnevaluatedIndex, allAvailablePropertiesUnevaluatedIndex);
            unevaluatedObjects.RemoveFirst(count);
            commonPropertiesUnevaluatedIndex -= count;
            allAvailablePropertiesUnevaluatedIndex -= count;
        }
        #endregion

        #region Dictionaries
        // Keep in mind, those functions' performance is really low
        /// <summary>Returns a <seealso cref="Dictionary{TKey, TValue}"/> that categorizes the level objects in this <seealso cref="LevelObjectCollection"/> based on their main color ID.</summary>
        public Dictionary<int, LevelObjectCollection> GetMainColorIDObjectDictionary() => GetObjectDictionary(o => o.Color1ID);
        /// <summary>Returns a <seealso cref="Dictionary{TKey, TValue}"/> that categorizes the level objects in this <seealso cref="LevelObjectCollection"/> based on their detail color ID.</summary>
        public Dictionary<int, LevelObjectCollection> GetDetailColorIDObjectDictionary() => GetObjectDictionary(o => o.Color2ID);
        /// <summary>Returns a <seealso cref="Dictionary{TKey, TValue}"/> that categorizes the level objects in this <seealso cref="LevelObjectCollection"/> based on their main and detail color IDs.</summary>
        public Dictionary<int, LevelObjectCollection> GetColorIDObjectDictionary() => GetObjectDictionary(o => (IEnumerable<int>)new List<int> { o.Color1ID, o.Color2ID });
        /// <summary>Returns a <seealso cref="Dictionary{TKey, TValue}"/> that categorizes the level objects in this <seealso cref="LevelObjectCollection"/> based on their group IDs.</summary>
        public Dictionary<int, LevelObjectCollection> GetGroupIDObjectDictionary() => GetObjectDictionary(o => (IEnumerable<int>)o.GroupIDs);

        /// <summary>Returns a <seealso cref="Dictionary{TKey, TValue}"/> that categorizes the level objects in this <seealso cref="LevelObjectCollection"/> based on a selector.</summary>
        /// <param name="selector">The selector function to categorize this <seealso cref="LevelObjectCollection"/>'s objects in the dictionary.</param>
        public Dictionary<TKey, LevelObjectCollection> GetObjectDictionary<TKey>(Func<GeneralObject, TKey> selector)
        {
            var result = new Dictionary<TKey, LevelObjectCollection>();
            foreach (var o in objects)
                HandleEntryInsertion(result, selector(o), o);
            return result;
        }
        /// <summary>Returns a <seealso cref="Dictionary{TKey, TValue}"/> that categorizes the level objects in this <seealso cref="LevelObjectCollection"/> based on a multiple key selector.</summary>
        /// <param name="selector">The selector function to categorize this <seealso cref="LevelObjectCollection"/>'s objects in the dictionary. Each of the returned keys will contain this object.</param>
        public Dictionary<TKey, LevelObjectCollection> GetObjectDictionary<TKey>(Func<GeneralObject, IEnumerable<TKey>> selector)
        {
            var result = new Dictionary<TKey, LevelObjectCollection>();
            foreach (var o in objects)
                foreach (var key in selector(o))
                    HandleEntryInsertion(result, key, o);
            return result;
        }
        #endregion

        /// <summary>Gets or sets the level object at the specified index.</summary>
        /// <param name="index">The index of the level object.</param>
        public GeneralObject this[int index]
        {
            get => objects[index];
            set => objects[index] = value;
        }

        public IEnumerator<GeneralObject> GetEnumerator() => objects.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void AddToCounters(IEnumerable<GeneralObject> objects)
        {
            foreach (var o in objects)
                AddToCounters(o);
        }
        private void AddToCounters(GeneralObject o)
        {
            AdjustCounters(o, 1);
            ObjectCounts.IncrementOrAddKeyValue(o.ObjectID);
            foreach (var g in o.GroupIDs)
                GroupCounts.IncrementOrAddKeyValue(g);
        }
        private void RemoveFromCounters(GeneralObject o)
        {
            AdjustCounters(o, -1);
            ObjectCounts[o.ObjectID]--;
            foreach (var g in o.GroupIDs)
                GroupCounts[g]--;
        }
        private void AdjustCounters(GeneralObject o, int adjustment)
        {
            switch (o)
            {
                case ColorTrigger _:
                    if (colorTriggerCount > -1)
                        colorTriggerCount += adjustment;
                    break;
                case Trigger _:
                    if (triggerCount > -1)
                        triggerCount += adjustment;
                    break;
            }
        }
        private void ResetCounters()
        {
            colorTriggerCount = -1;
            triggerCount = -1;
        }

        private void RegisterUnevaluatedObject(GeneralObject o)
        {
            if (ShouldRegisterUnevaluatedObjects())
                unevaluatedObjects.Add(o);
        }
        private void RegisterUnevaluatedObjects(IEnumerable<GeneralObject> objects)
        {
            if (ShouldRegisterUnevaluatedObjects())
                unevaluatedObjects.Add(objects);
        }
        private bool ShouldRegisterUnevaluatedObjects() => commonProperties != null || allAvailableProperties != null;
        private void SetPropertyCacheToDefault()
        {
            commonProperties = new PropertyAccessInfoDictionary();
            allAvailableProperties = new PropertyAccessInfoDictionary();
            ResetUnevaluatedObjects();
        }
        private void ClearPropertyCache()
        {
            commonProperties = null;
            allAvailableProperties = null;
            ResetUnevaluatedObjects();
        }
        private void ResetUnevaluatedObjects()
        {
            unevaluatedObjects.Clear();
            commonPropertiesUnevaluatedIndex = 0;
            allAvailablePropertiesUnevaluatedIndex = 0;
        }

        private static void HandleEntryInsertion<TKey>(Dictionary<TKey, LevelObjectCollection> dictionary, TKey key, GeneralObject o)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key].Add(o);
            else
                dictionary.Add(key, new LevelObjectCollection(o));
        }

        /// <summary>Returns a <see langword="string"/> that represents the current object.</summary>
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            foreach (var o in objects)
                s.Append($"{o};");
            return s.ToString();
        }
    }
}
