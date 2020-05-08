﻿// Copyright (c) Six Labors and contributors.
// Licensed under the GNU Affero General Public License, Version 3.

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal abstract class GlyphLoader
    {
        public abstract GlyphVector CreateGlyph(GlyphTable table);

        public static GlyphLoader Load(BinaryReader reader)
        {
            short contoursCount = reader.ReadInt16();
            var bounds = Bounds.Load(reader);

            if (contoursCount >= 0)
            {
                return SimpleGlyphLoader.LoadSimpleGlyph(reader, contoursCount, bounds);
            }
            else
            {
                return CompositeGlyphLoader.LoadCompositeGlyph(reader, bounds);
            }
        }
    }
}
