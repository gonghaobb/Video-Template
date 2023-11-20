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

using UnityEditor;
using UnityEngine;

namespace Matrix.EcosystemSimulate
{
    [CustomPropertyDrawer(typeof(TransitionParameters))]
    public class EcosystemTransitionEditor : PropertyDrawer
    {
        private SerializedProperty m_StartingTimeProperty;
        private SerializedProperty m_StartingDelayTimeProperty;
        private SerializedProperty m_EndingTimeProperty;
        private SerializedProperty m_EndingDelayTimeProperty;
        
        private bool m_InitFinish = false;
        private float m_PropertyHeight = 0;
        private Rect m_CurrentLineRect;
        private Rect m_StartLineRect;
        
        private static readonly float SINGLE_LINE_HEIGHT = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        private void Init(SerializedProperty property)
        {
            m_StartingTimeProperty = property.FindPropertyRelative("m_StartingTime");
            m_StartingDelayTimeProperty = property.FindPropertyRelative("m_StartingDelayTime");
            m_EndingTimeProperty = property.FindPropertyRelative("m_EndingTime");
            m_EndingDelayTimeProperty = property.FindPropertyRelative("m_EndingDelayTime");
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return m_PropertyHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!m_InitFinish)
            {
                Init(property);
                m_InitFinish = true;
            }

            m_PropertyHeight = 0;
            m_StartLineRect = position;
            DrawHeaderField("渐变配置");
            DrawPropertyField(m_StartingTimeProperty , m_StartingDelayTimeProperty);
            DrawPropertyField(m_EndingTimeProperty , m_EndingDelayTimeProperty);
            DrawHeaderField("基础配置");
        }

        private void DrawHeaderField(string header)
        {
            float height = SINGLE_LINE_HEIGHT;
            m_CurrentLineRect = m_StartLineRect;
            m_CurrentLineRect.y = m_StartLineRect.y + m_PropertyHeight + 5;
            m_CurrentLineRect.height = height;
            EditorGUI.LabelField(m_CurrentLineRect , header , EditorStyles.boldLabel);
            m_PropertyHeight += height + 5;
        }
        
        private void DrawPropertyField(SerializedProperty propertyLeft , SerializedProperty propertyRight)
        {
            propertyLeft.serializedObject.Update();
            propertyRight.serializedObject.Update();
            float height = EditorGUI.GetPropertyHeight(propertyLeft);
            float width = m_StartLineRect.width / 2.0f;
            m_CurrentLineRect = m_StartLineRect;
            m_CurrentLineRect.y = m_StartLineRect.y + m_PropertyHeight + 5;
            m_CurrentLineRect.height = height;
            m_CurrentLineRect.width = width;
            EditorGUI.PropertyField(m_CurrentLineRect, propertyLeft);
            m_CurrentLineRect.x += width;
            EditorGUI.PropertyField(m_CurrentLineRect, propertyRight);
            m_PropertyHeight += height + 5;
            propertyLeft.serializedObject.ApplyModifiedProperties();
            propertyRight.serializedObject.ApplyModifiedProperties();
        }
    }
}