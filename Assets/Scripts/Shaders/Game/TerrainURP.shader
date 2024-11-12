Shader "Custom/TerrainURP"
{
    Properties
    {
        _ColWest("Colour West", 2D) = "white" {}
        _ColEast("Colour East", 2D) = "white" {}
        _NormalMapWest("Normal Map West", 2D) = "white" {}
        _NormalMapEast("Normal Map East", 2D) = "white" {}
        _LightMap("Light Map", 2D) = "white" {}
        _LakeMask("Lake Mask", 2D) = "white" {}

        [Header(Lighting)]
        _AmbientNight("Ambient Night", Color) = (0,0,0,0)
        _CityLightAmbient("City Light Ambient", Color) = (0,0,0,0)
        _FresnelCol("Fresnel Col", Color) = (0,0,0,0)
        _Contrast("Contrast", Float) = 1
        _BrightnessAdd("Brightness Add", Float) = 0
        _BrightnessMul("Brightness Mul", Float) = 1

        [Header(Shadows)]
        _ShadowStrength("Shadow Strength", Range(0,1)) = 1
        _ShadowEdgeCol("Shadow Edge Col", Color) = (0,0,0,0)
        _ShadowInnerCol("Shadow Inner Col", Color) = (0,0,0,0)

        [Header(Lakes)]
        _Specular("Specular", Float) = 0
        [NoScaleOffset] _WaveNormalA("Wave Normal A", 2D) = "bump" {}
        _WaveNormalScale("Wave Normal Scale", Float) = 1
        _WaveStrength("Wave Strength", Range(0, 1)) = 1

        [Header(Test)]
        _TestParams("Test Params", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
            };

            TEXTURE2D(_ColWest);
            SAMPLER(sampler_ColWest);
            TEXTURE2D(_ColEast);
            SAMPLER(sampler_ColEast);
            TEXTURE2D(_NormalMapWest);
            SAMPLER(sampler_NormalMapWest);
            TEXTURE2D(_NormalMapEast);
            SAMPLER(sampler_NormalMapEast);
            TEXTURE2D(_LightMap);
            SAMPLER(sampler_LightMap);
            TEXTURE2D(_LakeMask);
            SAMPLER(sampler_LakeMask);
            TEXTURE2D(_WaveNormalA);
            SAMPLER(sampler_WaveNormalA);

            float _ShadowStrength;
            float3 _ShadowEdgeCol;
            float3 _ShadowInnerCol;
            float _WaveNormalScale;
            float _WaveStrength;
            float _Specular;
            float _BrightnessAdd;
            float _BrightnessMul;
            float _Contrast;
            float4 _AmbientNight;
            float4 _CityLightAmbient;
            float4 _FresnelCol;
            float4 _TestParams;

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
                output.positionHCS = vertexInput.positionHCS;
                output.worldPos = vertexInput.positionWS;
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
                output.shadowCoord = GetMainLightShadowCoord(vertexInput.positionWS);
                return output;
            }

            float calculateSpecular(float3 normal, float3 viewDir, float3 dirToSun, float smoothness)
            {
                float specularAngle = acos(dot(normalize(dirToSun - viewDir), normal));
                float specularExponent = specularAngle / smoothness;
                float specularHighlight = exp(-max(0, specularExponent) * specularExponent);
                return specularHighlight;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 pointOnUnitSphere = normalize(input.worldPos);
                float2 texCoord = pointToUV(pointOnUnitSphere);
                float lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, texCoord).r;
                float lakeMask = SAMPLE_TEXTURE2D(_LakeMask, sampler_LakeMask, texCoord).r;

                float3 detailNormal;
                float3 unlitTerrainCol;
                if (texCoord.x < 0.5)
                {
                    float2 tileTexCoord = float2(texCoord.x * 2, texCoord.y);
                    unlitTerrainCol = SAMPLE_TEXTURE2D(_ColWest, sampler_ColWest, tileTexCoord).rgb;
                    detailNormal = SAMPLE_TEXTURE2D(_NormalMapWest, sampler_NormalMapWest, tileTexCoord).rgb;
                }
                else
                {
                    float2 tileTexCoord = float2((texCoord.x - 0.5) * 2, texCoord.y);
                    unlitTerrainCol = SAMPLE_TEXTURE2D(_ColEast, sampler_ColEast, tileTexCoord).rgb;
                    detailNormal = SAMPLE_TEXTURE2D(_NormalMapEast, sampler_NormalMapEast, tileTexCoord).rgb;
                }

                float3 meshWorldNormal = normalize(input.worldNormal);
                detailNormal = normalize(detailNormal * 2 - 1);
                float3 worldNormal = normalize(meshWorldNormal * 2 + detailNormal * 1.25);
                float3 dirToSun = GetMainLightDirection();
                float3 viewDir = normalize(input.viewDirWS);

                float3 waveA = TriplanarSampleNormal(_WaveNormalA, sampler_WaveNormalA, input.worldPos, pointOnUnitSphere, _WaveNormalScale) * _WaveStrength;
                float lakeSpecular = calculateSpecular(waveA, viewDir, dirToSun, _Specular) * lakeMask;

                float shadows = SampleMainLightShadow(input.shadowCoord);
                float3 shadowCol = lerp(_ShadowEdgeCol, _ShadowInnerCol, saturate((1 - shadows) * 1.5));
                shadows = lerp(1, shadows, _ShadowStrength);

                float fakeLighting = pow(dot(worldNormal, pointOnUnitSphere), 3);
                float greyscale = dot(unlitTerrainCol, float3(0.299, 0.587, 0.114));
                float3 nightCol = (pow(greyscale, 0.67) * fakeLighting + fakeLighting * 0.3) * lerp(_AmbientNight.rgb * 0.1, _CityLightAmbient.rgb, saturate(lightMap * 1));
                float fresnel = saturate(1.5 * pow(1 + dot(viewDir, worldNormal), 5));
                nightCol += fresnel * _FresnelCol.rgb;
                float nightT = smoothstep(-0.25, 0.25, dot(pointOnUnitSphere, dirToSun));

                float3 shading = saturate(dot(worldNormal, dirToSun) + _BrightnessAdd) * _BrightnessMul;
                float3 terrainCol = unlitTerrainCol * shading + lakeSpecular;
                terrainCol = lerp(terrainCol, shadowCol, 1 - shadows);
                terrainCol = lerp(0.5, terrainCol, _Contrast);
                terrainCol *= lerp(fakeLighting, 1, 0.5);

                float3 finalTerrainCol = lerp(nightCol, terrainCol, nightT);
                return half4(finalTerrainCol, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
