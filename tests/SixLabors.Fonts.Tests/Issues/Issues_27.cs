﻿using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_27
    {
        [Fact]
        public void ThrowsMeasureingWhitespace()
        {
            // wendy one returns wrong points for 'o'
            Font font = new FontCollection().Install(TestFonts.WendyOneFile);

            GlyphRenderer r = new GlyphRenderer();

            var size = new TextMeasurer().MeasureText("          ", new FontSpan(new Font(font, 30), 72));

            Assert.Equal(60, size.Width, 1);
            Assert.Equal(37.1, size.Height, 1);
        }
    }
}