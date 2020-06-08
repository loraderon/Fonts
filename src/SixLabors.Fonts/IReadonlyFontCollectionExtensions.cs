// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A readonly collection of fonts.
    /// </summary>
    public static class IReadonlyFontCollectionExtensions
    {
        /// <summary>
        /// Create a new instance of the <see cref="Font"/> for the named font family.
        /// </summary>
        /// <param name="collection">The the ont collection to retrieve the font family from.</param>
        /// <param name="fontFamily">The family.</param>
        /// <param name="size">The size.</param>
        /// <param name="style">The style.</param>
        /// <returns>The font for the representing the configured options.</returns>
        public static Font CreateFont(this IReadOnlyFontCollection collection, string fontFamily, float size, FontStyle style)
        {
            return new Font(collection.Find(fontFamily), size, style);
        }

        /// <summary>
        /// Create a new instance of the <see cref="Font"/> for the named font family with regular styling.
        /// </summary>
        /// <param name="collection">The the ont collection to retrieve the font family from.</param>
        /// <param name="fontFamily">The family.</param>
        /// <param name="size">The size.</param>
        /// <returns>The font for the representing the configured options.</returns>
        public static Font CreateFont(this IReadOnlyFontCollection collection, string fontFamily, float size)
        {
            return new Font(collection.Find(fontFamily), size);
        }

#if SUPPORTS_CULTUREINFO_LCID
        /// <summary>
        /// Create a new instance of the <see cref="Font"/> for the named font family.
        /// </summary>
        /// <param name="collection">The the ont collection to retrieve the font family from.</param>
        /// <param name="fontFamily">The family.</param>
        /// <param name="culture">The culture of the retuend font family</param>
        /// <param name="size">The size.</param>
        /// <param name="style">The style.</param>
        /// <returns>The font for the representing the configured options.</returns>
        public static Font CreateFont(this IReadOnlyFontCollection collection, string fontFamily, CultureInfo culture, float size, FontStyle style)
        {
            return new Font(collection.Find(fontFamily, culture), size, style);
        }

        /// <summary>
        /// Create a new instance of the <see cref="Font"/> for the named font family with regular styling.
        /// </summary>
        /// <param name="collection">The the ont collection to retrieve the font family from.</param>
        /// <param name="fontFamily">The family.</param>
        /// <param name="culture">The culture of the retuend font family</param>
        /// <param name="size">The size.</param>
        /// <returns>The font for the representing the configured options.</returns>
        public static Font CreateFont(this IReadOnlyFontCollection collection, string fontFamily, CultureInfo culture, float size)
        {
            return new Font(collection.Find(fontFamily, culture), size);
        }
#endif
    }
}
