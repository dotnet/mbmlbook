// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    using UnclutteringYourInbox.Features;

    using MBMLViews;
    using MBMLViews.Annotations;

    using Microsoft.ML.Probabilistic.Collections;

    using InstanceSet = System.Collections.Generic.Dictionary<InputMode, System.Collections.Generic.IList<Inputs.Instance>>;

    /// <summary>
    /// Input mode
    /// </summary>
    [Flags]
    public enum InputMode
    {
        /// <summary>
        /// Training mode.
        /// </summary>
        Training = 1,

        /// <summary>
        /// Validation mode.
        /// </summary>
        Validation = 2,

        /// <summary>
        /// Testing mode.
        /// </summary>
        Testing = 4,

        /// <summary>
        /// Community training mode.
        /// </summary>
        CommunityTraining = 8,

        /// <summary>
        /// The train and validation mode. 
        /// Used for calculating posteriors for the training set and validation set together.
        /// </summary>
        TrainAndValidation = 16
    }

    /// <summary>
    /// The inputs.
    /// </summary>
    [Serializable]
    public class Inputs : IEquatable<Inputs>
    {
        /// <summary>
        /// The train and validation.
        /// </summary>
        [UsedImplicitly]
        private DataSet trainAndValidation;

        /// <summary>
        /// The train.
        /// </summary>
        private DataSet train;

        /// <summary>
        /// The validation.
        /// </summary>
        private DataSet validation;

        /// <summary>
        /// The test.
        /// </summary>
        private DataSet test;

        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public string Id
        {
            get { return this.UserName + " " + this.FeatureSet; }
        }

        /// <summary>
        /// Gets or sets the feature set.
        /// </summary>
        public FeatureSet FeatureSet { get; set; }

        /// <summary>
        /// Gets or sets the train data.
        /// </summary>
        public DataSet Train
        {
            get
            {
                return this.train;
            }

            set
            {
                this.train = value;
                this.train.FeatureSet = this.FeatureSet;
            }
        }

        /// <summary>
        /// Gets or sets the validation.
        /// </summary>
        public DataSet Validation
        {
            get
            {
                return this.validation;
            }

            set
            {
                this.validation = value;
                this.validation.FeatureSet = this.FeatureSet;
            }
        }

        /// <summary>
        /// Gets or sets the train and validation.
        /// </summary>
        public DataSet TrainAndValidation
        {
            get
            {
                return this.Train == null
                           ? null
                           : this.trainAndValidation
                             ?? new DataSet
                                    {
                                        FeatureSet = this.FeatureSet,
                                        Instances = this.Train.Instances.Concat(this.Validation.Instances).ToArray(),
                                        Name = (InputMode.Training | InputMode.Validation).ToString()
                                    };
            }

            set
            {
                this.trainAndValidation = value;
            }
        }

        /// <summary>
        /// Gets or sets the test data.
        /// </summary>
        public DataSet Test
        {
            get
            {
                return this.test;
            }

            set
            {
                this.test = value;
                this.test.FeatureSet = this.FeatureSet;
            }
        }

        /// <summary>
        /// Gets the feature names.
        /// </summary>
        public IList<string> FeatureNames
        {
            get
            {
                return this.FeatureSet == null
                           ? null
                           : this.FeatureSet.Features.SelectMany(f => f.Buckets.Select(b => b.ToString())).ToArray();
            }
        }

        /// <summary>
        /// Gets the total count.
        /// </summary>
        public int TotalCount
        {
            get { return this.Train == null ? 0 : this.Train.Count + this.Validation.Count + this.Test.Count; }
        }

        /// <summary>
        /// Gets the top senders.
        /// </summary>
        public Dictionary<string, double> TopSenders
        {
            get
            {
                var topSenderCounts = new Dictionary<FeatureBucket, int[]>();
                foreach (var instance in this.TrainAndValidation.Instances)
                {
                    foreach (var featureValue in instance.FeatureValues.Where(featureValue => featureValue.Key.Feature is Sender))
                    {
                        if (!topSenderCounts.ContainsKey(featureValue.Key))
                        {
                            topSenderCounts[featureValue.Key] = new int[2];
                        }

                        int idx = instance.Label ? 0 : 1;
                        topSenderCounts[featureValue.Key][idx] += (int)featureValue.Value;
                    }
                }

                return
                    topSenderCounts.OrderByDescending(ia => ia.Value[0] + ia.Value[1])
                        .Take(20)
                        .ToDictionary(
                            ia => string.Format("{0} ({1})", ia.Key.Name, ia.Value[0] + ia.Value[1]),
                            ia => (double)ia.Value[0] / (ia.Value[0] + ia.Value[1]));
            }
        }

        /// <summary>
        /// Inputs from the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="featureSetType">Type of the feature set.</param>
        /// <returns>
        /// The <see cref="Inputs" />
        /// </returns>
        public static Inputs FromUser(User user, FeatureSetType featureSetType)
        {
            return FromUser(user, FeatureSet.GetFeatureSet(user, featureSetType));
        }

        /// <summary>
        /// Inputs from the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="featureSet">The feature set.</param>
        /// <returns>
        /// The <see cref="Inputs" />
        /// </returns>
        public static Inputs FromUser(User user, FeatureSet featureSet)
        {
            Inputs inputs = new Inputs
            {
                UserName = user.Name.ToString(),
                FeatureSet = featureSet
            };

            var instanceSet = GetInstances(user, inputs.FeatureSet);
            inputs.SetInstances(instanceSet, inputs.FeatureSet);

            return inputs;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return new { this.FeatureSet, this.Id, this.Train, this.Validation, this.Test }.GetHashCode();
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Inputs other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Id == other.Id
                && this.FeatureSet.Equals(other.FeatureSet)
                && this.Train.Equals(other.Train)
                && this.Validation.Equals(other.Validation)
                && this.Test.Equals(other.Test);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var strings = new[]
                {
                    string.Format("Training messages: {0}", this.Train.Count),
                    string.Format("Validation messsages: {0}", this.Validation.Count),
                    string.Format("Test messsages: {0}", this.Test.Count)
                };

            return string.Join(", ", strings);
        }

        /// <summary>
        /// Gets the instances.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="includeSharedFeatures">if set to <c>true</c> [include shared features].</param>
        /// <returns>
        /// The <see cref="InstanceSet" />
        /// </returns>
        internal static InstanceSet GetInstances(User user, FeatureSet featureSet, bool includeSharedFeatures = true)
        {
            return new InstanceSet
                {
                    { InputMode.Training, user.TrainMessages.Select(m => Instance.FromMessage(m, user, featureSet, includeSharedFeatures)).ToArray() },
                    { InputMode.Validation, user.ValidationMessages.Select(m => Instance.FromMessage(m, user, featureSet, includeSharedFeatures)).ToArray() },
                    { InputMode.Testing, user.TestMessages.Select(m => Instance.FromMessage(m, user, featureSet, includeSharedFeatures)).ToArray() }
                };
        }

        /// <summary>
        /// Sets the instances.
        /// </summary>
        /// <param name="instanceSet">The instance set.</param>
        /// <param name="featureSet">The feature set.</param>
        private void SetInstances(InstanceSet instanceSet, FeatureSet featureSet)
        {
            this.Train = new DataSet
                             {
                                 FeatureSet = featureSet,
                                 Instances = instanceSet[InputMode.Training],
                                 Name = InputMode.Training.ToString()
                             };
            this.Validation = new DataSet
                                  {
                                      FeatureSet = featureSet,
                                      Instances = instanceSet[InputMode.Validation],
                                      Name = InputMode.Validation.ToString()
                                  };
            this.Test = new DataSet
                            {
                                FeatureSet = featureSet,
                                Instances = instanceSet[InputMode.Testing],
                                Name = InputMode.Testing.ToString()
                            };
        }

        /// <summary>
        /// Sets the instances.
        /// </summary>
        /// <param name="instanceSets">The instance sets.</param>
        /// <param name="featureSet">The feature set.</param>
        private void SetInstances(IList<InstanceSet> instanceSets, FeatureSet featureSet)
        {
            this.Train = new DataSet
                             {
                                 FeatureSet = featureSet,
                                 Instances = instanceSets.SelectMany(ia => ia[InputMode.Training]).ToArray(),
                                 Name = InputMode.Training.ToString()
                             };

            this.Validation = new DataSet
                                  {
                                      FeatureSet = featureSet,
                                      Instances = instanceSets.SelectMany(ia => ia[InputMode.Validation]).ToArray(),
                                      Name = InputMode.Validation.ToString()
                                  };

            this.Test = new DataSet
                            {
                                FeatureSet = featureSet,
                                Instances = instanceSets.SelectMany(ia => ia[InputMode.Testing]).ToArray(),
                                Name = InputMode.Testing.ToString()
                            };
        }

        /// <summary>
        /// The data set.
        /// </summary>
        public class DataSet : IEquatable<DataSet>
        {
            /// <summary>
            /// The labels.
            /// </summary>
            private bool[] labels;

            /// <summary>
            /// The positive instances.
            /// </summary>
            private Instance[] positiveInstances;

            /// <summary>
            /// The negative instances.
            /// </summary>
            private Instance[] negativeInstances;

            /// <summary>
            /// The personal sparse indices.
            /// </summary>
            private int[][] personalSparseIndices;

            /// <summary>
            /// The personal sparse values.
            /// </summary>
            private double[][] personalSparseValues;

            /// <summary>
            /// The personal sparse counts.
            /// </summary>
            private int[] personalSparseCounts;

            /// <summary>
            /// The shared sparse indices.
            /// </summary>
            private int[][] sharedSparseIndices;

            /// <summary>
            /// The shared sparse values.
            /// </summary>
            private double[][] sharedSparseValues;

            /// <summary>
            /// The shared sparse counts.
            /// </summary>
            private int[] sharedSparseCounts;

            /// <summary>
            /// The feature histograms.
            /// </summary>
            private Dictionary<string, Dictionary<string, double>> featureHistograms;

            /// <summary>
            /// The histograms.
            /// </summary>
            private Dictionary<IFeature, Dictionary<string, Counter>> histograms;

            /// <summary>
            /// The instances.
            /// </summary>
            private IList<Instance> instances;

            private FeatureSet featureSet;

            /// <summary>
            /// Gets the histograms.
            /// </summary>
            public Dictionary<IFeature, Dictionary<string, Counter>> Histograms
            {
                get
                {
                    if (this.histograms != null)
                    {
                        return this.histograms;
                    }

                    this.histograms = this.GetFeatureHistograms(this.FeatureSet.FeatureBuckets.Select(ia => ia.Feature).Distinct());
                    return this.histograms;
                }
            }

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            public string Name { get; set; }

            #region Data properties

            /// <summary>
            /// Gets or sets the message data.
            /// </summary>
            public IList<Instance> Instances
            {
                get
                {
                    return this.instances;
                }

                set
                {
                    this.instances = value;
                    if (value == null)
                    {
                        return;
                    }

                    if (this.FeatureSet == null)
                    {
                        return;
                    }

                    // set back references
                    this.instances.ForEach(ia => ia.FeatureSet = this.FeatureSet);
                }
            }

            /// <summary>
            /// Gets the labels.
            /// </summary>
            public bool[] Labels
            {
                get { return this.Instances == null ? null : this.labels ?? (this.labels = this.Instances.Select(ia => ia.Label).ToArray()); }
            }

            /// <summary>
            /// Gets the positive instances.
            /// </summary>
            /// <value>
            /// The positive instances.
            /// </value>
            public IList<Instance> PositiveInstances
            {
                get
                {
                    return this.positiveInstances
                           ?? (this.positiveInstances = this.Instances == null ? null : this.Instances.Where(ia => ia.Label).ToArray());
                }
            }

            /// <summary>
            /// Gets the negative instances.
            /// </summary>
            /// <value>
            /// The negative instances.
            /// </value>
            public IList<Instance> NegativeInstances
            {
                get
                {
                    return this.negativeInstances
                           ?? (this.negativeInstances = this.Instances == null ? null : this.Instances.Where(ia => !ia.Label).ToArray());
                }
            }

            /// <summary>
            /// Gets the count.
            /// </summary>
            public int Count
            {
                get { return this.Instances == null ? 0 : this.Instances.Count; }
            }

            /// <summary>
            /// Gets or sets the feature set.
            /// </summary>
            [XmlIgnore]
            public FeatureSet FeatureSet
            {
                get
                {
                    return this.featureSet;
                }

                set
                {
                    this.featureSet = value;
                    if (this.Count > 0)
                    {
                        this.Instances.ForEach(ia => ia.FeatureSet = value);
                    }
                }
            }

            /// <summary>
            /// Gets the personal sparse indices.
            /// </summary>
            public int[][] PersonalSparseIndices
            {
                get
                {
                    return this.personalSparseIndices
                           ?? (this.personalSparseIndices =
                               this.Instances.Select(
                                   ia =>
                                   ia.FeatureValues.Where(fbv => !fbv.Key.Feature.IsShared)
                                       .Select(fbv => this.FeatureSet.FeatureBuckets.IndexOf(fbv.Key))
                                       .ToArray()).ToArray());
                }
            }

            /// <summary>
            /// Gets the personal sparse values.
            /// </summary>
            public double[][] PersonalSparseValues
            {
                get
                {
                    return this.personalSparseValues
                           ?? (this.personalSparseValues =
                               this.Instances.Select(
                                   ia => ia.FeatureValues.Where(fbv => !fbv.Key.Feature.IsShared).Select(fbv => fbv.Value).ToArray())
                                   .ToArray());
                }
            }

            /// <summary>
            /// Gets the personal sparse counts.
            /// </summary>
            public int[] PersonalSparseCounts
            {
                get
                {
                    return this.personalSparseCounts
                           ?? (this.personalSparseCounts = this.PersonalSparseIndices.Select(ia => ia.Length).ToArray());
                }
            }

            /// <summary>
            /// Gets the shared sparse indices.
            /// </summary>
            public int[][] SharedSparseIndices
            {
                get
                {
                    return this.sharedSparseIndices
                              ?? (this.sharedSparseIndices =
                                  this.Instances.Select(
                                      ia =>
                                      ia.FeatureValues.Where(fbv => fbv.Key.Feature.IsShared)
                                          .Select(fbv => this.FeatureSet.FeatureBuckets.IndexOf(fbv.Key))
                                          .ToArray()).ToArray());
                }
            }

            /// <summary>
            /// Gets the shared sparse values.
            /// </summary>
            public double[][] SharedSparseValues
            {
                get
                {
                    return this.sharedSparseValues
                           ?? (this.sharedSparseValues =
                               this.Instances.Select(
                                   ia => ia.FeatureValues.Where(fbv => fbv.Key.Feature.IsShared).Select(fbv => fbv.Value).ToArray())
                                   .ToArray());
                }
            }

            /// <summary>
            /// Gets the shared sparse counts.
            /// </summary>
            public int[] SharedSparseCounts
            {
                get
                {
                    return this.sharedSparseCounts
                           ?? (this.sharedSparseCounts = this.SharedSparseIndices.Select(ia => ia.Length).ToArray());
                }
            }

            #endregion

            #region Feature Analysis
            /// <summary>
            /// Gets the feature histograms. 
            /// </summary>
            public Dictionary<string, Dictionary<string, double>> FeatureHistograms 
            {
                get
                {
                    if (this.featureHistograms != null)
                    {
                        return this.featureHistograms;
                    }

                    this.featureHistograms = this.Histograms
                        .ToDictionary(
                            ia => ia.Key.GetType().Name,
                            ia =>
                            ia.Value.ToDictionary(
                                inner => string.Format("{0} ({1})", inner.Key, inner.Value.Total),
                                inner => inner.Value.Fraction));

                    return this.featureHistograms;
                }
            }

            #endregion

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode()
            {
                return new { this.FeatureSet, this.Instances, this.Name }.GetHashCode();
            }

            /// <summary>
            /// Indicates whether the current object is equal to another object of the same type.
            /// </summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns>
            /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
            /// </returns>
            public bool Equals(DataSet other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                if (!(this.FeatureSet.Equals(other.FeatureSet) && this.Name == other.Name))
                {
                    return false;
                }

                if (this.Instances.Count != other.Instances.Count)
                {
                    return false;
                }

                for (int i = 0; i < this.Instances.Count; i++)
                {
                    if (!this.Instances[i].Equals(other.Instances[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String" /> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return this.Name;
            }

            /// <summary>
            /// Generates the binary permutations.
            /// </summary>
            /// <param name="count">The count.</param>
            /// <returns>An enumerator over permutations.</returns>
            internal static IEnumerable<double[]> GenerateBinaryPermutations(int count)
            {
                int possibleValueCount = (int)Math.Pow(2, count);

                if (possibleValueCount < 0 || possibleValueCount > 1000)
                {
                    // This happens if count is too large
                    yield return new double[] { };
                }
                else
                {
                    var combinations = Enumerable.Range(0, possibleValueCount).Select(ia => new BitArray(new[] { ia })).ToArray();
                    
                    foreach (var b in combinations)
                    {
                        bool[] bits = new bool[b.Count];
                        b.CopyTo(bits, 0);
                        
                        yield return bits.Take(count).Select(Convert.ToDouble).ToArray();
                    }
                }
            }

            /// <summary>
            /// Gets the feature histograms.
            /// </summary>
            /// <param name="features">The features.</param>
            /// <param name="filterFeatureBuckets">if set to <c>true</c> [filter feature buckets].</param>
            /// <returns>
            /// The feature histograms.
            /// </returns>
            internal Dictionary<IFeature, Dictionary<string, Counter>> GetFeatureHistograms(
                IEnumerable<IFeature> features, bool filterFeatureBuckets = true)
            {
                // functions for counting binary buckets
                Func<Instance, FeatureBucket, bool> posFunc = (ia, bucket) => ia.FeatureValues[bucket] > 0.5;
                Func<Instance, FeatureBucket, bool> negFunc = (ia, bucket) => ia.FeatureValues[bucket] < 0.5;

                var histogramDict = new Dictionary<IFeature, Dictionary<string, Counter>>();

                foreach (var feature in features)
                {
                    histogramDict[feature] = new Dictionary<string, Counter>();

                    foreach (var bucket in feature.Buckets)
                    {
                        if (bucket.Feature is BinaryFeature)
                        {
                            foreach (bool b in new[] { false, true })
                            {
                                string key = b.ToString();
                                histogramDict[feature][key] = new Counter
                                                                 {
                                                                     Description = key,
                                                                     Positive =
                                                                         this.PositiveInstances.Count(
                                                                             ia => b ? posFunc(ia, bucket) : negFunc(ia, bucket)),
                                                                     Negative =
                                                                         this.NegativeInstances.Count(
                                                                             ia => b ? posFunc(ia, bucket) : negFunc(ia, bucket))
                                                                 };
                            }
                        }
                        else if (bucket.Feature is OneOfNFeature || bucket.Feature is NumericFeature)
                        {
                            // Ignore MofNFeature and Threshold
                            
                            var key = bucket.Feature.GetDescription(bucket.Feature.Buckets.IndexOf(bucket)).SplitCamelCase();
                            histogramDict[feature][key] = new Counter
                                                              {
                                                                  Description = key,
                                                                  Positive =
                                                                      this.PositiveInstances.Count(
                                                                          ia => ia.FeatureValues.ContainsKey(bucket)),
                                                                  Negative =
                                                                      this.NegativeInstances.Count(
                                                                          ia => ia.FeatureValues.ContainsKey(bucket))
                                                              };
                        }
                    }

                    if (feature.Count > 100)
                    {
                        histogramDict[feature] =
                            histogramDict[feature].OrderByDescending(ia => ia.Value.Total)
                                .Take(20)
                                .ToDictionary(ia => ia.Key, ia => ia.Value);
                    }
                }

                return histogramDict;
            }

            /// <summary>
            /// The counter.
            /// </summary>
            public class Counter
            {
                /// <summary>
                /// Gets or sets the positive.
                /// </summary>
                public int Positive { get; set; }

                /// <summary>
                /// Gets or sets the negative.
                /// </summary>
                public int Negative { get; set; }

                /// <summary>
                /// Gets the total.
                /// </summary>
                public int Total 
                {
                    get { return this.Positive + this.Negative; } 
                }

                /// <summary>
                /// Gets the fraction.
                /// </summary>
                public double Fraction
                {
                    get { return this.Total == 0 ? 0.0 : (double)this.Positive / this.Total; }
                }

                /// <summary>
                /// Gets or sets the description.
                /// </summary>
                public string Description { get; set; }
            }
        }

        /// <summary>
        /// The instance (was message data).
        /// </summary>
        public class Instance : IEquatable<Instance>
        {
            /// <summary>
            /// Gets or sets the feature set.
            /// </summary>
            [XmlIgnore]
            public FeatureSet FeatureSet { get; set; }

            /// <summary>
            /// Gets or sets the feature values.
            /// </summary>
            public Dictionary<FeatureBucket, double> FeatureValues { get; set; }

            /// <summary>
            /// Gets the probability of the label being false.
            /// </summary>
            public double ProbabilityLabelFalse
            {
                get { return 1 - this.ProbabilityLabelTrue; }
            }

            /// <summary>
            /// Gets the probability of the label being true.
            /// </summary>
            public double ProbabilityLabelTrue
            {
                get { return this.Label ? 0.99 : 0.01; }
            }

            /// <summary>
            /// Gets or sets a value indicating whether [label].
            /// </summary>
            /// <value>
            ///   <c>true</c> if [label]; otherwise, <c>false</c>.
            /// </value>
            public bool Label { get; set; }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode()
            {
                return new { this.Label, this.FeatureSet, this.FeatureValues }.GetHashCode();
            }

            /// <summary>
            /// Indicates whether the current object is equal to another object of the same type.
            /// </summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns>
            /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
            /// </returns>
            public bool Equals(Instance other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                if (
                    !(this.Label == other.Label && this.FeatureSet.Equals(other.FeatureSet)
                      && this.FeatureValues.Count == other.FeatureValues.Count))
                {
                    return false;
                }

                foreach (var kvp in this.FeatureValues)
                {
                    if (!other.FeatureValues.ContainsKey(kvp.Key))
                    {
                        return false;
                    }

                    if (Math.Abs(kvp.Value - other.FeatureValues[kvp.Key]) > double.Epsilon)
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Instance from message.
            /// </summary>
            /// <param name="message">The message.</param>
            /// <param name="user">The user.</param>
            /// <param name="featureSet">The feature set.</param>
            /// <param name="includeSharedFeatures">if set to <c>true</c> [include shared features].</param>
            /// <returns>
            /// The <see cref="Instance" />.
            /// </returns>
            internal static Instance FromMessage(Message message, User user, FeatureSet featureSet, bool includeSharedFeatures)
            {
                return new Instance
                           {
                               FeatureSet = featureSet,
                               FeatureValues = featureSet.ComputeFeatureValues(user, message, includeSharedFeatures),
                               Label = message.IsRepliedTo
                           };
            }
        }
    }
}
