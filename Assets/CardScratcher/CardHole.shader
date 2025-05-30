// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/CardHole"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _HoleCenterX ("HoleCenterX",Range(0,1)) = 0.5
        _HoleCenterY ("HoleCenterY", Range(0,1)) = 0.5
        _LastHoleCenterX ("LastHoleCenterX",Range(0,1)) = 0.5
        _LastHoleCenterY ("LastHoleCenterY", Range(0,1)) = 0.5
        _HoleRadius ("HoleRadius", Range(0,1)) = 0.2
        _Aspect ("Aspect(X:Y)", Range(0,1)) = 3

    }
    CGINCLUDE
        #include "unitycg.cginc"

        struct v2f
        {
            float4 pos:POSITION;
            float2 uv:TEXCOORD1;
        };

        sampler2D _MainTex;
        float _HoleCenterX;
        float _HoleCenterY;
        float _LastHoleCenterX;
        float _LastHoleCenterY;
        float _HoleRadius;
        float _Aspect; //比例修正

        v2f vert(appdata_base v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.texcoord;
            return o;
        }

        float4 frag(v2f i):COLOR
        {
            float4 mainColor = tex2D(_MainTex, i.uv);
            //本次挖洞点相对于上次的方向
            float2 dir = float2(_Aspect * (_HoleCenterX - _LastHoleCenterX), _HoleCenterY - _LastHoleCenterY);
            // float2 dirNormal = normalize(dirNormal);

            float2 lastVec = float2(_Aspect * (i.uv.x - _LastHoleCenterX), i.uv.y - _LastHoleCenterY);
            float2 thisVec = float2(_Aspect * (i.uv.x - _HoleCenterX), i.uv.y - _HoleCenterY);

            bool isInMiddleAera = dot(lastVec, dir) * dot(thisVec, dir) <= 0;

            if (isInMiddleAera)
            {
                float2 lastToVertical = normalize(dir) * dot(lastVec, normalize(dir));
                float2 distVec = lastVec - lastToVertical;
                // distVec = float2(_Aspect * distVec.x , distVec.y);
                //距离挖洞点的距离
                float dist = length(distVec);
                if (dist < _HoleRadius)
                {
                    mainColor.a = 1;
                }
            }
            else
            {
                float2 distVec = i.uv - float2(_HoleCenterX, _HoleCenterY);
                distVec = float2(_Aspect * distVec.x, distVec.y);
                float dist = length(distVec);
                if (dist < _HoleRadius)
                {
                    mainColor.a = 1;
                }
            }

            return mainColor;
        }

        fixed4 frag_black(v2f i): SV_TARGET
        {
            return fixed4(0, 0, 0, 0);
        }
		fixed4 frag_white(v2f i): SV_TARGET
        {
            return fixed4(1, 1, 1, 1);
        }
    ENDCG
    SubShader
    {
        ZTest Always//必须加，否则2018.4.18有问题，2019正常
        // Blend SrcAlpha OneMinusSrcAlpha
        // Tags
        // {
        //     "RenderType" = "Transparent" "Queue" = "Transparent"
        // }
        pass
        {
            //必须加Name，否则Graphics.Blit会出错
            Name "Update" // 0
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }

        pass//清零pass,用来重置RT
        {
            Name "Clear" // 1
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_black
            ENDCG
        }        
		pass//清零pass,用来重置RT
        {
            Name "Clear2" // 2
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_white
            ENDCG
        }
    }
    FallBack "Diffuse"
}