Shader "Ultraleap/Passthrough/Background"
{
    Properties
    {
        [MaterialToggle] _MirrorImageHorizontally ("MirrorImageHorizontally", Float) = 0
        _DeviceID ("DeviceID", Int) = 0
        _Tint ("Tint", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "IgnoreProjector" = "True"
            "RenderType" = "Opaque"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Tint;

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(positionOS);
            }

            fixed4 frag(float4 positionCS : SV_POSITION) : SV_Target
            {
                return _Tint;
            }
            ENDCG
        }
    }

    Fallback Off
}
