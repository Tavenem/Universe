using System;
using System.Collections.Generic;
using System.Linq;
using Troschuetz.Random;

namespace WorldFoundry.Substances
{
    /// <summary>
    /// Defines a mixture of components in particular proportions.
    /// </summary>
    public class Mixture : DataItem
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
        /// Mixes all child layers according to their proportions, then combines them with this <see
        /// cref="Mixture"/>'s own <see cref="Components"/>, so that it no longer has child <see cref="Mixtures"/>.
        /// </summary>
        public void AbsorbLayers()
        {
            while (Mixtures?.Count > 1)
            {
                CombineLayers(GetChildAtFirstLayer(), GetChildAtLastLayer());
            }
            if (Components == null)
            {
                Components = new HashSet<MixtureComponent>();
            }
            var source = GetChildAtFirstLayer();
            if (source?.Components != null)
            {
                foreach (var component in source.Components)
                {
                    var newProportion = GetProportion(component.Chemical, component.Phase) + component.Proportion;

                    var oldComponent = GetComponent(component.Chemical, component.Phase);
                    if (TMath.AreEqual(newProportion, oldComponent?.Proportion ?? 0))
                    {
                        continue;
                    }
                    else if (oldComponent == null)
                    {
                        Components.Add(new MixtureComponent
                        {
                            Chemical = component.Chemical,
                            Phase = component.Phase,
                            Proportion = newProportion,
                        });
                    }
                    else
                    {
                        oldComponent.Proportion = newProportion;
                    }
                }
            }
            BalanceProportions();
            Mixtures?.Remove(source);
        }

        /// <summary>
        /// Introduce a new <see cref="Chemical"/> into the <see cref="Mixture"/> in the specified
        /// <see cref="Phase"/>, at the specified proportion. The proportions of the existing <see
        /// cref="Components"/> are reduced proportionately in order to "make room" for the specified
        /// proportion of the new component.
        /// </summary>
        /// <param name="chemical">The new <see cref="Chemical"/> to add.</param>
        /// <param name="phase">THe <see cref="Phase"/> in which to add the <paramref name="chemical"/>.</param>
        /// <param name="proportion">The proportion at which to add the <paramref name="chemical"/>.</param>
        /// <param name="children">
        /// If true, adds the <paramref name="chemical"/> to all child <see cref="Mixture"/>s, not
        /// to this overall <see cref="Mixture"/>.
        /// </param>
        public void AddComponent(Chemical chemical, Phase phase, float proportion, bool children = false)
        {
            RemoveComponent(chemical, phase, children);

            if (children)
            {
                if (Mixtures != null)
                {
                    foreach (var mixture in Mixtures)
                    {
                        mixture.AddComponent(chemical, phase, proportion);
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
                    BalanceProportions(1 - proportion);
                }

                Components.Add(new MixtureComponent
                {
                    Chemical = chemical,
                    Phase = phase,
                    Proportion = Math.Min(1, proportion),
                });
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
                BalanceProportions(1 - mixture.Proportion, true);
            }

            Mixtures.Add(mixture);
        }

        /// <summary>
        /// Adjusts the proportions of all components at an even rate, if necessary, in order to
        /// match the specified total.
        /// </summary>
        /// <param name="total">The total target proportion, between 0 and 1.</param>
        /// <param name="children">
        /// If true, balances the proportions of the child <see cref="Mixtures"/> rather than the
        /// direct components.
        /// </param>
        internal void BalanceProportions(float total = 1, bool children = false)
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
        /// Combines two layers, according to their proportions. Leaves the <paramref
        /// name="destination"/> layer and removes the <paramref name="source"/> layer when the
        /// operation is complete.
        /// </summary>
        /// <param name="destination">The layer into which <paramref name="source"/> will be mixed.</param>
        /// <param name="source">The layer which will be mixed into <paramref name="destination"/>.</param>
        internal void CombineLayers(Mixture destination, Mixture source)
        {
            var ratio = source.Proportion / destination.Proportion;
            if (destination.Components == null)
            {
                destination.Components = new HashSet<MixtureComponent>();
            }
            foreach (var component in source.Components)
            {
                var newProportion = destination.GetProportion(component.Chemical, component.Phase) + component.Proportion * ratio;

                var oldComponent = destination.GetComponent(component.Chemical, component.Phase);
                if (TMath.AreEqual(newProportion, oldComponent?.Proportion ?? 0))
                {
                    continue;
                }
                else if (oldComponent == null)
                {
                    destination.Components.Add(new MixtureComponent
                    {
                        Chemical = component.Chemical,
                        Phase = component.Phase,
                        Proportion = newProportion,
                    });
                }
                else
                {
                    oldComponent.Proportion = newProportion;
                }
            }
            destination.BalanceProportions();
            foreach (var mixture in Mixtures?.Where(x => x.Layer > source.Layer))
            {
                mixture.Layer--;
            }
            Mixtures?.Remove(source);
        }

        /// <summary>
        /// Determines whether a component with the given characteristics exists in this <see cref="Mixture"/>.
        /// </summary>
        /// <param name="chemical">The <see cref="Chemical"/> to match.</param>
        /// <param name="phase">The <see cref="Phase"/> to match.</param>
        /// <returns>true if a component is matched; false otherwise.</returns>
        public bool ContainsSubstance(Chemical chemical, Phase phase)
            => Components?.FirstOrDefault(c => c.Chemical == chemical && (phase == Phase.Any || c.Phase == phase)) != null
            || (Mixtures?.Any(m => m.ContainsSubstance(chemical, phase)) ?? false);

        /// <summary>
        /// Copies the child <see cref="Mixture"/> at the specified layer and adds it as a new child
        /// <see cref="Mixture"/> at the top of the collection.
        /// </summary>
        /// <param name="layer">
        /// The 0-based layer index of the child <see cref="Mixture"/> to be copied.
        /// </param>
        /// <param name="proportion">The proportion to be assigned to the new layer (mass ratio).</param>
        /// <remarks>If no layer with the given index exists, this method does nothing.</remarks>
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

            var newLayer = original.GetDeepCopy();
            newLayer.Layer = (Mixtures?.Max(m => m.Layer) ?? -1) + 1;
            newLayer.Proportion = Math.Min(1, proportion);

            AddMixture(newLayer);
        }

