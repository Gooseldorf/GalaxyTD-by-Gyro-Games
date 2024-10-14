Shader "AnimationBakingStudio/NormalMap"
{
    Properties
    {
        _RotX ("Rotation around X Axis", Range (0, 90)) = 0
        _RotY ("Rotation around Y Axis", Range (0, 360)) = 0
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // include file that contains UnityObjectToWorldNormal helper function
            #include "UnityCG.cginc"

            half _RotX;
            half _RotY;

            struct v2f {
                // we'll output world space normal as one of regular ("texcoord") interpolators
                half3 worldNormal : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            float3 RotateAroundXInDegrees (float3 dir, float degree)
            {
                float radian = degree * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(radian, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, dir.yz), dir.x).zxy;
            }

            float3 RotateAroundYInDegrees (float3 dir, float degree)
            {
                float radian = degree * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(radian, sina, cosa);
                float2x2 m = float2x2(cosa, sina, -sina, cosa);
                return float3(mul(m, dir.xz), dir.y).xzy;
            }

            // vertex shader: takes object space normal as input too
            v2f vert (float4 vertex : POSITION, float3 normal : NORMAL)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                // UnityCG.cginc file contains function to transform
                // normal from object to world space, use that
                float3 worldNormal = UnityObjectToWorldNormal(normal);
                worldNormal = RotateAroundYInDegrees(worldNormal, _RotY);
                worldNormal = RotateAroundXInDegrees(worldNormal, _RotX);
                o.worldNormal = worldNormal;
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = 0;
                // normal is a 3D vector with xyz components; in -1..1
                // range. To display it as color, bring the range into 0..1
                // and put into red, green, blue components
                float3 normal = i.worldNormal;
                normal.x = -normal.x;
                c.rgb = normal * 0.5 + 0.5;
                return c;
            }
            ENDCG
        }
    }
}
