Shader "UI/PhotoWithMaskLerp"
{
    Properties
    {
        _MainTex ("Photo", 2D) = "white" {}
        _MaskA   ("Mask A", 2D) = "white" {}
        _MaskB   ("Mask B", 2D) = "white" {}
        _Blend   ("Mask Blend 0..1", Range(0,1)) = 0
        _Color   ("Tint", Color) = (1,1,1,1)
        _Cutoff  ("Alpha Cutoff", Range(0,1)) = 0.001

        // 幾何/圓形參數
        _RectAspect   ("Rect Aspect (W/H)", Float) = 1.0
        _CircleInner  ("Circle Inner (0..1)", Range(0,1)) = 0.88
        _BorderFeather("Border Feather (0..0.25)", Range(0,0.25)) = 0.04

        // 邊界“變形蟲”噪聲
        _EdgeNoiseFreq     ("Edge Noise Freq", Float) = 12.0   // 沿圓周的顆粒數
        _EdgeNoiseStrength ("Edge Noise Strength (0..0.3)", Range(0,0.3)) = 0.08
        _EdgeNoisePhase    ("Edge Noise Phase", Float) = 0.0   // 可改變形狀或做動畫

        // 遮罩對邊界形狀的影響
        _MaskInfluence ("Mask Influence (0..1)", Range(0,1)) = 0.5
        _MaskContrast  ("Mask Contrast", Range(0.5,4.0)) = 1.5
        _InvertMask    ("Invert Mask (0/1)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _MaskA;   float4 _MaskA_ST;
            sampler2D _MaskB;   float4 _MaskB_ST;
            float _Blend, _Cutoff;
            float4 _Color;

            float _RectAspect, _CircleInner, _BorderFeather;
            float _EdgeNoiseFreq, _EdgeNoiseStrength, _EdgeNoisePhase;
            float _MaskInfluence, _MaskContrast, _InvertMask;

            struct vIn { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct vOut {
                float4 pos:SV_POSITION;
                float2 uv0:TEXCOORD0;
                float2 uvA:TEXCOORD1;
                float2 uvB:TEXCOORD2;
            };

            vOut vert (vIn v)
            {
                vOut o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv0 = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvA = TRANSFORM_TEX(v.uv, _MaskA);
                o.uvB = TRANSFORM_TEX(v.uv, _MaskB);
                return o;
            }

            // 1D 平滑雜訊（value noise）
            float hash11(float p){ return frac(sin(p*12.9898)*43758.5453); }
            float noise1(float x){
                float i = floor(x), f = frac(x);
                float u = f*f*(3.0-2.0*f);
                return lerp(hash11(i), hash11(i+1.0), u);
            }

            // 以較短邊為基準的圓距離：中心0 → 內切圓1
            float circleRadius01(float2 uv, float rectAspect)
            {
                float2 d = uv - 0.5;
                if (rectAspect >= 1.0) d.x /= rectAspect;
                else                    d.y *= rectAspect;
                return length(d) / 0.5;
            }

            fixed4 frag (vOut i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv0) * _Color;

                // 融合遮罩
                fixed aA = tex2D(_MaskA, i.uvA).a;
                fixed aB = tex2D(_MaskB, i.uvB).a;
                float maskAlpha = lerp(aA, aB, saturate(_Blend));
                if (_InvertMask > 0.5) maskAlpha = 1.0 - maskAlpha;
                // 調對比：越小越白（不切），越大越黑（切多）
                maskAlpha = pow(saturate(maskAlpha), _MaskContrast);

                // 圓半徑 & 角度（0..1）— 用於沿圓周取噪聲
                float2 d = i.uv0 - 0.5;
                if (_RectAspect >= 1.0) d.x /= _RectAspect; else d.y *= _RectAspect;
                float r = length(d) / 0.5;
                float theta = atan2(d.y, d.x);               // [-pi, pi]
                float t = theta * (1.0/(2.0*UNITY_PI)) + 0.5; // 0..1

                // 變形蟲邊界的位移：噪聲 + 遮罩影響
                float n = noise1(t * _EdgeNoiseFreq + _EdgeNoisePhase); // 0..1
                float dispNoise = (n*2.0 - 1.0) * _EdgeNoiseStrength;   // -S..S
                float dispMask  = ((1.0 - maskAlpha)*2.0 - 1.0) * (_EdgeNoiseStrength * _MaskInfluence);
                float innerEff  = clamp(_CircleInner + dispNoise + dispMask, 0.0, 1.0);

                // 以“變形後”的內半徑作為形狀閾值：內側滿、外側透明；邊緣 feather 柔化
                float inside = 1.0 - smoothstep(innerEff, innerEff + _BorderFeather, r);

                col.a *= inside;
                clip(col.a - _Cutoff);
                return col;
            }
            ENDCG
        }
    }
}
