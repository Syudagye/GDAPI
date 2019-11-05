﻿using GDAPI.Utilities.Attributes;
using GDAPI.Utilities.Enumerations.GeometryDash;
using GDAPI.Utilities.Information.GeometryDash;
using GDAPI.Utilities.Objects.GeometryDash.LevelObjects.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDAPI.Utilities.Objects.GeometryDash.LevelObjects.SpecialObjects.Orbs
{
    /// <summary>Represents a green orb.</summary>
    [ObjectID(OrbType.GreenOrb)]
    public class GreenOrb : Orb
    {
        /// <summary>The object ID of the green orb.</summary>
        [ObjectStringMappable(ObjectParameter.ID)]
        public override int ObjectID => (int)OrbType.GreenOrb;

        /// <summary>Initializes a new instance of the <seealso cref="GreenOrb"/> class.</summary>
        public GreenOrb() : base() { }

        /// <summary>Returns a clone of this <seealso cref="GreenOrb"/>.</summary>
        public override GeneralObject Clone() => AddClonedInstanceInformation(new GreenOrb());
    }
}