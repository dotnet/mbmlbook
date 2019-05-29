// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using UnclutteringYourInbox.Features;

    using MBMLViews;

    using Microsoft.ML.Probabilistic.Collections;

    /// <summary>
    /// The inputs collection.
    /// </summary>
    [Serializable]
    public class InputsCollection : IEquatable<InputsCollection>
    {
        /// <summary>
        /// The inputs collection.
        /// </summary>
        private readonly List<Inputs> inputsCollection = new List<Inputs>();

        /// <summary>
        /// The feature histograms.
        /// </summary>
        private Dictionary<string, Dictionary<string, double>> featureHistograms;

        /// <summary>
        /// Gets or sets the inputs.
        /// </summary>
        public IList<Inputs> Inputs
        {
            get
            {
                return this.inputsCollection.ToList();
            }

            set
            {
                if (value != null)
                {
                    value.ForEach(this.Add);
                }
            }
        }

        /// <summary>
        /// Gets the data set sizes.
        /// </summary>
        public Dictionary<string, object> DataSetSizes
        {
            get
            {
                return this.inputsCollection.ToDictionaryForTable(
                    ia => ia.UserName,
                    new Dictionary<string, Func<Inputs, int>>
                        {
                            { "Train", ia => ia.Train.Count },
                            { "Validation", ia => ia.Validation.Count },
                            { "Test", ia => ia.Test.Count },
                            { "User Total", ia => ia.TotalCount }
                        },
                    false,
                    new Dictionary<string, Func<IList<int>, object>>
                        {
                            { "Total", ia => ia.Sum() },
                            // Miss off the last one because it's the "Combined" user and hence already total
                            { "Average", ia => ia.Take(ia.Count - 1).Average() }
                        },
                    string.Empty);
            }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count
        {
            get { return this.inputsCollection.Count; }
        }

        /// <summary>
        /// Gets the feature histograms.
        /// </summary>
        public Dictionary<string, Dictionary<string, double>> FeatureHistograms
        {
            get
            {
                if (this.Inputs == null || this.Inputs.Count == 0)
                {
                    return null;
                }

                if (this.featureHistograms != null)
                {
                    return this.featureHistograms;
                }

                this.featureHistograms = new Dictionary<string, Dictionary<string, double>>();

                var histograms = new Dictionary<Type, Dictionary<string, Inputs.DataSet.Counter>>();

                foreach (var inputs in this.Inputs)
                {
                    var dataSet = inputs.TrainAndValidation;
                    var features = dataSet.FeatureSet.FeatureBuckets.Select(ia => ia.Feature).Where(ia => !(ia is IConfigurableFeature)).Distinct();
                    var histogram = dataSet.GetFeatureHistograms(features);

                    foreach (var kvp in histogram)
                    {
                        var featureType = kvp.Key.GetType();
                        if (!histograms.ContainsKey(featureType))
                        {
                            histograms[featureType] = new Dictionary<string, Inputs.DataSet.Counter>();
                        }

                        foreach (var bucket in kvp.Value)
                        {
                            var bucketKey = bucket.Value.Description;
                            var counter = bucket.Value;
                            
                            if (!histograms[featureType].ContainsKey(bucketKey))
                            {
                                histograms[featureType][bucketKey] = new Inputs.DataSet.Counter { Description = bucketKey };
                            }

                            histograms[featureType][bucketKey].Positive += counter.Positive;
                            histograms[featureType][bucketKey].Negative += counter.Negative;
                        }
                    }
                }

                this.featureHistograms = histograms.ToDictionary(
                    ia => ia.Key.Name,
                    ia =>
                    ia.Value.ToDictionary(
                        inner => string.Format("{0}\n({1})", inner.Value.Description, inner.Value.Total),
                        inner => inner.Value.Fraction));

                return this.featureHistograms;
            }
        }

        /// <summary>
        /// The contains.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <returns>
        /// The <see cref="bool" />.
        /// </returns>
        public bool Contains(Inputs inputs)
        {
            return this.inputsCollection.Contains(inputs);
        }

        /// <summary>
        /// Adds the specified inputs.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        public void Add(Inputs inputs)
        {
            if (inputs != null && !this.inputsCollection.Contains(inputs))
            {
                this.inputsCollection.Add(inputs);
            }
        }

        /// <summary>
        /// Adds the range of inputs.
        /// </summary>
        /// <param name="inputsEnumerable">The inputs enumerable.</param>
        public void AddRange(IEnumerable<Inputs> inputsEnumerable)
        {
            if (inputsEnumerable != null)
            {
                inputsEnumerable.ForEach(this.Add);
            }
        }

        /// <summary>
        /// Adds the specified inputs collection.
        /// </summary>
        /// <param name="inputs">The inputs collection.</param>
        public void Add(InputsCollection inputs)
        {
            if (inputs != null)
            {
                this.AddRange(inputs.Inputs);
            }
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(InputsCollection other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }
            
            if (this.Inputs.Count != other.Inputs.Count)
            {
                return false;
            }

            for (int i = 0; i < this.Inputs.Count; i++)
            {
                if (!this.Inputs[i].Equals(other.Inputs[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}