using System;
using UnityEngine;
using UnityEngine.TextCore;

namespace TMPro
{
    /// <summary>
    /// The visual representation of the sprite character using this glyph.
    /// </summary>
    [Serializable]
    public class TMP_QuadGlyph : Glyph
    {
        // ********************
        // CONSTRUCTORS
        // ********************

        public float xscale = 0;
        public float yscale = 0;

        public TMP_QuadGlyph() { }

        /// <summary>
        /// Constructor for new sprite glyph.
        /// </summary>
        /// <param name="index">Index of the sprite glyph.</param>
        /// <param name="metrics">Metrics which define the position of the glyph in the context of text layout.</param>
        /// <param name="glyphRect">GlyphRect which defines the coordinates of the glyph in the atlas texture.</param>
        /// <param name="scale">Scale of the glyph.</param>  
        public TMP_QuadGlyph(uint index, GlyphMetrics metrics, GlyphRect glyphRect, float scale)
        {
            this.index = index;
            this.metrics = metrics;
            this.glyphRect = glyphRect;
            this.scale = scale;
        }
    }
}