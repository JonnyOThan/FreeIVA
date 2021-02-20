Shader "KSP/Alpha/Translucent Fresnel Additive"
{
	Properties 
	{
		_MainTex("MainTex (RGBA)", 2D) = "white" {}
		_Fresnel("Frensel fade intensity", Float) = 1.0
		_TintColor ("Overlay color", Color) = (1,1,1,1)
	}
	
	Category 
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend SrcAlpha One
		AlphaTest Greater .01
		ColorMask RGB
		Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
		BindChannels {
			Bind "Color", color
			Bind "Vertex", vertex 
			Bind "TexCoord", texcoord 
		}
	
		SubShader 
		{
			Pass 
			{
				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest 
				#pragma multi_compile_particles
				#include "UnityCG.cginc"

				sampler2D _MainTex;
				fixed4 _TintColor;
				float _Fresnel;

				struct v2f 
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float2 texcoord : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float3 viewDir : TEXCOORD2;
					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD3;
					#endif
				};
			
				float4 _MainTex_ST;

				v2f vert (appdata_full v)
				{
					v2f o;
					o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
					#ifdef SOFTPARTICLES_ON
					o.projPos = ComputeScreenPos (o.vertex);
					COMPUTE_EYEDEPTH(o.projPos.z);
					#endif
					o.color = v.color;
					o.normal = v.normal;
					o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
					o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
					return o;
				}

				sampler2D _CameraDepthTexture;
				float _InvFade;
			
				fixed4 frag (v2f i) : COLOR
				{
					#ifdef SOFTPARTICLES_ON
					float sceneZ = LinearEyeDepth (UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))));
					float partZ = i.projPos.z;
					float fade = saturate (_InvFade * (sceneZ-partZ));
					i.color.a *= fade;
					#endif

					float3 normal = i.normal;
					half rim = 1.0 - saturate(dot (normalize(i.viewDir), normal));
					return 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord) * pow ((1 - rim), _Fresnel);
				}
				ENDCG 
			}
		}
		
			SubShader 
	{
		Pass {
			SetTexture [_MainTex] {
				constantColor [_TintColor]
				combine constant * primary
			}
			SetTexture [_MainTex] {
				combine texture * previous DOUBLE
			}
		}
	}
	
	SubShader {
		Pass {
			SetTexture [_MainTex] {
				combine texture * primary
			}
		}
	}	
					
	}
}