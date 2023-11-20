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

using UnityEngine;
using UnityEditor;

namespace Matrix.EcosystemSimulate
{
    public static class CoreEditorStyles
    {
        public static readonly GUIStyle miniLabelButton;

        private static readonly Texture2D paneOptionsIconDark;
        private static readonly Texture2D paneOptionsIconLight;

        public static Texture2D paneOptionsIcon
        {
            get { return EditorGUIUtility.isProSkin ? paneOptionsIconDark : paneOptionsIconLight; }
        }

        static CoreEditorStyles()
        {
            Texture2D transparentTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            transparentTexture.SetPixel(0, 0, Color.clear);
            transparentTexture.Apply();

            miniLabelButton = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = new GUIStyleState
                {
                    background = transparentTexture, scaledBackgrounds = null, textColor = Color.grey
                }
            };
            var activeState = new GUIStyleState
            {
                background = transparentTexture,
                scaledBackgrounds = null,
                textColor = Color.white
            };
            miniLabelButton.active = activeState;
            miniLabelButton.onNormal = activeState;
            miniLabelButton.onActive = activeState;

            paneOptionsIconDark = (Texture2D) EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/pane options.png");
            paneOptionsIconLight = (Texture2D) EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/pane options.png");
        }
    }
}