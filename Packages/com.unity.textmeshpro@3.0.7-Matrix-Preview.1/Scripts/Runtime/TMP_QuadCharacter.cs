using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;

namespace TMPro
{
    /// <summary>
    /// A basic element of text representing a pictograph, image, sprite or emoji.
    /// </summary>
    [Serializable]
    public class TMP_QuadCharacter : TMP_TextElement
    {
        public TMP_QuadItem quadItem;

        /// <summary>
        /// The name of the sprite element.
        /// </summary>
        public string name
        {
            get { return m_Name; }
            set
            {
                if (value == m_Name)
                    return;

                m_Name = value;
                m_HashCode = TMP_TextParsingUtilities.GetHashCode(m_Name);
            }
        }

        /// <summary>
        /// The hashcode value which is computed from the name of the sprite element.
        /// This value is read-only and updated when the name of the text sprite is changed.
        /// </summary>
        public int hashCode { get { return m_HashCode; } }


        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        private string m_Name;

        [SerializeField]
        private int m_HashCode;


        /// <summary>
        /// Attributes
        /// </summary>
        public Dictionary<int, string> attrs { get; set; }

        public float xscale = 0;
        public float yscale = 0;

        /// <summary>
        /// The glyph.scale
        /// </summary>
        public float gscale { get { return GetFloatAttrValue(gscaleHash, 1); } }
        public int width { get { return GetIntAttrValue(wHash); } }
        public int height { get { return GetIntAttrValue(hHash); } }
        public int yoff { get { return GetIntAttrValue(yoffHash); } }

        private static int s_xscaleHash = 0;
        private static int s_yscaleHash = 0;

        private static int s_gscaleHash = 0;
        private static int s_wHash = 0;
        private static int s_hHash = 0;
        private static int s_yoffHash = 0;

        public static int xscaleHash
        {
            get
            {
                if (s_xscaleHash != 0) return s_xscaleHash;
                s_xscaleHash = TMP_TextParsingUtilities.GetHashCode("xscale");
                return s_xscaleHash;
            }
        }

        public static int yscaleHash
        {
            get
            {
                if (s_yscaleHash != 0) return s_yscaleHash;
                s_yscaleHash = TMP_TextParsingUtilities.GetHashCode("yscale");
                return s_yscaleHash;
            }
        }

        public static int gscaleHash
        {
            get
            {
                if (s_gscaleHash != 0) return s_gscaleHash;
                s_gscaleHash = TMP_TextParsingUtilities.GetHashCode("gscale");
                return s_gscaleHash;
            }
        }
        public static int wHash {
            get
            {
                if (s_wHash != 0) return s_wHash;
                s_wHash = TMP_TextParsingUtilities.GetHashCode("w");
                return s_wHash;
            }
        }
        public static int hHash {
            get
            {
                if (s_hHash != 0) return s_hHash;
                s_hHash = TMP_TextParsingUtilities.GetHashCode("h");
                return s_hHash;
            }
        }
        public static int yoffHash
        {
            get
            {
                if (s_yoffHash != 0) return s_yoffHash;
                s_yoffHash = TMP_TextParsingUtilities.GetHashCode("yoff");
                return s_yoffHash;
            }
        }

        // ********************
        // CONSTRUCTORS
        // ********************

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TMP_QuadCharacter()
        {
            m_ElementType = TextElementType.Quad;
        }

        /// <summary>
        /// Constructor for new sprite character.
        /// </summary>
        /// <param name="unicode">Unicode value of the sprite character.</param>
        /// <param name="glyph">Glyph used by the sprite character.</param>
        public TMP_QuadCharacter(uint unicode)
        {
            m_ElementType = TextElementType.Quad;

            this.unicode = unicode;
            //this.glyphIndex = glyph.index;
            //this.glyph = glyph;
            this.scale = 1.0f;
        }

        public void SetQuadGlyph(TMP_QuadGlyph glyph_)
        {
            if (glyph_ != null)
            {
                this.glyphIndex = glyph_.index;
                this.glyph = glyph_;
            }
        }

        public void UpdateAttributes(char[] htmlTag, RichTextTagAttribute[] attrsList)
        {
            if (attrs == null)
            { // assign attrs
                attrs = new Dictionary<int, string>();
            }

            attrs.Clear();
            for (int i = 1; i < attrsList.Length; ++i)
            {
                var attrInfo = attrsList[i];
                if (attrInfo.nameHashCode != 0)
                {
                    var attrValue = new string(htmlTag, attrsList[i].valueStartIndex, attrsList[i].valueLength);
                    if (!attrs.ContainsKey(attrInfo.nameHashCode))
                    {
                        attrs.Add(attrInfo.nameHashCode, attrValue);
                    }
                    else
                    {
                        attrs[attrInfo.nameHashCode] = attrValue;
                    }
                }
                else
                {
                    break;
                }
            }

            xscale = GetFloatAttrValue(xscaleHash);
            yscale = GetFloatAttrValue(yscaleHash);
        }

        private int GetIntAttrValue(int hash, int defaultValue = 0)
        {
            string strValue;
            attrs.TryGetValue(hash, out strValue);
            if (!string.IsNullOrEmpty(strValue))
            {
                try
                {
                    return Convert.ToInt32(strValue);
                }
                catch (FormatException)
                { // ignore format exception, return defaultValue
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        private float GetFloatAttrValue(int hash, float defaultValue = 0)
        {
            string strValue;
            attrs.TryGetValue(hash, out strValue);
            if (!string.IsNullOrEmpty(strValue))
            {
                try
                {
                    return Convert.ToSingle(strValue);
                }
                catch (FormatException)
                { // ignore format exception, return defaultValue
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}
