using MathAndScience;
using MathAndScience.Shapes;
using System;
using System.Collections.Generic;
using MathAndScience.Numerics;
using System.Text;
using WorldFoundry.Space;
using Substances;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets
{
    /// <summary>
    /// Any planetary-mass object (massive enough to be rounded under its own gravity), such as dwarf planets, some moons, and planets.
    /// </summary>
    public class Planemo : Planetoid
    {
        private const double IcyRingDensity = 300.0;
        private const double RockyRingDensity = 1380.0;

        private List<PlanetaryRing> _rings;
        /// <summary>
        /// The collection of rings around this <see cref="Planemo"/>.
        /// </summary>
        public List<PlanetaryRing> Rings
        {
            get
            {
                if (_rings == null)
                {
                    GenerateRings();
                }
                return _rings;
            }
        }

        /// <summary>
        /// The name for this type of <see cref="ICelestialLocation"/>.
        /// </summary>
        public override string TypeName
        {
            get
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(PlanemoClassPrefix))
                {
                    sb.Append(PlanemoClassPrefix);
                    sb.Append(" ");
                }
                if (Orbit?.OrbitedObject is Planemo)
                {
                    sb.Append("Moon");
                }
                else if (ContainingCelestialRegion is StarSystem)
                {
                    sb.Append(BaseTypeName);
                }
                else
                {
                    sb.Insert(0, "Rogue ");
                    sb.Append(BaseTypeName);
                }
                return sb.ToString();
            }
        }

        private protected override string BaseTypeName => "Planet";

        // Set to 5 for Planemo. For reference, Pluto has 5 moons, the most of any
        // planemo in the Solar System apart from the giants. No others are known to have more than 2.
        private protected override int MaxSatellites => 5;

        private protected virtual string PlanemoClassPrefix => null;

        private protected virtual double RingChance => 0;

        /// <summary>
        /// Initializes a new instance of <see cref="Planemo"/>.
        /// </summary>
        internal Planemo() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planemo"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Planemo"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planemo"/>.</param>
        internal Planemo(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planemo"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Planemo"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planemo"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Planemo"/> during random generation, in kg.
        /// </param>
        internal Planemo(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        internal override void GenerateOrbit(ICelestialLocation orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            Space.Orbit.SetOrbit(
                this,
                orbitedObject,
                GetDistanceTo(orbitedObject),
                Eccentricity,
                Math.Round(Randomizer.Instance.NextDouble(0.9), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4));
        }

        private protected void GenerateRings()
        {
            _rings = new List<PlanetaryRing>();

            var innerLimit = Atmosphere == null ? 0 : Atmosphere.AtmosphericHeight;

            var outerLimit_Icy = GetRingDistance(IcyRingDensity);
            if (Orbit != null)
            {
                outerLimit_Icy = Math.Min(outerLimit_Icy, GetHillSphereRadius() / 3.0);
            }
            if (innerLimit >= outerLimit_Icy)
            {
                return;
            }

            var outerLimit_Rocky = GetRingDistance(RockyRingDensity);
            if (Orbit != null)
            {
                outerLimit_Rocky = Math.Min(outerLimit_Rocky, GetHillSphereRadius() / 3.0);
            }

            var _ringChance = RingChance;
            while (Randomizer.Instance.NextDouble() <= _ringChance && innerLimit <= outerLimit_Icy)
            {
                if (innerLimit < outerLimit_Rocky && Randomizer.Instance.NextBoolean())
                {
                    var innerRadius = Math.Round(Randomizer.Instance.NextDouble(innerLimit, outerLimit_Rocky));

                    Rings.Add(new PlanetaryRing(false, innerRadius, outerLimit_Rocky));

                    outerLimit_Rocky = innerRadius;
                    if (outerLimit_Rocky <= outerLimit_Icy)
                    {
                        outerLimit_Icy = innerRadius;
                    }
                }
                else
                {
                    var innerRadius = Math.Round(Randomizer.Instance.NextDouble(innerLimit, outerLimit_Icy));

                    Rings.Add(new PlanetaryRing(true, innerRadius, outerLimit_Icy));

                    outerLimit_Icy = innerRadius;
                    if (outerLimit_Icy <= outerLimit_Rocky)
                    {
                        outerLimit_Rocky = innerRadius;
                    }
                }

                _ringChance *= 0.5;
            }
        }

        private protected override IComposition GetComposition(double mass, IShape shape)
        {
            var coreProportion = GetCoreProportion(mass);
            var crustProportion = GetCrustProportion(shape);
            var mantleProportion = 1 - (coreProportion + crustProportion);

            var coreLayers = GetCore(mass);
            var mantleLayers = GetMantle(shape, mantleProportion);
            var crustLayers = GetCrust();

            var lc = new LayeredComposite();
            foreach (var (layer, proportion) in coreLayers)
            {
                lc.Layers.Add((layer, proportion * coreProportion));
            }
            foreach (var (layer, proportion) in mantleLayers)
            {
                lc.Layers.Add((layer, proportion * mantleProportion));
            }
            foreach (var (layer, proportion) in crustLayers)
            {
                lc.Layers.Add((layer, proportion * crustProportion));
            }

            return lc;
        }

        private protected virtual IEnumerable<(IComposition, double)> GetCore(double mass)
        {
            yield return (new Material(Chemical.Rock, Phase.Solid), 1);
        }

        private protected virtual double GetCoreProportion(double mass) => 0.15;

        private protected virtual IEnumerable<(IComposition, double)> GetCrust()
        {
            var dust = Randomizer.Instance.NextDouble();
            var total = dust;

            // 50% chance of not including the following:
            var waterIce = Math.Max(0, Randomizer.Instance.NextDouble(-0.5, 0.5));
            total += waterIce;

            var n2 = Math.Max(0, Randomizer.Instance.NextDouble(-0.5, 0.5));
            total += n2;

            var ch4 = Math.Max(0, Randomizer.Instance.NextDouble(-0.5, 0.5));
            total += ch4;

            var co = Math.Max(0, Randomizer.Instance.NextDouble(-0.5, 0.5));
            total += co;

            var co2 = Math.Max(0, Randomizer.Instance.NextDouble(-0.5, 0.5));
            total += co2;

            var nh3 = Math.Max(0, Randomizer.Instance.NextDouble(-0.5, 0.5));
            total += nh3;

            var ratio = 1.0 / total;
            dust *= ratio;
            waterIce *= ratio;
            n2 *= ratio;
            ch4 *= ratio;
            co *= ratio;
            co2 *= ratio;
            nh3 *= ratio;

            var crust = new Composite((Chemical.Dust, Phase.Solid, dust));
            if (waterIce > 0)
            {
                crust.Components[(Chemical.Water, Phase.Solid)] = waterIce;
            }
            if (n2 > 0)
            {
                crust.Components[(Chemical.Nitrogen, Phase.Solid)] = n2;
            }
            if (ch4 > 0)
            {
                crust.Components[(Chemical.Methane, Phase.Solid)] = ch4;
            }
            if (co > 0)
            {
                crust.Components[(Chemical.CarbonMonoxide, Phase.Solid)] = co;
            }
            if (co2 > 0)
            {
                crust.Components[(Chemical.CarbonDioxide, Phase.Solid)] = co2;
            }
            if (nh3 > 0)
            {
                crust.Components[(Chemical.Ammonia, Phase.Solid)] = nh3;
            }
            yield return (crust, 1);
        }

        // Smaller planemos have thicker crusts due to faster proto-planetary cooling.
        private protected virtual double GetCrustProportion(IShape shape) => 400000.0 / Math.Pow(shape.ContainingRadius, 1.6);

        private protected IComposition GetIronNickelCore()
        {
            var coreNickel = Randomizer.Instance.NextDouble(0.03, 0.15);
            return new Composite(
                (Chemical.Iron, Phase.Solid, 1 - coreNickel),
                (Chemical.Nickel, Phase.Solid, coreNickel));
        }

        private protected virtual IEnumerable<(IComposition, double)> GetMantle(IShape shape, double proportion)
        {
            var mantleIce = Randomizer.Instance.NextDouble(0.2, 1);
            yield return (new Composite(
                (Chemical.Water, Phase.Solid, mantleIce),
                (Chemical.Water, Phase.Liquid, 1 - mantleIce)),
                1);
        }

        private protected override double GetMass(IShape shape = null)
        {
            var maxMass = MaxMass;
            if (ContainingCelestialRegion != null)
            {
                maxMass = Math.Min(maxMass, GetSternLevisonLambdaMass() / 100);
                if (maxMass < MinMass)
                {
                    maxMass = MinMass; // sanity check; may result in a "dwarf" planet which *can* clear its neighborhood
                }
            }

            return Randomizer.Instance.NextDouble(MinMass, maxMass);
        }

        private protected override (double, IShape) GetMassAndShape()
        {
            var mass = GetMass();
            return (mass, GetShape(mass));
        }

        private protected double GetMaxRadius() => MaxMassForType.HasValue ? GetRadiusForMass(MaxMassForType.Value) : double.PositiveInfinity;

        private double GetRadiusForMass(double mass) => Math.Pow(mass / Density / MathConstants.FourThirdsPI, 1.0 / 3.0);

        /// <summary>
        /// Calculates the approximate outer distance at which rings of the given density may be
        /// found, in meters.
        /// </summary>
        /// <param name="density">The density of the rings, in kg/m³.</param>
        /// <returns>The approximate outer distance at which rings of the given density may be
        /// found, in meters.</returns>
        private double GetRingDistance(double density) => 1.26 * Shape.ContainingRadius * Math.Pow(Density / density, 1.0 / 3.0);

        private protected override IShape GetShape(double? mass = null, double? knownRadius = null)
        {
            // If no known radius is provided, an approximate radius as if the shape was a sphere is
            // determined, which is no less than the minimum required for hydrostatic equilibrium.
            var radius = knownRadius ?? Math.Max(MinimumRadius, GetRadiusForMass(mass.Value));
            var flattening = Randomizer.Instance.NextDouble(0.1);
            return new Ellipsoid(radius, radius * (1 - flattening), 1, Position);
        }

        /// <summary>
        /// Calculates the mass at which the Stern-Levison parameter for this <see cref="Planemo"/>
        /// will be 1, given its orbital characteristics.
        /// </summary>
        /// <remarks>
        /// Also sets <see cref="Eccentricity"/> as a side effect, if the <see cref="Planemo"/>
        /// doesn't already have a defined orbit.
        /// </remarks>
        /// <exception cref="Exception">Cannot be called if this <see cref="Planemo"/> has no Orbit
        /// or <see cref="ICelestialLocation.ContainingCelestialRegion"/>.</exception>
        private protected double GetSternLevisonLambdaMass()
        {
            var semiMajorAxis = 0.0;
            if (Orbit.HasValue)
            {
                semiMajorAxis = Orbit.Value.SemiMajorAxis;
            }
            else if (ContainingCelestialRegion == null)
            {
                throw new Exception($"{nameof(GetSternLevisonLambdaMass)} cannot be called on a {nameof(Planemo)} without either an {nameof(Orbit)} or a {nameof(ContainingCelestialRegion)}");
            }
            else
            {
                // Even if this planetoid is not yet in a defined orbit, some orbital
                // characteristics must be determined early, in order to distinguish a dwarf planet
                // from a planet, which depends partially on orbital distance.
                semiMajorAxis = GetDistanceTo(ContainingCelestialRegion) * (1 + Eccentricity) / (1 - Eccentricity);
            }

            return Math.Sqrt(Math.Pow(semiMajorAxis, 3.0 / 2.0) / 2.5e-28);
        }
    }
}
