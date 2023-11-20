//
//      __  __       _______ _____  _______   __
//      |  \/  |   /\|__   __|  __ \|_   _\ \ / /
//      | \  / |  /  \  | |  | |__) | | |  \ V / 
//      | |\/| | / /\ \ | |  |  _  /  | |   > <  
//      | |  | |/ ____ \| |  | | \ \ _| |_ / . \ 
//      |_|  |_/_/    \_\_|  |_|  \_\_____/_/ \_\                        
//									   (ByteDance)
//
//      Created by Matrix team.
//      Procedural LOGO:https://www.shadertoy.com/view/ftKBRW
//
//      The team was set up on September 4, 2019.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LWGUI;
using UnityEditor;
using UnityEngine;

namespace Matrix.ShaderGUI
{
    public enum SurfaceType
    {
        Opaque = 0,

        Transparent = 1,
    }

    //用于替换UnityEngine.Rendering.CullMode
    public enum RenderFace
    {
        Front = 2,//CullMode.Back

        Back = 1,//CullMode.Front

        Both = 0,//CullMode.Off
    }

    public enum SmoothnessMapChannel
    {
        MetallicAlpha,

        AlbedoAlpha,
    }

    public enum BlendMode
    {
        Alpha,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
        Premultiply, // Physically plausible transparency mode, implemented as alpha pre-multiply
        Additive,
        Multiply
    }

    public enum LightingMode
    {
        PBR,
        BlingPhong,
        None,
    }

    public enum ColorWriteMask
    {
        None = 0x00,
        Alpha = 0x1,
        Blue = 0x2,
        Green = 0x4,
        Red = 0x8,
        All = 0xF
    }

    internal class ExtendEnumDrawer : SubDrawer
    {
        private string[] m_Names;
        private int[] m_Values;
        private string m_Indent = "false";
        private float m_SelectedExtraValue = -1.0f;
        private string m_ExtraPropName;

        public ExtendEnumDrawer(string group, string enumTypeName) : this(group, enumTypeName, "false", -1, "") { }

        public ExtendEnumDrawer(string group, string enumTypeName, string indent) : this(group, enumTypeName, indent, -1, "") { }

        public ExtendEnumDrawer(string group, string enumTypeName, float selectedExtraValue, string extraPropName) : this(group, enumTypeName, "false", selectedExtraValue, extraPropName) { }

        public ExtendEnumDrawer(string group, string enumTypeName, string indent, float selectedExtraValue, string extraPropName)
        {
            this.group = group;

            var array = ReflectionHelper.GetAllTypes();
            Type enumType = ((IEnumerable<System.Type>)array).FirstOrDefault<System.Type>((Func<System.Type, bool>)(x => x.IsSubclassOf(typeof(Enum)) && (x.Name == enumTypeName || x.FullName == enumTypeName)));

            if (enumType == null)
            {
                Debug.LogErrorFormat("enumTypeName:{0} is wrong", enumTypeName);
                return;
            }

            m_Names = enumType.GetEnumNames();
            m_Values = (int[])enumType.GetEnumValues();
            m_Indent = indent;
            m_SelectedExtraValue = selectedExtraValue;
            m_ExtraPropName = extraPropName;
        }

        protected override bool IsMatchPropType(MaterialProperty property) { return property.type == MaterialProperty.PropType.Float; }

        public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            int index = (int)prop.floatValue;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            if (m_Indent == "true")
            {
                EditorGUI.indentLevel++;
            }
            int num = EditorGUI.IntPopup(position, label.text, index, this.m_Names, this.m_Values);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = (float)num;
            }

            if (num == (int)m_SelectedExtraValue)
            {
                MaterialProperty extraProp = null;
                if (m_ExtraPropName != "" && m_ExtraPropName != "_")
                {
                    extraProp = LWGUI.LWGUI.FindProp(m_ExtraPropName, props, true);
                }

                if (extraProp != null)
                {
                    EditorGUI.indentLevel++;
                    editor.ShaderProperty(EditorGUILayout.GetControlRect(), extraProp, extraProp.displayName);
                    EditorGUI.indentLevel--;
                }
            }

            if (m_Indent == "true")
            {
                EditorGUI.indentLevel--;
            }
        }
    }

    internal class ExtendToggleDrawer : SubDrawer
    {
        string _keyWord;
        string _extraPropName;

        public ExtendToggleDrawer(string group, string keyWord, string extraPropName)
        {
            this.group = group;
            this._keyWord = keyWord;
            this._extraPropName = extraPropName;
        }

        protected override bool IsMatchPropType(MaterialProperty property) { return property.type == MaterialProperty.PropType.Float; }

        public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            EditorGUI.showMixedValue = prop.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var rect = position;//EditorGUILayout.GetControlRect();
            var value = EditorGUI.Toggle(rect, label, prop.floatValue > 0.0f);
            string k = Helper.GetKeyWord(_keyWord, prop.name);
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = value ? 1.0f : 0.0f;
                Helper.SetShaderKeyWord(editor.targets, k, value);
            }

            GroupStateHelper.SetKeywordConditionalDisplay(editor.target, k, value);

            //checked
            if (value)
            {
                MaterialProperty extraProp = null;
                if (_extraPropName != "" && _extraPropName != "_")
                {
                    extraProp = LWGUI.LWGUI.FindProp(_extraPropName, props, true);
                }

                if (extraProp != null)
                {
                    EditorGUI.indentLevel++;
                    editor.ShaderProperty(EditorGUILayout.GetControlRect(), extraProp, extraProp.displayName);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.showMixedValue = false;
        }
    }

    internal class ExtendTexDrawer : LWGUI.TexDrawer
    {
        string _keyWord = "";
        string _scaleOffset = "false";

        public ExtendTexDrawer() { }

        public ExtendTexDrawer(string group) : base(group, String.Empty) { }

        public ExtendTexDrawer(string group, string extraPropName) : base(group, extraPropName)
        {
            _keyWord = "_";
            _scaleOffset = "false";
        }

        public ExtendTexDrawer(string group, string extraPropName, string keyWord) : base(group, extraPropName) 
        {
            _keyWord = keyWord;
            _scaleOffset = "false";
        }

        public ExtendTexDrawer(string group, string extraPropName, string keyWord, string scaleOffset) : base(group, extraPropName)
        {
            _keyWord = keyWord;
            _scaleOffset = scaleOffset;
        }

        public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            base.DrawProp(position, prop, label, editor);

            if (_scaleOffset == "true")
            {
                editor.TextureScaleOffsetProperty(prop);
            }

            if (_keyWord != "" && _keyWord != "_")
            {
                string k = Helper.GetKeyWord(_keyWord, prop.name);
                Helper.SetShaderKeyWord(editor.targets, k, prop.textureValue != null);
            }
        }
    }

    internal class ExtendSubDrawer : LWGUI.SubDrawer
    {
        string _keyWord;

        public ExtendSubDrawer() { }

        public ExtendSubDrawer(string group) : base(group) { }

        public ExtendSubDrawer(string group, string keyWord) : base(group)
        {
            _keyWord = keyWord;
        }

        public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            base.DrawProp(position, prop, label, editor);

            if (_keyWord != "" && _keyWord != "_")
            {
                string k = Helper.GetKeyWord(_keyWord, prop.name);
                Helper.SetShaderKeyWord(editor.targets, k, prop.textureValue != null);
            }
        }
    }
}