        /// <summary>
        /// Gets a deep copy of this <see cref="Mixture"/>.
        /// </summary>
        /// <returns>A deep copy of this <see cref="Mixture"/>.</returns>
        /// <remarks>
        /// This <see cref="DataItem.ID"/> is not copied. The individual <see
        /// cref="MixtureComponent"/>s are shallow-copied.
        /// </remarks>
        public Mixture GetDeepCopy()
        {
            var newMixture = new Mixture()
            {
                Components = new HashSet<MixtureComponent>(),
                Layer = Layer,
                Mixtures = Mixtures == null ? new HashSet<Mixture>() : new HashSet<Mixture>(Mixtures),
                Proportion = Proportion,
            };
            if (Components != null)
            {
                foreach (var component in Components)
                {
                    newMixture.Components.Add(component.GetShallowCopy());
                }
            }
            if (Mixtures != null)
            {
                foreach (var mixture in Mixtures)
                {
                    newMixture.Mixtures.Add(mixture.GetDeepCopy());
                }
            }
            return newMixture;
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
        /// Gets the first <see cref="MixtureComponent"/> in this <see cref="Mixture"/> that matches
        /// the given properties.
        /// </summary>
        /// <param name="chemical">The <see cref="Chemical"/> to match.</param>
        /// <param name="phase">The <see cref="Phase"/> to match.</param>
        /// <returns>The matched component, or null if no components match the criteria.</returns>
        public MixtureComponent GetComponent(Chemical chemical, Phase phase)
            => Components?.FirstOrDefault(c => c.Chemical == chemical && c.Phase == phase);

        /// <summary>
        /// Gets the first <see cref="MixtureComponent"/> in this <see cref="Mixture"/> that matches
        /// the given <see cref="ComponentRequirement"/>.
        /// </summary>
        /// <param name="requirement">The <see cref="ComponentRequirement"/> to match.</param>
        /// <returns>
        /// The matched component, or null if no components match the <see cref="ComponentRequirement"/>.
        /// </returns>
        public MixtureComponent GetComponent(ComponentRequirement requirement) => GetComponent(requirement.Chemical, requirement.Phase);

        /// <summary>
        /// Gets all <see cref="MixtureComponent"/>s in this <see cref="Mixture"/> that match any
        /// <see cref="Phase"/> of the given <see cref="Chemical"/>.
        /// </summary>
        /// <param name="chemical">The <see cref="Chemical"/> to match.</param>
        /// <returns>
        /// An enumeration of the matched components, or null if the <see cref="Chemical"/> is not present.
        /// </returns>
        public IEnumerable<MixtureComponent> GetComponentPhases(Chemical chemical)
            => Components?.Where(c => c.Chemical == chemical);

        /// <summary>
        /// Determines if this mixture meets a set of <see cref="ComponentRequirement"/> s.
        /// </summary>
        /// <param name="requirements">
        /// A list of <see cref="ComponentRequirement"/> s which must be met.
        /// </param>
        /// <returns>
        /// An enumeration of all the <see cref="ComponentRequirement"/> s which failed, along with
        /// the reason(s) for each failure.
        /// </returns>
        public virtual IEnumerable<(ComponentRequirement, ComponentRequirementFailureType)> GetFailedRequirements(IEnumerable<ComponentRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                var failureReason = MeetsRequirement(requirement);
                if (failureReason != ComponentRequirementFailureType.None)
                {
                    yield return (requirement, failureReason);
                }
            }
        }

