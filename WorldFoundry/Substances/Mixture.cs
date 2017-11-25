using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WorldFoundry.Substances
{
    /// <summary>
    /// Defines a mixture of <see cref="Substance"/>s in particular proportions.
    /// </summary>
    public class Mixture
    {
        /// <summary>
        /// The <see cref="MixtureComponent"/>s included in this mixture.
        /// </summary>
        public ICollection<MixtureComponent> Components { get; internal set; }

        /// <summary>
        /// When this <see cref="Mixture"/> is part of a larger overall <see cref="Mixture"/> which
        /// is stratified, this indicates at which 0-based layer this child <see cref="Mixture"/> is
        /// located in the greater <see cref="Mixture"/>.
        /// </summary>
        public int Layer { get; private set; }

        /// <summary>
        /// The child <see cref="Mixture"/>s included in this overall mixture.
        /// </summary>
        public ICollection<Mixture> Mixtures { get; internal set; }

        /// <summary>
        /// The proportion of this mixture in a larger overall mixture (mass fraction).
        /// </summary>
        public float Proportion { get; internal set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Mixture"/>.
        /// </summary>
        public Mixture() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Mixture"/> with the given properties.
        /// </summary>
        public Mixture(IEnumerable<MixtureComponent> components) => Components = new HashSet<MixtureComponent>(components);

        /// <summary>
        /// Initializes a new instance of <see cref="Mixture"/> with the given properties.
        /// </summary>
        public Mixture(int layer, IEnumerable<MixtureComponent> components) : this(components) => Layer = layer;

        /// <summary>
        /// Introduce a new <see cref="Substance"/> into the <see cref="Mixture"/> at the specified
        /// proportion. The proportions of the existing <see cref="Substance"/>s are reduced
        /// proportionately in order to "make room" for the specified proportion of the new <see cref="Substance"/>.
        /// </summary>
        /// <param name="substance">The new <see cref="Substance"/> to add.</param>
        /// <param name="proportion">The proportion at which to add the new <see cref="Substance"/>.</param>
        /// <param name="children">
        /// If true, adds the <see cref="Substance"/> to all child <see cref="Mixture"/>s, not to
        /// this overall <see cref="Mixture"/>.
        /// </param>
        public void AddComponent(Substance substance, float proportion, bool children = false)
        {
            RemoveComponent(substance, children);

            if (children)
            {
                if (Mixtures != null)
                {
                    foreach (var mixture in Mixtures)
                    {
                        mixture.AddComponent(substance, proportion);
                    }
                }
            }
            else
            {
                if (Components == null)
                {
                    Components = new HashSet<MixtureComponent>();
                }

                if (proportion <= 0)
                {
                    return;
                }
                else if (proportion >= 1)
                {
                    Components.Clear();
                }
                else
                {
                    BalanceProportionsForValue(1 - proportion);
                }

                Components.Add(new MixtureComponent { Substance = substance, Proportion = Math.Min(1, proportion) });
            }
        }

        /// <summary>
        /// Introduce a new child <see cref="Mixture"/> into this overall <see cref="Mixture"/>. The
        /// proportions of the existing child <see cref="Mixture"/>s are reduced proportionately in
        /// order to "make room" for the new child <see cref="Mixture"/>.
        /// </summary>
        /// <param name="mixture">The new child <see cref="Mixture"/> to add.</param>
        /// <param name="proportion">
        /// The proportion at which to add the new child <see cref="Mixture"/>. If unspecified, uses
        /// the <see cref="Proportion"/> defined on <paramref name="mixture"/>.
        /// </param>
        public void AddMixture(Mixture mixture)
        {
            if (mixture.Proportion <= 0)
            {
                return;
            }

            if (Mixtures == null)
            {
                Mixtures = new HashSet<Mixture>();
            }

            if (mixture.Proportion >= 1)
            {
                mixture.Proportion = Math.Min(1, mixture.Proportion);
                Mixtures.Clear();
            }
            else
            {
                BalanceProportionsForValue(1 - mixture.Proportion, true);
            }

            Mixtures.Add(mixture);
        }

        /// <summary>
        /// Reduces the proportions of all components at an even rate, if necessary, in order to stay
        /// at or below the specified total.
        /// </summary>
        /// <param name="total">The total target proportion, between 0 and 1.</param>
        /// <param name="children">
        /// If true, balances the proportions of the child <see cref="Mixture"/> s rather than the
        /// direct components.
        /// </param>
        internal void BalanceProportionsForValue(float total = 1, bool children = false)
        {
            var currentTotal = children ? (Mixtures?.Sum(m => m.Proportion) ?? 0) : (Components?.Sum(c => c.Proportion) ?? 0);
            if (currentTotal == total)
            {
                return;
            }

            var ratio = total / currentTotal;
            if (children)
            {
                if (Mixtures != null)
                {
                    foreach (var mixture in Mixtures)
                    {
                        mixture.Proportion *= ratio;
                    }
                }
            }
            else if (Components != null)
            {
                foreach (var component in Components)
                {
                    component.Proportion *= ratio;
                }
            }
        }

        /// <summary>
        /// Determines whether a <see cref="Substance"/> with the given characteristics exists in
        /// this <see cref="Mixture"/>.
        /// </summary>
        /// <param name="chemical">The <see cref="Chemical"/> to match.</param>
        /// <param name="phase">The <see cref="Phase"/> to match.</param>
        /// <returns>true if a <see cref="Substance"/> is matched; false otherwise.</returns>
        public bool ContainsSubstance(Chemical chemical, Phase phase)
            => Components?.FirstOrDefault(c => c.Substance.Chemical == chemical && (phase == Phase.Any || c.Substance.Phase == phase)) != null
            || (Mixtures?.Any(m => m.ContainsSubstance(chemical, phase)) ?? false);

        /// <summary>
        /// Copies the child <see cref="Mixture"/> at the specified layer and adds it as a new child
        /// <see cref="Mixture"/> at the top of the collection.
        /// </summary>
        /// <param name="layer">
        /// The 0-based layer index of the child <see cref="Mixture"/> to be copied.
        /// </param>
        /// <param name="proportion">The proportion to be assigned to the new layer (mass ratio).</param>
        public void CopyLayer(int layer, float proportion)
        {
            if (proportion <= 0)
            {
                return;
            }

            var original = GetChildAtLayer(layer);
            if (original == null)
            {
                return;
            }

            if (Mixtures == null)
            {
                Mixtures = new HashSet<Mixture>();
            }

            var newLayer = new Mixture()
            {
                Components = new HashSet<MixtureComponent>(original.Components),
                Layer = Mixtures.Max(m => m.Layer) + 1,
                Mixtures = new HashSet<Mixture>(original.Mixtures),
                Proportion = Math.Min(1, proportion),
            };

            AddMixture(newLayer);
        }

        /// <summary>
        /// Retrieves the child <see cref="Mixture"/> at the last layer.
        /// </summary>
        /// <returns>
        /// The child <see cref="Mixture"/> at the first layer, or null if no children exist.
        /// </returns>
        /// <remarks>
        /// If more than one child exists with the lowest <see cref="Layer"/> value, the first one
        /// found is returned.
        /// </remarks>
        public Mixture GetChildAtFirstLayer() => Mixtures?.FirstOrDefault(m => m.Layer == Mixtures.Min(x => x.Layer));

        /// <summary>
        /// Retrieves the child <see cref="Mixture"/> at the last layer.
        /// </summary>
        /// <returns>
        /// The child <see cref="Mixture"/> at the last layer, or null if no children exist.
        /// </returns>
        /// <remarks>
        /// If more than one child exists with the highest <see cref="Layer"/> value, the first one
        /// found is returned.
        /// </remarks>
        public Mixture GetChildAtLastLayer() => Mixtures?.FirstOrDefault(m => m.Layer == Mixtures.Max(x => x.Layer));

        /// <summary>
        /// Retrieves the child <see cref="Mixture"/> at the given layer.
        /// </summary>
        /// <param name="layer">The 0-based layer at which to retrieve a child <see cref="Mixture"/>.</param>
        /// <returns>
        /// The child <see cref="Mixture"/> at the given layer, or null if no such child exists.
        /// </returns>
        /// <remarks>
        /// If more than one child exists with the given <see cref="Layer"/> value, the first one
        /// found is returned.
        /// </remarks>
        public Mixture GetChildAtLayer(int layer) => Mixtures?.FirstOrDefault(m => m.Layer == layer);

        /// <summary>
        /// Determines if this mixture meets a set of <see cref="SubstanceRequirement"/> s.
        /// </summary>
        /// <param name="requirements">
        /// A list of <see cref="SubstanceRequirement"/> s which must be met.
        /// </param>
        /// <returns>
        /// An enumeration of all the <see cref="SubstanceRequirement"/> s which failed, along with
        /// the reason(s) for each failure.
        /// </returns>
        public IEnumerable<(SubstanceRequirement, SubstanceRequirementFailureType)> GetFailedRequirements(IEnumerable<SubstanceRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                var failureReason = MeetsRequirement(requirement);
                if (failureReason != SubstanceRequirementFailureType.None)
                {
                    yield return (requirement, failureReason);
                }
            }
        }

        /// <summary>
        /// Gets the first <see cref="MixtureComponent"/> in this <see cref="Mixture"/> that matches
        /// the given <see cref="Substance"/>.
        /// </summary>
        /// <param name="chemical">The <see cref="Chemical"/> to match.</param>
        /// <param name="phase">The <see cref="Phase"/> to match.</param>
        /// <returns>
        /// The matched <see cref="Substance"/>, or null if none of the components are present.
        /// </returns>
        public MixtureComponent GetSubstance(Chemical chemical, Phase phase)
            => Components?.FirstOrDefault(c => c.Substance.Chemical == chemical && c.Substance.Phase == phase);

        /// <summary>
        /// Gets the first <see cref="MixtureComponent"/> in this <see cref="Mixture"/> that matches
        /// the given <see cref="Substance"/>.
        /// </summary>
        /// <param name="substance">The <see cref="Substance"/> to match.</param>
        /// <returns>
        /// The matched <see cref="Substance"/>, or null if none of the components are present.
        /// </returns>
        public MixtureComponent GetSubstance(SubstanceRequirement requirement) => GetSubstance(requirement.Chemical, requirement.Phase);

        /// <summary>
        /// Retrieves the total proportion of the given <see cref="Substance"/> in all child <see
        /// cref="Mixture"/>s (mass ratio).
        /// </summary>
        /// <param name="chemical">The <see cref="Chemical"/> to match.</param>
        /// <param name="phase">The <see cref="Phase"/> to match.</param>
        /// <returns>
        /// The total proportion of the <see cref="Substance"/> in all child <see cref="Mixture"/>s
        /// (mass ratio).
        /// </returns>
        public float GetSubstanceProportionInAllChildren(Chemical chemical, Phase phase)
        {
            float proportion = 0;
            if (Mixtures != null)
            {
                foreach (var mixture in Mixtures)
                {
                    var match = mixture.GetSubstance(chemical, phase);
                    if (match != null)
                    {
                        proportion += match.Proportion * mixture.Proportion;
                    }
                }
            }

            return proportion;
        }

        /// <summary>
        /// Determines if this <see cref="Mixture"/> meets the given <see cref="SubstanceRequirement"/>.
        /// </summary>
        /// <param name="requirement">The <see cref="SubstanceRequirement"/> to test.</param>
        /// <returns>
        /// A <see cref="SubstanceRequirementFailureType"/> indicating why the <see cref="Mixture"/>
        /// fails the requirement (may be <see cref="SubstanceRequirementFailureType.None"/>, if it passes).
        /// </returns>
        public SubstanceRequirementFailureType MeetsRequirement(SubstanceRequirement requirement)
        {
            var match = GetSubstance(requirement);
            if (match == null)
            {
                return requirement.MinimumProportion > 0
                    ? SubstanceRequirementFailureType.Missing
                    : SubstanceRequirementFailureType.Other;
            }

            var failureReason = SubstanceRequirementFailureType.None;

            if (match.Substance.Phase != requirement.Phase &&
                requirement.Phase != Phase.Any && match.Substance.Phase != Phase.Any)
            {
                failureReason = SubstanceRequirementFailureType.WrongPhase;
            }

            if (match.Proportion < requirement.MinimumProportion)
            {
                failureReason = failureReason | SubstanceRequirementFailureType.TooLittle;
            }

            if (requirement.MaximumProportion.HasValue
                && match.Proportion > requirement.MaximumProportion)
            {
                failureReason = failureReason | SubstanceRequirementFailureType.TooMuch;
            }

            return failureReason;
        }

        /// <summary>
        /// Remove the given <see cref="Substance"/> from the <see cref="Mixture"/>.
        /// </summary>
        /// <param name="substance">The <see cref="Substance"/> to remove.</param>
        /// <param name="children">
        /// If true, removes the <see cref="Substance"/> from all child <see cref="Mixture"/> s, not
        /// from this overall <see cref="Mixture"/>.
        /// </param>
        public void RemoveComponent(Chemical chemical, Phase phase, bool children = false)
        {
            if (children)
            {
                if (Mixtures != null)
                {
                    foreach (var mixture in Mixtures)
                    {
                        mixture.RemoveComponent(chemical, phase);
                    }
                }
            }
            else if (Components != null)
            {
                var match = GetSubstance(chemical, phase);
                if (match != null)
                {
                    Components.Remove(match);
                }
            }
        }
    }
}
