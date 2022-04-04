Shader "AlonerShader/SonicShader3"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        [HideInInspector]_GrabPassTransparent ("Grab Pass Transparent", 2D) = "white" {}
        _GrabPassTransparentRT ("Grab Pass Transparent RenderTexture", 2D) = "white" {}
        _DisStr ("Distortion Strength", Range(0, 1)) = 0.5
        _WaveSpd ("Wave Speed", Range(0, 2)) = 0.5
        _Freq ("Frequency", Range(0.001, 1)) = 0.3
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _BlendColor("Blend Color", Color) = (1, 1, 1, 1)
        _BlendOpacity ("Blend Opacity", Range(0, 1)) = 0.3
    }


    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Sprite"
            "AlphaDepth" = "False"
            "CanUseSpriteAtlas" = "True"
            "IgnoreProjector" = "True"
        }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_GrabPassTransparent);
        SAMPLER(sampler_GrabPassTransparent);
        TEXTURE2D(_GrabPassTransparentRT);
        SAMPLER(sampler_GrabPassTransparentRT);
        float _DisStr;
        float _WaveSpd;
        float _Freq;
        float4 _BlendColor;
        float _BlendOpacity;
        // FLOAT(_DisStr);
        // FLOAT(_WaveSpd);
        // FLOAT(_Freq);
        // float4(_BlendColor);
        // FLOAT(_BlendOpacity);

        struct VertexInput
        {
            float4 position : POSITION;
            float2 uv : TEXCOORD0;

        };

        struct VertexOutput
        {
            float4 position : SV_POSITION;
            float2 uv : TEXCOORD0;
            // float4 ScreenPosition;
        };

        ENDHLSL

        // Main Pass
        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            float Remap(float In)
            {
                return 2 * (In - 0.5);
            }

            float GetRemain(float In, float Divider)
            {
                return In%Divider;
            }

            float ZeroToOneCurve_DoubleEase(float In)
            {
                In = saturate(In);
                float Pi = 3.14159265;
                return 1 - ( cos(In*Pi)+1 )/2;
            }

            float ZeroToOneCurve_UP(float In)
            {
                In = saturate(In);
                return pow( 1 - pow(1 - In, 2), 0.5);
            }

            VertexOutput vert(VertexInput i)
            {
                VertexOutput o;

                o.position = TransformObjectToHClip(i.position.xyz);
                o.uv = i.uv;
                return o;
            }

            float4 frag(VertexOutput i) : SV_Target
            {
                float4 screenPos = ComputeScreenPos(TransformWorldToHClip(i.position));

                float2 directionVector2 = float2(Remap(i.uv.x), Remap(i.uv.y));
                float closenessToCenter = 1 - saturate( length(directionVector2) );
                float fixedClosenessToCenter = ZeroToOneCurve_UP(closenessToCenter);
                float value1 = fixedClosenessToCenter + ( (_TimeParameters.x * _WaveSpd)%1 );
                float freqFactor = _Freq * 0.5;
                value1 = abs( (value1 % _Freq) - freqFactor) * ( 1 / freqFactor);
                value1 = ZeroToOneCurve_DoubleEase(value1) * _DisStr * closenessToCenter;
                float2 UVfixFactor = float2( directionVector2.x * value1, directionVector2.y * value1 );
                screenPos = float4(screenPos.xy / screenPos.w, 0, 0);
                float2 fixedUV = screenPos + UVfixFactor;

                float4 texColor;
                // texColor = SAMPLE_TEXTURE2D(_GrabPassTransparentRT, _GrabPassTransparentRT, fixedUV);
                // texColor = SAMPLE_TEXTURE2D(_GrabPassTransparent, sampler_GrabPassTransparent, fixedUV);
                texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, fixedUV);
                return lerp((texColor * _BaseColor), _BlendColor, _BlendOpacity);
            }

            ENDHLSL
        }
    }

}