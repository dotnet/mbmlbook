// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Math;

    /// <summary>
    /// The document and vocabulary information in the corpus.
    /// </summary>
    public class CorpusInformation
    {
        /// <summary>
        /// Gets the vocabulary.
        /// </summary>
        public string[] Vocabulary { get; protected set; }

        /// <summary>
        /// Gets the vocabulary to vocabulary index.
        /// </summary>
        public Dictionary<string, int> VocabularyToVocabularyIndex { get; protected set; }

        /// <summary>
        /// Gets inverse document frequencies for the vocabulary.
        /// </summary>
        public double[] VocabularyIdfs { get; protected set; }

        /// <summary>
        /// Gets the number of documents for each word in the vocabulary.
        /// </summary>
        public int[] DocumentCounts { get; protected set; }

        /// <summary>
        /// Gets the documents.
        /// </summary>
        public int[][] Documents { get; protected set; }

        /// <summary>
        /// The number of documents.
        /// </summary>
        public int NumberOfDocuments => this.Documents?.Length ?? 0;

        /// <summary>
        /// The number of words.
        /// </summary>
        public int NumberOfWords => this.Vocabulary?.Length ?? 0;

        /// <summary>
        /// Extracts information from a list of documents.
        /// </summary>
        /// <param name="docs">
        /// Array of documents
        /// </param>
        /// <returns>
        /// The corpus information.
        /// </returns>
        public static CorpusInformation FromDocs(string[] docs)
        {
            var vocabIndicesAndCountsForDocs = new Dictionary<int, int>[docs.Length];
            var vocabToVocabIndex = new Dictionary<string, int>();
            var vocabToDocumentCount = new Dictionary<string, int>();
            var documentCount = (double)docs.Length;
            var documents = new int[docs.Length][];

            var vocabIndex = 0;
            for (var documentIndex = 0; documentIndex < documentCount; documentIndex++)
            {
                if (documentIndex % 10000 == 0)
                {
                    Console.WriteLine(@"Processing " + documentIndex + @"/" + docs.Length);
                }
                
                var document = Tokenizer.GetTokensFromPreProcessedDoc(docs[documentIndex]);
                vocabIndicesAndCountsForDocs[documentIndex] = new Dictionary<int, int>();
                var counts = document.GroupBy(token => token).ToDictionary(grp => grp.Key, grp => grp.Count());
                foreach (var kvp in counts)
                {
                    var token = kvp.Key;
                    if (!vocabToVocabIndex.ContainsKey(token))
                    {
                        vocabToDocumentCount[token] = 0;
                        vocabToVocabIndex[token] = vocabIndex++;
                    }

                    vocabToDocumentCount[token] += 1;
                    vocabIndicesAndCountsForDocs[documentIndex][vocabToVocabIndex[token]] = kvp.Value;
                }

                documents[documentIndex] = document.Select(token => vocabToVocabIndex[token]).ToArray();
            }

            // Vocabulary indices are 0-based consecutive indices. So order the vocab strings by index
            // to get a vocabulary ordering consistent with the above.
            var vocabulary = vocabToVocabIndex.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToArray();
            var documentCounts = vocabulary.Select(v => vocabToDocumentCount[v]).ToArray();
            var vocabIdfs = vocabulary.Select(v => Math.Log(documentCount / (1.0 + vocabToDocumentCount[v]))).ToArray();

            return new CorpusInformation
            {
                VocabularyToVocabularyIndex = vocabToVocabIndex,
                Vocabulary = vocabulary,
                VocabularyIdfs = vocabIdfs,
                DocumentCounts = documentCounts,
                Documents = documents
            };
        }

        /// <summary>
        /// Calculates the corpus information.
        /// </summary>
        /// <param name="corpus">
        /// The array of texts.
        /// </param>
        /// <param name="vocabularyThreshold">
        /// Optional vocabulary threshold.If set, terms need to be seen at least this many times to be considered as part of the vocabulary.
        /// </param>
        /// <returns>
        /// The corpus information.
        /// </returns>
        public static CorpusInformation BuildCorpusInformation(string[] corpus, int? vocabularyThreshold = null)
        {
            Console.WriteLine(@"Building vocabulary... ");
            var docAndVocabularyInfo = CorpusInformation.FromDocs(corpus);
            Console.WriteLine($@"Vocabulary size: {docAndVocabularyInfo.NumberOfWords}");

            if (vocabularyThreshold.HasValue)
            {
                Console.WriteLine($@"Deleting words seen less than {vocabularyThreshold.Value} times...");
                docAndVocabularyInfo = docAndVocabularyInfo.ConvertToThresholdedVocabulary(vocabularyThreshold);
                Console.WriteLine($@"Vocabulary size after deletion: {docAndVocabularyInfo.NumberOfWords}");
            }

            return docAndVocabularyInfo;
        }

        /// <summary>
        /// Restricts the current instance to a sub-vocabulary
        /// </summary>
        /// <param name="subVocabulary">The sub-vocabulary</param>
        /// <returns>The document and vocabulary information for the sub-vocabulary.</returns>
        public virtual CorpusInformation ConvertToSubVocabulary(List<string> subVocabulary)
        {
            var result = new CorpusInformation();
            var subVocabDistinct = subVocabulary.Distinct().ToList();
            var subVocabSize = subVocabDistinct.Count;
            var subVocabIndexToVocabIndex =
                subVocabDistinct.Select(word => this.VocabularyToVocabularyIndex[word]).ToArray();

            var subVocabToSubVocabIndex = subVocabDistinct
                .Select((id, idx) => new KeyValuePair<string, int>(id, idx)).ToDictionary(x => x.Key, y => y.Value);

            result.Documents = new int[this.NumberOfDocuments][];

            for (var docIndex = 0; docIndex < this.NumberOfDocuments; docIndex++)
            {
                result.Documents[docIndex] = this.Documents[docIndex]
                    .Select(idx => this.Vocabulary[idx]).Where(token => subVocabToSubVocabIndex.ContainsKey(token))
                    .Select(token => subVocabToSubVocabIndex[token]).ToArray();
            }

            result.DocumentCounts = new int[subVocabSize];
            result.VocabularyIdfs = new double[subVocabSize];

            for (var subVocabIndex = 0; subVocabIndex < subVocabSize; subVocabIndex++)
            {
                result.DocumentCounts[subVocabIndex] =
                    this.DocumentCounts[subVocabIndexToVocabIndex[subVocabIndex]];
                result.VocabularyIdfs[subVocabIndex] =
                    this.VocabularyIdfs[subVocabIndexToVocabIndex[subVocabIndex]];
            }

            result.Vocabulary = subVocabDistinct.ToArray();
            result.VocabularyToVocabularyIndex = subVocabToSubVocabIndex;

            return result;
        }

        /// <summary>
        /// The convert to vocabulary filtered by various thresholds.
        /// </summary>
        /// <param name="documentCountThreshold">
        /// The document count threshold.
        /// </param>
        /// <returns>
        /// The updated information.
        /// </returns>
        public CorpusInformation ConvertToThresholdedVocabulary(
            int? documentCountThreshold)
        {
            var docThresholdedVocabulary = new HashSet<string>(this.Vocabulary);
            var tfidfThresholdedVocabulary = new HashSet<string>(this.Vocabulary);

            if (documentCountThreshold.HasValue)
            {
                var indices = this.DocumentCounts.Select((idx, cnt) => new { idx, cnt })
                    .Where(pr => pr.cnt >= documentCountThreshold.Value).Select(pr => pr.idx).ToArray();

                docThresholdedVocabulary = new HashSet<string>(
                    this.VocabularyToVocabularyIndex
                        .Where(kvp => this.DocumentCounts[kvp.Value] >= documentCountThreshold.Value)
                        .Select(kvp => kvp.Key));
            }

            var subVocab = docThresholdedVocabulary.Intersect(tfidfThresholdedVocabulary).ToList();

            return this.ConvertToSubVocabulary(subVocab);
        }

        /// <summary>
        /// Gets the  word indices for a new document.
        /// </summary>
        /// <param name="doc">
        /// The document.
        /// </param>
        /// <returns>
        /// The ordered array of word indices/>.
        /// </returns>
        public virtual int[] GetWordIndices(string doc)
        {
            var document = Tokenizer.GetTokensFromPreProcessedDoc(doc)
                .Where(token => this.VocabularyToVocabularyIndex.ContainsKey(token)).ToArray();
            int[] result = document.Select(token => this.VocabularyToVocabularyIndex[token]).ToArray();

            return result;
        }

        /// <summary>
        /// Normalizes a sparse vector using the L2 norm.
        /// </summary>
        /// <param name="sparseVector">
        /// The sparse vector.
        /// </param>
        /// <returns>
        /// The normalized sparse vector
        /// </returns>
        private static SparseVector Normalize(SparseVector sparseVector)
        {
            var sumSquared = sparseVector.Inner(sparseVector);
            if (sumSquared > 1e-10)
            {
                return (SparseVector)(sparseVector / Math.Sqrt(sumSquared));
            }
            else
            {
                return sparseVector;
            }
        }
    }
}
