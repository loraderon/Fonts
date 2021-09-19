// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Encapsulated logic or laying out text.
    /// </summary>
    internal class TextLayout2
    {
        internal static TextLayout2 Default { get; set; } = new TextLayout2();

        /// <summary>
        /// Generates the layout.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>A collection of layout that describe all that's needed to measure or render a series of glyphs.</returns>
        public IReadOnlyList<GlyphLayout> GenerateLayout(ReadOnlySpan<char> text, RendererOptions options)
        {
            if (text.IsEmpty)
            {
                return Array.Empty<GlyphLayout>();
            }

            var dpi = new Vector2(options.DpiX, options.DpiY);
            Vector2 origin = options.Origin / dpi;
            float originX = 0;

            // Handle potential horizontal alignment adjustment based upon wrapping width.
            float maxWidth = float.MaxValue;
            if (options.WrappingWidth > 0)
            {
                // Trim trailing white spaces from the text
                text = text.TrimEnd(null);
                maxWidth = options.WrappingWidth / options.DpiX;

                switch (options.HorizontalAlignment)
                {
                    case HorizontalAlignment.Right:
                        originX = maxWidth;
                        break;
                    case HorizontalAlignment.Center:
                        originX = maxWidth * .5F;
                        break;
                }
            }

            // Check our string again after trimming.
            if (text.IsEmpty)
            {
                return Array.Empty<GlyphLayout>();
            }

            int codePointCount = CodePoint.GetCodePointCount(text);
            AppliedFontStyle spanStyle = options.GetStyle(0, codePointCount);
            spanStyle.ProcessText(text);

            var layout = new List<GlyphLayout>(codePointCount);

            float unscaledLineHeight = 0f;
            float lineHeight = 0f;
            float unscaledLineMaxAscender = 0f;
            float unscaledLineMaxDescender = 0f;
            float lineMaxAscender = 0f;
            float lineMaxDescender = 0f;
            Vector2 location = Vector2.Zero;

            // Remember where the top of the layouted text is for accurate vertical alignment.
            // This is important because there is considerable space between the lineHeight at the glyph's ascender.
            float top = 0;
            float scale = 0;
            bool firstLine = true;
            GlyphMetrics? previousGlyph = null;
            int lastWrappableLocation = -1;
            int nextWrappableLocation = codePointCount;
            bool nextWrappableRequired = false;
            bool shouldWrap = options.WrappingWidth > 0;
            bool breakAll = options.WordBreaking == WordBreaking.BreakAll;
            bool keepAll = options.WordBreaking == WordBreaking.KeepAll;
            bool startOfLine = true;
            float totalHeight = 0;

            // Calculate the initial position of potential line breaks.
            var lineBreakEnumerator = new LineBreakEnumerator(text);
            if (lineBreakEnumerator.MoveNext())
            {
                LineBreak b = lineBreakEnumerator.Current;
                nextWrappableLocation = b.PositionWrap - 1;
                nextWrappableRequired = b.Required;
            }

            int graphemeIndex;
            int codePointIndex = 0;

            // Enumerate through each grapheme in the text.
            var graphemeEnumerator = new SpanGraphemeEnumerator(text);
            for (graphemeIndex = 0; graphemeEnumerator.MoveNext(); graphemeIndex++)
            {
                // Now enumerate through each codepoint in the grapheme.
                int graphemeCodePointIndex = 0;
                var codePointEnumerator = new SpanCodePointEnumerator(graphemeEnumerator.Current);
                while (codePointEnumerator.MoveNext())
                {
                    if (spanStyle.End < codePointIndex)
                    {
                        spanStyle = options.GetStyle(codePointIndex, codePointCount);
                        spanStyle.ProcessText(text);
                        previousGlyph = null;
                    }

                    if (!spanStyle.TryGetGlyphMetrics(codePointIndex, out GlyphMetrics[]? glyphs))
                    {
                        // Codepoint was skipped.
                        codePointIndex++;
                        graphemeCodePointIndex++;
                        continue;
                    }

                    GlyphMetrics? glyph = glyphs[0];
                    CodePoint codePoint = glyph.CodePoint;
                    if (previousGlyph != null && glyph.FontMetrics != previousGlyph.FontMetrics)
                    {
                        scale = glyph.ScaleFactor;
                    }

                    float fontHeight = glyph.FontMetrics.LineHeight * options.LineSpacing;
                    if (fontHeight > unscaledLineHeight)
                    {
                        // Get the largest line height thus far
                        unscaledLineHeight = fontHeight;
                        scale = glyph.ScaleFactor;
                        lineHeight = unscaledLineHeight * spanStyle.PointSize / scale;
                    }

                    if (glyph.FontMetrics.Ascender > unscaledLineMaxAscender)
                    {
                        unscaledLineMaxAscender = glyph.FontMetrics.Ascender;
                        scale = glyph.ScaleFactor;
                        lineMaxAscender = unscaledLineMaxAscender * spanStyle.PointSize / scale;
                    }

                    if (Math.Abs(glyph.FontMetrics.Descender) > unscaledLineMaxDescender)
                    {
                        unscaledLineMaxDescender = Math.Abs(glyph.FontMetrics.Descender);
                        scale = glyph.ScaleFactor;
                        lineMaxDescender = unscaledLineMaxDescender * spanStyle.PointSize / scale;
                    }

                    if (firstLine)
                    {
                        // Set the position for the first line.
                        switch (options.VerticalAlignment)
                        {
                            case VerticalAlignment.Top:
                                top = lineMaxAscender;
                                break;
                            case VerticalAlignment.Center:
                                top = (lineMaxAscender * .5F) - (lineMaxDescender * .5F);
                                break;
                            case VerticalAlignment.Bottom:
                                top = -lineMaxDescender;
                                break;
                        }
                    }

                    // Keep a record of where to wrap text and ensure that no line starts with white space
                    if ((shouldWrap && (breakAll || nextWrappableLocation == codePointIndex))
                        || nextWrappableRequired)
                    {
                        if (!(keepAll && UnicodeUtility.IsCJKCodePoint((uint)codePoint.Value)))
                        {
                            // We don't want to ever break between codepoints within a grapheme.
                            if (graphemeCodePointIndex == 0)
                            {
                                for (int j = layout.Count - 1; j >= 0; j--)
                                {
                                    GlyphLayout item = layout[j];
                                    if (!item.IsWhiteSpace())
                                    {
                                        lastWrappableLocation = j + 1;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // Find the next line break.
                    if (nextWrappableLocation == codePointIndex && lineBreakEnumerator.MoveNext())
                    {
                        LineBreak b = lineBreakEnumerator.Current;
                        nextWrappableLocation = b.PositionWrap - 1;
                        nextWrappableRequired = b.Required;
                    }

                    float glyphWidth = glyph.AdvanceWidth * spanStyle.PointSize / scale;
                    float glyphHeight = glyph.AdvanceHeight * spanStyle.PointSize / scale;

                    if (glyphWidth > 0 && !CodePoint.IsNewLine(codePoint) && !CodePoint.IsWhiteSpace(codePoint))
                    {
                        foreach (GlyphMetrics? g in glyphs)
                        {
                            float w = g.AdvanceWidth * spanStyle.PointSize / scale;
                            float h = g.AdvanceHeight * spanStyle.PointSize / scale;
                            layout.Add(new GlyphLayout(
                                graphemeIndex,
                                codePoint,
                                new Glyph(g, spanStyle.PointSize),
                                location,
                                w,
                                h,
                                lineHeight,
                                startOfLine));

                            if (w > glyphWidth)
                            {
                                glyphWidth = w;
                            }
                        }

                        startOfLine = false;

                        // Move forward the actual width of the glyph, we are retaining the baseline
                        location.X += glyphWidth;

                        // If the word extended pass the end of the box, wrap it.
                        // We don't want to ever break between codepoints within a grapheme.
                        if (graphemeCodePointIndex == 0
                            && location.X >= maxWidth
                            && lastWrappableLocation > 0
                            && lastWrappableLocation < layout.Count)
                        {
                            float wrappingOffset = layout[lastWrappableLocation].Location.X;
                            startOfLine = true;

                            // Move the characters to the next line
                            for (int j = lastWrappableLocation; j < layout.Count; j++)
                            {
                                if (layout[j].IsWhiteSpace())
                                {
                                    wrappingOffset += layout[j].Width;
                                    layout.RemoveAt(j);
                                    j--;
                                    continue;
                                }

                                GlyphLayout current = layout[j];
                                var wrapped = GlyphLayout.Offset(current, new Vector2(-wrappingOffset, lineHeight), startOfLine);
                                startOfLine = false;
                                location.X = wrapped.Location.X + wrapped.Width;
                                layout[j] = wrapped;
                            }

                            location.Y += lineHeight;
                            totalHeight += lineHeight;
                            firstLine = false;
                            lastWrappableLocation = -1;
                        }

                        previousGlyph = glyph;
                    }
                    else if (codePoint.Value == '\r')
                    {
                        // Carriage Return resets the XX coordinate to 0
                        location.X = 0;
                        previousGlyph = null;
                        startOfLine = true;

                        layout.Add(new GlyphLayout(
                            graphemeIndex,
                            codePoint,
                            new Glyph(glyph, spanStyle.PointSize),
                            location,
                            0,
                            glyphHeight,
                            lineHeight,
                            startOfLine));

                        startOfLine = false;
                    }
                    else if (CodePoint.IsNewLine(codePoint))
                    {
                        // New Line resets the XX coordinate to 0 and offsets vertically to a new line.
                        layout.Add(new GlyphLayout(
                            graphemeIndex,
                            codePoint,
                            new Glyph(glyph, spanStyle.PointSize),
                            location,
                            0,
                            glyphHeight,
                            lineHeight,
                            startOfLine));

                        location.X = 0;
                        location.Y += lineHeight;
                        totalHeight += lineHeight;
                        unscaledLineHeight = 0;
                        unscaledLineMaxAscender = 0;
                        previousGlyph = null;
                        firstLine = false;
                        lastWrappableLocation = -1;
                        startOfLine = true;
                    }
                    else if (codePoint.Value == '\t')
                    {
                        float tabStop = glyphWidth * spanStyle.TabWidth;
                        float finalWidth = 0;

                        if (tabStop > 0)
                        {
                            finalWidth = tabStop - (location.X % tabStop);
                        }

                        if (finalWidth < glyphWidth)
                        {
                            // If we are not going to tab at least a glyph width add another tabstop
                            // to it ??? TODO: Should I be doing this?
                            finalWidth += tabStop;
                        }

                        layout.Add(new GlyphLayout(
                            graphemeIndex,
                            codePoint,
                            new Glyph(glyph, spanStyle.PointSize),
                            location,
                            finalWidth,
                            glyphHeight,
                            lineHeight,
                            startOfLine));

                        startOfLine = false;

                        // Advance to a position > width away that
                        location.X += finalWidth;
                        previousGlyph = null;
                    }
                    else if (CodePoint.IsWhiteSpace(codePoint))
                    {
                        layout.Add(new GlyphLayout(
                            graphemeIndex,
                            codePoint,
                            new Glyph(glyph, spanStyle.PointSize),
                            location,
                            glyphWidth,
                            glyphHeight,
                            lineHeight,
                            startOfLine));

                        startOfLine = false;
                        location.X += glyphWidth;
                        previousGlyph = null;
                    }

                    codePointIndex++;
                    graphemeCodePointIndex++;
                }
            }

            var offsetY = new Vector2(0, top);
            switch (options.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    offsetY += new Vector2(0, -(totalHeight * .5F));
                    break;
                case VerticalAlignment.Bottom:
                    offsetY += new Vector2(0, -totalHeight);
                    break;
            }

            Vector2 offsetX = Vector2.Zero;
            for (int i = 0; i < layout.Count; i++)
            {
                GlyphLayout glyphLayout = layout[i];
                graphemeIndex = glyphLayout.GraphemeIndex;

                // Scan ahead getting the width.
                if (glyphLayout.StartOfLine)
                {
                    float width = 0;
                    for (int j = i; j < layout.Count; j++)
                    {
                        GlyphLayout current = layout[j];
                        int currentGraphemeIndex = current.GraphemeIndex;
                        if (current.StartOfLine && (currentGraphemeIndex != graphemeIndex))
                        {
                            // Leading graphemes can be made up of multiple glyphs all marked as 'StartOfLine so we only
                            // break when we are sure we have entered a new cluster or previously defined break.
                            break;
                        }

                        width = Math.Max(width, current.Location.X + current.Width);
                    }

                    // Calculate an offset from the 'origin' based on TextAlignment for each line
                    switch (options.HorizontalAlignment)
                    {
                        case HorizontalAlignment.Left:
                            offsetX = new Vector2(originX, 0) + offsetY;
                            break;
                        case HorizontalAlignment.Right:
                            offsetX = new Vector2(originX - width, 0) + offsetY;
                            break;
                        case HorizontalAlignment.Center:
                            offsetX = new Vector2(originX - (width * .5F), 0) + offsetY;
                            break;
                    }
                }

                layout[i] = GlyphLayout.Offset(glyphLayout, offsetX + origin, glyphLayout.StartOfLine);
            }

            return layout;
        }
    }
}