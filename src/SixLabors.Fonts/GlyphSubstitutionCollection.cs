// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a collection of glyph indices that are mapped to input codepoints.
    /// </summary>
    public sealed class GlyphSubstitutionCollection
    {
        /// <summary>
        /// Contains a map between the index of a map within the collection and its offset.
        /// </summary>
        private readonly List<int> offsets = new();

        /// <summary>
        /// Contains a map between non-sequential codepoint offsets and their glyph ids.
        /// </summary>
        private readonly Dictionary<int, CodePointGlyphs> map = new();

        /// <summary>
        /// Contains hashset of substitution features to apply for each glyph.
        /// </summary>
        private readonly Dictionary<int, HashSet<Tag>> substitutionFeatureTags = new();

        /// <summary>
        /// Gets the number of glyphs ids contained in the collection.
        /// This may be more or less than original input codepoint count (due to substitution process).
        /// </summary>
        public int Count => this.offsets.Count;

        /// <summary>
        /// Gets the glyph ids at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The <see cref="ReadOnlySpan{UInt16}"/>.</returns>
        public ReadOnlySpan<int> this[int index] => this.map[this.offsets[index]].GlyphIds;

        /// <summary>
        /// Gets the substitution features for the given glyph index.
        /// </summary>
        /// <param name="index">The glyph index.</param>
        /// <returns>The substitution features to use.</returns>
        internal HashSet<Tag> GetSubstitutionFeatures(int index) => this.substitutionFeatureTags[index];

        /// <summary>
        /// Adds the glyph id and the codepoint it represents to the collection.
        /// </summary>
        /// <param name="glyphId">The id of the glyph to add.</param>
        /// <param name="codePoint">The codepoint the glyph represents.</param>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        public void AddGlyph(int glyphId, CodePoint codePoint, int offset)
        {
            this.map.Add(offset, new CodePointGlyphs(codePoint, new[] { glyphId }));
            this.offsets.Add(offset);
            this.substitutionFeatureTags[offset] = new HashSet<Tag>();
        }

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        public void Clear()
        {
            this.offsets.Clear();
            this.map.Clear();
            this.substitutionFeatureTags.Clear();
        }

        /// <summary>
        /// Gets the specified glyph ids matching the given codepoint offset.
        /// </summary>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        /// <param name="codePoint">
        /// When this method returns, contains the codepoint associated with the specified offset,
        /// if the value is found; otherwise, the default value for the type of the codepoint parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <param name="glyphIds">
        /// When this method returns, contains the glyph ids associated with the specified offset,
        /// if the value is found; otherwise, the default value for the type of the glyphIds parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="GlyphSubstitutionCollection"/> contains glyph ids
        /// for the specified offset; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetCodePointAndGlyphIdsAtOffset(int offset, [NotNullWhen(true)] out CodePoint? codePoint, [NotNullWhen(true)] out IEnumerable<int>? glyphIds)
        {
            if (this.map.TryGetValue(offset, out CodePointGlyphs value))
            {
                codePoint = value.CodePoint;
                glyphIds = value.GlyphIds;
                return true;
            }

            codePoint = null;
            glyphIds = null;
            return false;
        }

        /// <summary>
        /// Gets the glyph ids and the Unicode script for those ids at the specified position.
        /// </summary>
        /// <param name="index">The zero-based index of the elements to get.</param>
        /// <param name="codePoint">The Unicode codepoint.</param>
        /// <param name="offset">The zero-based index within the input codepoint collection.</param>
        /// <param name="glyphIds">The glyph ids.</param>
        public void GetCodePointAndGlyphIds(int index, out CodePoint codePoint, out int offset, out IEnumerable<int> glyphIds)
        {
            offset = this.offsets[index];
            CodePointGlyphs value = this.map[offset];
            codePoint = value.CodePoint;
            glyphIds = value.GlyphIds;
        }

        /// <summary>
        /// Performs a 1:1 replacement of a glyph id at the given position.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="glyphId">The replacement glyph id.</param>
        public void Replace(int index, int glyphId)
        {
            int offset = this.offsets[index];
            this.map[offset] = new CodePointGlyphs(this.map[offset].CodePoint, new[] { glyphId });
        }

        /// <summary>
        /// Replaces a series of glyph ids starting at the given position with a new id.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to replace.</param>
        /// <param name="count">The number of elements to replace.</param>
        /// <param name="glyphId">The replacement glyph id.</param>
        public void Replace(int index, int count, int glyphId)
        {
            // Remove the count starting at the at index.
            int offset = this.offsets[index];
            CodePoint codePoint = this.map[offset].CodePoint;
            for (int i = 0; i < count; i++)
            {
                this.map.Remove(this.offsets[i + index]);
            }

            this.offsets.RemoveRange(index, count);

            // Assign our new id at the index.
            this.map[offset] = new CodePointGlyphs(codePoint, new[] { glyphId });
            this.offsets.Insert(index, offset);
        }

        /// <summary>
        /// Replaces a single glyph id with a collection of glyph ids.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="glyphIds">The collection of replacement glyph ids.</param>
        public void Replace(int index, IEnumerable<int> glyphIds)
        {
            int offset = this.offsets[index];
            this.map[offset] = new CodePointGlyphs(this.map[offset].CodePoint, glyphIds.ToArray());
        }

        /// <summary>
        /// Adds the substitution feature to the features which should be applied to the glyph at a given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="feature">The feature to apply.</param>
        internal void AddSubstitutionFeature(int index, Tag feature) => this.substitutionFeatureTags[index].Add(feature);

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        private readonly struct CodePointGlyphs
        {
            public CodePointGlyphs(CodePoint codePoint, int[] glyphIds)
            {
                this.CodePoint = codePoint;
                this.GlyphIds = glyphIds;
            }

            public CodePoint CodePoint { get; }

            public int[] GlyphIds { get; }

            private string DebuggerDisplay
                => FormattableString
                .Invariant($"{this.CodePoint.ToDebuggerDisplay()} : {CodePoint.GetScript(this.CodePoint)} : [{string.Join(",", this.GlyphIds)}]");
        }
    }
}