        /// <summary>
        /// Retrieves the proportion of the given component in this <see cref="Mixture"/> (mass ratio).
        /// </summary>
        /// <param name="chemical">The <see cref="Chemical"/> to match.</param>
        /// <param name="phase">The <see cref="Phase"/> to match.</param>
        /// <param name="children">
        /// If true, checks for the total proportion in all child <see cref="Mixtures"/> (weighted by
        /// their own <see cref="Proportion"/>s).
        /// </param>
        /// <returns>The proportion of the component in this <see cref="Mixture"/> (mass ratio).</returns>
        public float GetProportion(Chemical chemical, Phase phase, bool children = false)
        {
            if (children)
            {
                return Mixtures?.Sum(x => x.GetProportion(chemical, phase) * x.Proportion) ?? 0;
            }
            else if (phase == Phase.Any)
            {
                return GetComponentPhases(chemical).Sum(x => x.Proportion);
            }
            else
            {
                return GetComponent(chemical, phase)?.Proportion ?? 0;
            }
        }

        /// <summary>
        /// Determines if this <see cref="Mixture"/> meets the given <see cref="ComponentRequirement"/>.
        /// </summary>
        /// <param name="requirement">The <see cref="ComponentRequirement"/> to test.</param>
        /// <returns>
        /// A <see cref="ComponentRequirementFailureType"/> indicating why the <see cref="Mixture"/>
        /// fails the requirement (may be <see cref="ComponentRequirementFailureType.None"/>, if it passes).
        /// </returns>
        public ComponentRequirementFailureType MeetsRequirement(ComponentRequirement requirement)
        {
            var failureType = ComponentRequirementFailureType.None;

            float proportion = 0;
            var matches = GetComponentPhases(requirement.Chemical);
            if (requirement.Phase == Phase.Any)
            {
                if (matches.Any())
                {
                    proportion = matches.Sum(x => x.Proportion);
                }
            }
            else
            {
                var match = GetComponent(requirement.Chemical, requirement.Phase);
                if (match != null)
                {
                    proportion = match.Proportion;
                }
                else if (matches.Any())
                {
                    failureType |= ComponentRequirementFailureType.WrongPhase;
                }
            }
            if (proportion < requirement.MinimumProportion)
            {
                failureType |= ComponentRequirementFailureType.TooLittle;
            }
            else if (proportion > requirement.MaximumProportion)
            {
                failureType |= ComponentRequirementFailureType.TooMuch;
            }

            return failureType;
        }

        /// <summary>
        /// Remove any component from the <see cref="Mixture"/> which matches the given criteria.
        /// </summary>
        /// <param name="chemical">The <see cref="Chemical"/> to be removed.</param>
        /// <param name="phase">
        /// The <see cref="Phase"/> to be removed. May be <see cref="Phase.Any"/> to remove all phases.
        /// </param>
        /// <param name="children">
        /// If true, removes any matching component from all child <see cref="Mixture"/>s, not from
        /// this overall <see cref="Mixture"/>.
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
                if (phase == Phase.Any)
                {
                    foreach (var match in GetComponentPhases(chemical).ToList())
                    {
                        Components.Remove(match);
                    }
                }
                else
                {
                    var match = GetComponent(chemical, phase);
                    if (match != null)
                    {
                        Components.Remove(match);
                    }
                }
            }
        }

        /// <summary>
        /// Removes any child <see cref="Mixtures"/> with no <see cref="Components"/>, and adjusts
        /// the <see cref="Proportion"/>s of the remaining children proportionately.
        /// </summary>
        public void RemoveEmptyChildren()
        {
            var empties = Mixtures?.Where(x => x.Components.Count == 0);
            foreach (var empty in empties)
            {
                Mixtures.Remove(empty);
                BalanceProportions(1, true);
            }
        }

        /// <summary>
        /// Adjusts the proportion of a component in the <see cref="Mixture"/> to the specified
        /// value. The proportions of the existing components are adjusted proportionately in order
        /// to accommodate for the specified proportion. If the component was not already present, it
        /// is added to the <see cref="Mixture"/>.
        /// </summary>
        /// <param name="chemical">The <see cref="Chemical"/> to adjust.</param>
        /// <param name="phase">The <see cref="Phase"/> of chemical to adjust.</param>
        /// <param name="proportion">The proportion at which to add the new component.</param>
        /// <param name="children">
        /// If true, adjusts the proportion of the component in all child <see cref="Mixture"/> s,
        /// not in this overall <see cref="Mixture"/>.
        /// </param>
        public void SetProportion(Chemical chemical, Phase phase, float proportion, bool children = false)
        {
            if (children)
            {
                if (Mixtures != null)
                {
                    foreach (var mixture in Mixtures)
                    {
                        mixture.SetProportion(chemical, phase, proportion);
                    }
                }
            }
            else if (proportion <= 0)
            {
                RemoveComponent(chemical, phase);
            }
            else
            {
                if (Components == null)
                {
                    Components = new HashSet<MixtureComponent>();
                }

                var substance = GetComponent(chemical, phase);
                if (TMath.AreEqual(proportion, (substance?.Proportion ?? 0)))
                {
                    return;
                }
                else
                {
                    AddComponent(chemical, phase, proportion);
                }
            }
        }
    }
}
