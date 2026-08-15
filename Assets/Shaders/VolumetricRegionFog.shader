Shader "Vespidae/Volumetric Region Fog"
{
    Properties
    {
        [Header(Region Mask)]
        [Space(4)]
        _RegionMask ("Region mask  RGB = bands, A = base density", 2D) = "white" {}
        _MaskMin ("Mask world min (X, Z)", Vector) = (-28.72, -16.48, 0, 0)
        _MaskMax ("Mask world max (X, Z)", Vector) = (21.14, 14.21, 0, 0)
        _RegionsRevealed ("Regions revealed (R, G, B)", Vector) = (0, 0, 0, 0)
        _RevealEdge ("Reveal erosion softness", Range(0.02, 0.6)) = 0.18
        // The mask stores band coverage as a long gradient falling away from the hexes. Gaining that
        // up before it is thresholded dilates the clear middle without repainting anything, which is
        // what makes the size of the opening a live dial rather than a bake.
        _OpeningExpand ("Opening expand", Range(0.25, 5)) = 1

        [Header(Volume)]
        [Space(4)]
        _SlabBottom ("Slab bottom Y", Float) = -1.5
        _SlabTop ("Slab top Y", Float) = 3.4
        _Steps ("Raymarch steps", Range(4, 32)) = 14
        _Density ("Density", Range(0, 8)) = 2.4
        _MaxDistance ("Max march distance", Float) = 120

        [Header(Noise)]
        [Space(4)]
        _NoiseScale ("Noise scale", Float) = 0.34
        _WarpStrength ("Domain warp", Range(0, 4)) = 1.8
        _Wind ("Wind (X, Z)", Vector) = (0.035, 0.012, 0, 0)
        _BreatheAmount ("Breathe amount", Range(0, 1)) = 0.25
        _BreathePeriod ("Breathe period (seconds)", Float) = 30

        [Header(Shading)]
        [Space(4)]
        _ColourDeep ("Colour in the mass", Color) = (0.46, 0.53, 0.62, 1)
        _ColourLit ("Colour at the lit edge", Color) = (0.97, 0.95, 0.91, 1)
        _SunDir ("Sun direction", Vector) = (-0.4, 0.75, -0.5, 0)
        _ShadowStrength ("Self shadow strength", Range(0, 1)) = 0.7

        [Header(Map Edge)]
        [Space(4)]
        _PlayMin ("Play area world min (X, Z)", Vector) = (-9, -14.4, 0, 0)
        _PlayMax ("Play area world max (X, Z)", Vector) = (14.4, 10.8, 0, 0)
        _EdgeStart ("Thickening starts this far out (world units)", Float) = 1
        _EdgeEnd ("Fully closed in by this far out (world units)", Float) = 6
        _EdgeNoiseScale ("Edge break-up scale", Float) = 0.09
        _EdgeNoiseAmount ("Edge break-up, in world units", Float) = 3
        // Well above 1 on purpose. The terrain plane stops only a few units past the last hex, so the
        // wall has to reach full opacity over a short path - including the steep, short paths of the
        // pixels nearest the camera - without the fog over the map itself getting any thicker.
        _EdgeWallDensity ("Outer wall density", Range(0, 8)) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            // After the transparent hex tiles, not alongside them. Several hex materials are
            // transparent with ZWrite off, so they never appear in the depth texture the march
            // clamps against, and at a shared queue the draw order flips with camera distance -
            // which shows up as some tiles rendering on top of the fog and others not. The slab
            // sits above the hex plane, so the fog belongs over them either way.
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "VolumetricRegionFog"

            // The shader composites the volume itself and outputs a premultiplied colour, so the
            // blend only has to apply the transmittance the march already worked out.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            // The box is only a surface to rasterise from - the march itself stops at scene depth,
            // so occlusion is already handled. Depth testing it would punch holes in the fog
            // wherever terrain happens to sit above the slab.
            ZTest Always
            // Back faces only. A flat quad loses the fog entirely the moment the camera drops below
            // it, so the volume is rasterised from a box the camera sits inside instead. Culling the
            // front faces keeps that to one fragment per pixel from inside or outside - with Cull Off
            // a box would hand the march two fragments and composite the fog twice.
            Cull Front

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #define FBM_OCTAVES 3

            TEXTURE2D(_RegionMask);
            SAMPLER(sampler_RegionMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _MaskMin;
                float4 _MaskMax;
                float4 _RegionsRevealed;
                float  _RevealEdge;
                float  _OpeningExpand;

                float  _SlabBottom;
                float  _SlabTop;
                float  _Steps;
                float  _Density;
                float  _MaxDistance;

                float  _NoiseScale;
                float  _WarpStrength;
                float4 _Wind;
                float  _BreatheAmount;
                float  _BreathePeriod;

                float4 _ColourDeep;
                float4 _ColourLit;
                float4 _SunDir;
                float  _ShadowStrength;

                float4 _PlayMin;
                float4 _PlayMax;
                float  _EdgeStart;
                float  _EdgeEnd;
                float  _EdgeNoiseScale;
                float  _EdgeNoiseAmount;
                float  _EdgeWallDensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                return output;
            }

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash13(i);
                float n100 = Hash13(i + float3(1, 0, 0));
                float n010 = Hash13(i + float3(0, 1, 0));
                float n110 = Hash13(i + float3(1, 1, 0));
                float n001 = Hash13(i + float3(0, 0, 1));
                float n101 = Hash13(i + float3(1, 0, 1));
                float n011 = Hash13(i + float3(0, 1, 1));
                float n111 = Hash13(i + float3(1, 1, 1));

                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            float Fbm3(float3 p)
            {
                float sum = 0.0;
                float amplitude = 0.5;

                [unroll]
                for (int i = 0; i < FBM_OCTAVES; i++)
                {
                    sum += amplitude * ValueNoise3(p);
                    p = p * 2.03 + float3(1.7, 9.2, 4.1);
                    amplitude *= 0.5;
                }
                return sum;
            }

            float2 MaskUV(float3 positionWS)
            {
                return (positionWS.xz - _MaskMin.xy) / max(_MaskMax.xy - _MaskMin.xy, 1e-4);
            }

            // Density of the fog volume at a world point, before the reveal is applied.
            // heightFade and raw are handed back so the map edge can reuse them: the edge wall has to
            // feather at the top and bottom of the slab just like the rest of the volume does, and it
            // keeps a little of the cloud detail so it does not read as a painted grey card.
            float SampleShape(float3 positionWS, float breathe, out float heightFade, out float raw)
            {
                float3 p = positionWS * _NoiseScale;
                p.xz += _Wind.xy * _Time.y;

                // Domain warp - offsetting the sample position by another noise field is what turns
                // a sliding texture into something that folds and churns like real cloud.
                float3 warp = float3(Fbm3(p * 0.6),
                                     Fbm3(p * 0.6 + float3(5.2, 1.3, 7.4)),
                                     Fbm3(p * 0.6 + float3(2.8, 6.1, 3.5))) - 0.35;

                raw = Fbm3(p + warp * _WarpStrength);

                // Soft top and bottom so the slab never shows a hard cut at its own boundary.
                float mid = (_SlabTop + _SlabBottom) * 0.5;
                float halfHeight = max((_SlabTop - _SlabBottom) * 0.5, 1e-4);
                heightFade = 1.0 - saturate(abs(positionWS.y - mid) / halfHeight);
                heightFade = heightFade * heightFade * (3.0 - 2.0 * heightFade);

                return saturate(raw * 1.85 - 0.42 + breathe) * heightFade;
            }

            // How far outside the play area this point lies, in world units. Zero anywhere over the
            // hexes. Measuring in world units rather than mask UV matters: the mask is not square and
            // not centred on the hexes, so a UV-space ramp thickens unevenly and eats into one side of
            // the map before it has started on the other.
            //
            // length(max(d, 0)) is the distance to the rectangle, which rounds the corners for free -
            // no corner ever reads as the meeting point of two drawn lines.
            float EdgeDistance(float3 positionWS)
            {
                float2 d = max(_PlayMin.xy - positionWS.xz, positionWS.xz - _PlayMax.xy);
                float outside = length(max(d, 0.0));

                // Wobbling the distance itself is what stops the transition ever reading as a straight
                // line: the fog eats in as bays and headlands instead of as a rectangle.
                outside += (Fbm3(positionWS * _EdgeNoiseScale) - 0.5) * _EdgeNoiseAmount;
                return outside;
            }

            // Applies the painted mask: which band this point sits in, how thick the fog is there by
            // default, whether that band has been revealed, and the long ramp out to the map edge.
            float ApplyMask(float3 positionWS, float shape, float heightFade, float raw)
            {
                float2 uv = MaskUV(positionWS);
                float4 mask = SAMPLE_TEXTURE2D_LOD(_RegionMask, sampler_RegionMask, saturate(uv), 0);

                float density = shape * mask.a;

                // Black in the mask means outside the playable area, so revealed stays 0 and that
                // ground never clears no matter what the reveal vector says.
                float revealed = saturate(dot(mask.rgb, _RegionsRevealed.rgb) * _OpeningExpand);

                // Erosion rather than a fade - thin fog burns off first, in patches. The threshold is
                // pushed a soft edge past both ends so a fully revealed band clears completely: left
                // at raw 0-1 the densest patches never quite reach zero and a revealed region keeps a
                // permanent haze over it.
                float threshold = lerp(-_RevealEdge, 1.0 + _RevealEdge, revealed);
                float cleared = smoothstep(shape - _RevealEdge, shape + _RevealEdge, threshold);
                density *= (1.0 - cleared);

                // Thicken over a long distance so the playable area sits inside an open bowl that
                // closes over well past the last hex. Smoothstep twice: the second pass flattens
                // both ends of the ramp, which is what removes the visible line where it starts.
                float edge = smoothstep(_EdgeStart, max(_EdgeEnd, _EdgeStart + 0.01), EdgeDistance(positionWS));
                edge = edge * edge * (3.0 - 2.0 * edge);

                float wall = heightFade * _EdgeWallDensity * lerp(0.7, 1.0, saturate(raw));
                return lerp(density, wall, edge);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(input.positionWS - rayOrigin);

                // Slab entry and exit. Guard the near-horizontal case, where a ray can run for a
                // very long way through the volume and the division blows up.
                float invDirY = 1.0 / (abs(rayDir.y) < 1e-4 ? (rayDir.y >= 0.0 ? 1e-4 : -1e-4) : rayDir.y);
                float tA = (_SlabTop - rayOrigin.y) * invDirY;
                float tB = (_SlabBottom - rayOrigin.y) * invDirY;
                float tEntry = max(min(tA, tB), 0.0);
                float tExit = max(tA, tB);

                if (tExit <= tEntry)
                    return float4(0, 0, 0, 0);

                // Stop at whatever the opaque scene already drew, so terrain, hexes and wasps
                // occlude the fog correctly and the intersection feathers for free.
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float rawDepth = SampleSceneDepth(screenUV);
                float eyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float3 cameraForward = -UNITY_MATRIX_V._m20_m21_m22;
                float sceneDistance = eyeDepth / max(dot(rayDir, cameraForward), 1e-4);

                tExit = min(tExit, min(sceneDistance, tEntry + _MaxDistance));
                if (tExit <= tEntry)
                    return float4(0, 0, 0, 0);

                int steps = (int)_Steps;
                float stepLength = (tExit - tEntry) / steps;

                // A wide field of view makes step length vary a lot across the screen, which shows
                // up as banding. Offsetting each pixel's first sample turns those bands into noise.
                float dither = Hash13(float3(screenUV * _ScreenParams.xy, _Time.y * 60.0));

                float breathe = (sin(_Time.y * 6.2831853 / max(_BreathePeriod, 0.01)) * 0.5) * _BreatheAmount;
                float3 sunDir = normalize(_SunDir.xyz);

                float3 accumulated = 0.0;
                float transmittance = 1.0;

                [loop]
                for (int i = 0; i < steps; i++)
                {
                    float t = tEntry + (i + dither) * stepLength;
                    float3 samplePos = rayOrigin + rayDir * t;

                    float heightFade;
                    float raw;
                    float shape = SampleShape(samplePos, breathe, heightFade, raw);

                    float density = ApplyMask(samplePos, shape, heightFade, raw) * _Density * stepLength;
                    if (density <= 0.0005)
                        continue;

                    // One extra sample toward the sun gives the mass form instead of reading flat.
                    float towardHeightFade;
                    float towardRaw;
                    float toward = SampleShape(samplePos + sunDir * 0.55, breathe, towardHeightFade, towardRaw);
                    float shadow = saturate((toward - shape) * 1.8) * _ShadowStrength;

                    float3 colour = lerp(_ColourDeep.rgb, _ColourLit.rgb, smoothstep(0.05, 0.6, shape));
                    colour *= (1.0 - shadow * 0.75);

                    density = saturate(density);
                    accumulated += transmittance * density * colour;
                    transmittance *= (1.0 - density);

                    if (transmittance < 0.02)
                        break;
                }

                return float4(accumulated, 1.0 - transmittance);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
