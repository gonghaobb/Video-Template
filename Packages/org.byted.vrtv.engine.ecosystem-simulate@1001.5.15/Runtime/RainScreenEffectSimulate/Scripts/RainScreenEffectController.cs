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
using UnityEngine.Rendering;

namespace Matrix.EcosystemSimulate
{
    public class RainScreenEffectController
    {
        private RainScreenEffectManager.DynamicParam m_Variables;
        private float m_LifeTime = 1;
        private float m_CurrentLifeTimer = 0;
        private Vector3 m_ScreenPos;
        private Vector3 m_CurrentScreenPos;
        private Vector3 m_CurrentRotation = Vector3.zero;
        private Vector3 m_Scale = Vector3.one;
        private Vector3 m_CurrentScale = Vector3.one;
        private float m_CurrentDistortion = 0;
        private float m_CurrentBlur = 0;

        public Vector3 currentScreenPos { get => m_CurrentScreenPos; }
        public Vector3 currentRotation { get => m_CurrentRotation; }
        public Vector3 currentScale { get => m_CurrentScale; }
        public float currentDistortion { get => m_CurrentDistortion; }
        
        public float currentBlur { get => m_CurrentBlur; }

        private float progress
        {
            get
            {
                if (m_LifeTime <= 0)
                {
                    return 0;
                }
                else
                {
                    return Mathf.Clamp01(m_CurrentLifeTimer / m_LifeTime);
                }
            }
        }


        //private float m_IntervalTime = 1;
        public RainScreenEffectController(RainScreenEffectManager.DynamicParam variables)
        {
            m_Variables = variables;
            Reset();
        }

        public void Update()
        {
            if (m_CurrentLifeTimer >  m_LifeTime)
            {
                Reset();
            }
            else
            {
                m_CurrentLifeTimer += Time.deltaTime;
                float curProgress = progress;

                m_CurrentScreenPos = m_ScreenPos - Vector3.up * m_Variables.posYOffset * m_Variables.posYOverLifetime.Evaluate(curProgress);
                m_CurrentScale = m_Scale * m_Variables.sizeOverLifetime.Evaluate(curProgress);
                m_CurrentDistortion = m_Variables.distortionValue * m_Variables.distortionOverLifetime.Evaluate(curProgress);
                m_CurrentBlur = m_Variables.blurValue * m_Variables.blurOverLifetime.Evaluate(curProgress);
            }
        }

        public void Destroy()
        {

        }

        private void Reset()
        {
            m_LifeTime = Random.Range(m_Variables.lifetimeMin, m_Variables.lifetimeMax);
            m_CurrentLifeTimer = 0;

            //屏幕坐标（-1,1）
            float randomX = 0;
            float randomY = 0;
            if (m_Variables.isInvert)
            {
                randomX = Random.Range(-m_Variables.unaffectedRange, m_Variables.unaffectedRange);
                float unaffectedY = Mathf.Sqrt(Mathf.Pow(m_Variables.unaffectedRange, 2f) - Mathf.Pow(randomX, 2f));
                randomY = Random.Range(-unaffectedY, unaffectedY);
            }
            else
            {
                randomX = Random.Range(-1f, 1f);
                if (Mathf.Abs(randomX) >= m_Variables.unaffectedRange)
                {
                    randomY = Random.Range(-1f, 1f);
                }
                else
                {
                    float unaffectedY = Mathf.Sqrt(Mathf.Pow(m_Variables.unaffectedRange, 2f) - Mathf.Pow(randomX, 2f));
                    float ran = Random.Range(-1f, 1f);
                    randomY = unaffectedY * ran / Mathf.Abs(ran) + (1 - unaffectedY) * ran;
                }
            }
            m_ScreenPos = new Vector3(randomX, randomY, 1);
            m_CurrentScreenPos = m_ScreenPos;
            //旋转
            m_CurrentRotation = new Vector3(0, 0, Random.Range(0f, 179.9f));
            //缩放
            m_Scale = new Vector3( Random.Range(m_Variables.sizeMinX, m_Variables.sizeMaxX), Random.Range(m_Variables.sizeMinY, m_Variables.sizeMaxY), 1f );
            m_CurrentScale = m_Scale;

            m_CurrentDistortion = m_Variables.distortionValue * m_Variables.distortionOverLifetime.Evaluate(progress);
            m_CurrentBlur = m_Variables.blurValue * m_Variables.blurOverLifetime.Evaluate(progress);
        }
    }
}
