// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews
{
    using System;
    using System.Linq;

    /// <summary>
    /// The masked matrix validity.
    /// </summary>
    public enum MaskedMatrixValidity
    {
        /// <summary>
        /// masked matrix is valid.
        /// </summary>
        Valid,

        /// <summary>
        /// one of the properties is null.
        /// </summary>
        NullProperty,

        /// <summary>
        /// one of the matrices has zero length.
        /// </summary>
        ZeroLength,

        /// <summary>
        /// The rank is out of bounds.
        /// </summary>
        RankOutOfBounds,

        /// <summary>
        /// invalid mask size.
        /// </summary>
        InvalidMaskSize,

        /// <summary>
        /// invalid mask label length.
        /// </summary>
        InvalidMaskLabelLength
    }

    /// <summary>
    /// MaskedMatrix. Contains a 2d or jagged array of data (e.g. bool[][], double[,]) and
    /// a corresponding jagged array of mask values (integer valued)
    /// The string array is the set of labels corresponding to the mask values. There should be
    /// as many labels as there are unique values in the Mask
    /// </summary>
    public class MaskedMatrix
    {
        /// <summary>
        /// Gets or sets the data. Should be jagged or 2d array
        /// </summary>
        public Array Data { get; set; }

        /// <summary>
        /// Gets or sets the mask.
        /// </summary>
        public int[][] Mask { get; set; }

        /// <summary>
        /// Gets or sets the mask labels.
        /// </summary>
        public string[] MaskLabels { get; set; }

        /// <summary>
        /// Determines whether this instance is valid.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </returns>
        public MaskedMatrixValidity IsValid()
        {
            if (this.Data == null || this.Mask == null || this.MaskLabels == null)
            {
                return MaskedMatrixValidity.NullProperty;
            }

            if (this.Data.Length == 0 || this.Mask.Length == 0 || this.MaskLabels.Length == 0)
            {
                return MaskedMatrixValidity.ZeroLength;
            }

            if (this.Data.Rank != 1 && this.Data.Rank != 2)
            {
                return MaskedMatrixValidity.RankOutOfBounds;
            }

            int[] vals = this.Mask.SelectMany(ia => ia.Distinct()).Distinct().OrderBy(x => x).ToArray();
            
            int cols = (this.Data.Rank == 1)
                            ? (from Array row in this.Data select (row == null ? 0 : row.Length)).Max()
                            : this.Data.GetLength(1);

            if (cols != this.Mask.Length)
            {
                return MaskedMatrixValidity.InvalidMaskSize;
            }

            if (this.MaskLabels.Length != vals.Length)
            {
                return MaskedMatrixValidity.InvalidMaskLabelLength;
            }

            return MaskedMatrixValidity.Valid;
        }
    }
}
