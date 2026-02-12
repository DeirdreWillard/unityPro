// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "G/G_dissolve_add_norotate_optimized"
{
	Properties
	{
		[Enum(UnityEngine.Rendering.CullMode)]_CullMode("Cull Mode", Float) = 2
		[Enum(Off,0,On,1)]_ZWirteMode("ZWirte Mode", Float) = 0
		[Enum(ValueCtrl,0,particleCtrl,1)]_Control("Control", Float) = 0
		[Enum(Color,0,Gray,1)]_MT_Desaturate("MT_Desaturate", Float) = 0
		_MainColor("MainColor", Color) = (1,1,1,0)
		_MainTex("MainTex", 2D) = "white" {}
		_Intensity("Intensity", Float) = 1
		_MT_SpeedX("MT_SpeedX", Float) = 0
		_MT_SpeedY("MT_SpeedY", Float) = 0
		[Enum(MainTex,0,DissolveTex,1)]_PannerType("PannerType", Float) = 0
		_DissolveTex("DissolveTex(XY ctrl offset)", 2D) = "white" {}
		_Diss_Ramp("Diss_Ramp", Float) = 0.1
		_DissScale("DissScale(Z ctrl)", Range( 0 , 1)) = 0.6175
		_Diss_Amount("Diss_Amount(W ctrl)", Range( 0 , 1)) = 0
		[Enum(ValueCtrl,0,VertexAlpha,1)]_VertexCtrl_Diss("VertexCtrl_Diss", Float) = 0
		[Enum(Normal,0,Invert,1)]_DissolveInvert("DissolveInvert", Float) = 0
		[Enum(ValueCtrl,0,particleCtrl,1)]_DissScaleControl("DissScaleControl", Float) = 0
		_Diss_SpeedX("Diss_SpeedX", Float) = 0
		_Diss_SpeedY("Diss_SpeedY", Float) = 0
		_DistortTex("DistortTex", 2D) = "black" {}
		[Enum(NotAffect_Disss,0,Affect_Disss,1)]_DistortType1("DistortType1", Float) = 0
		[Enum(NotAffect_MT,0,Affect_MT,1)]_DistortType2("DistortType2", Float) = 0
		[Enum(NotAffect_Mask,0,Affect_Mask,1)]_DistortType3("DistortType3", Float) = 0
		_Dist_AmountX("Dist_AmountX", Float) = 0
		_Dist_AmountY("Dist_AmountY", Float) = 0
		_Dist_AmountRadiation("Dist_AmountRadiation", Float) = 0
		_Dist_SpeedX("Dist_SpeedX", Float) = 0
		_Dist_SpeedY("Dist_SpeedY", Float) = 0
		_MaskTex("MaskTex", 2D) = "white" {}
		[Enum(Normal,0,Invert,1)]_MaskInvert("MaskInvert", Float) = 0
		[Enum(Fresnel_off,0,Fresnel_on,1)]_FresnelSwitch("FresnelSwitch", Float) = 0
		_FresnelColor_In("FresnelColor_In", Color) = (0,0,0,0)
		_FresnelColor_Out("FresnelColor_Out", Color) = (1,1,1,0)
		_Fresnel_Scale("Fresnel_Scale", Float) = 1
		_Fresnel_Power("Fresnel_Power", Float) = 2
		_Fresnel_Intensity("Fresnel_Intensity", Float) = 1
		[HideInInspector]_StencilComp("Stencil Comparison", Float) = 8
		[HideInInspector]_StencilWriteMask("Stencil Write Mask", Float) = 255
		[HideInInspector]_Stencil("Stencil ID", Float) = 0
		[HideInInspector]_StencilOp("Stencil Operation", Float) = 0
		[HideInInspector]_StencilReadMask("Stencil Read Mask", Float) = 255
		[HideInInspector]_ColorMask("Color Mask", Float) = 15

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
	LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend One One
		Cull [_CullMode]
		ColorMask RGBA
		ZWrite [_ZWirteMode]
		ZTest LEqual
		Offset 0 , 0
		Stencil
		{
			Ref [_Stencil]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
			Comp [_StencilComp]
			Pass [_StencilOp]
		}
		
		
		Pass
		{
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float3 ase_normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
			};

			uniform float _StencilWriteMask;
			uniform float _StencilReadMask;
			uniform float _Stencil;
			uniform float _StencilComp;
			uniform float _ColorMask;
			uniform float _CullMode;
			uniform float _StencilOp;
			uniform float _ZWirteMode;
			uniform float4 _FresnelColor_In;
			uniform float4 _FresnelColor_Out;
			uniform float _Fresnel_Scale;
			uniform float _Fresnel_Power;
			uniform float _Fresnel_Intensity;
			uniform float _FresnelSwitch;
			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform sampler2D _DistortTex;
			uniform float4 _DistortTex_ST;
			uniform float _Dist_SpeedX;
			uniform float _Dist_SpeedY;
			uniform float _Dist_AmountX;
			uniform float _Dist_AmountY;
			uniform float _Dist_AmountRadiation;
			uniform float _DistortType2;
			uniform float _PannerType;
			uniform float _Control;
			uniform float _MT_SpeedX;
			uniform float _MT_SpeedY;
			uniform float _MT_Desaturate;
			uniform float4 _MainColor;
			uniform sampler2D _MaskTex;
			uniform float4 _MaskTex_ST;
			uniform float _DistortType3;
			uniform float _MaskInvert;
			uniform float _Intensity;
			uniform float _VertexCtrl_Diss;
			uniform float _DissolveInvert;
			uniform sampler2D _DissolveTex;
			uniform float4 _DissolveTex_ST;
			uniform float _DistortType1;
			uniform float _DissScaleControl;
			uniform float _DissScale;
			uniform float _Diss_SpeedX;
			uniform float _Diss_SpeedY;
			uniform float _Diss_Amount;
			uniform float _Diss_Ramp;

			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float3 ase_worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.ase_texcoord.xyz = ase_worldPos;
				float3 ase_worldNormal = UnityObjectToWorldNormal(v.ase_normal);
				o.ase_texcoord1.xyz = ase_worldNormal;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				o.ase_texcoord3 = v.ase_texcoord1;
				o.ase_color = v.color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.w = 0;
				o.ase_texcoord1.w = 0;
				o.ase_texcoord2.zw = 0;
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = vertexValue;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				float3 ase_worldPos = i.ase_texcoord.xyz;
				float3 ase_worldViewDir = UnityWorldSpaceViewDir(ase_worldPos);
				ase_worldViewDir = normalize(ase_worldViewDir);
				float3 ase_worldNormal = i.ase_texcoord1.xyz;
				float fresnelNdotV178 = dot( ase_worldNormal, ase_worldViewDir );
				float fresnelNode178 = ( 0.0 + _Fresnel_Scale * pow( 1.0 - fresnelNdotV178, _Fresnel_Power ) );
				float4 lerpResult181 = lerp( _FresnelColor_In , _FresnelColor_Out , fresnelNode178);
				float4 lerpResult196 = lerp( float4( 1,1,1,0 ) , saturate( ( lerpResult181 * _Fresnel_Intensity ) ) , _FresnelSwitch);
				float4 Fresnel186 = lerpResult196;
				float2 uv0_MainTex = i.ase_texcoord2.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 uv0_DistortTex = i.ase_texcoord2.xy * _DistortTex_ST.xy + _DistortTex_ST.zw;
				float mulTime54 = _Time.y * 0.1;
				float2 appendResult59 = (float2(( mulTime54 * _Dist_SpeedX ) , ( mulTime54 * _Dist_SpeedY )));
				float4 tex2DNode62 = tex2D( _DistortTex, ( uv0_DistortTex + appendResult59 ) );
				float3 desaturateInitialColor108 = tex2DNode62.rgb;
				float desaturateDot108 = dot( desaturateInitialColor108, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar108 = lerp( desaturateInitialColor108, desaturateDot108.xxx, 1.0 );
				float3 temp_output_129_0 = ( desaturateVar108 * tex2DNode62.a );
				float2 appendResult66 = (float2(( temp_output_129_0 * 0.1 * _Dist_AmountX ).x , ( temp_output_129_0 * 0.1 * _Dist_AmountY ).x));
				float2 uv0117 = i.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 _Vector0 = float2(0.5,0.5);
				float3 Distort67 = ( float3( appendResult66 ,  0.0 ) + ( temp_output_129_0 * float3( ( ( uv0117 - _Vector0 ) * distance( uv0117 , _Vector0 ) * -_Dist_AmountRadiation ) ,  0.0 ) ) );
				float4 uv11 = i.ase_texcoord3;
				uv11.xy = i.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult77 = (float2(uv11.x , uv11.y));
				float2 custom_uv76 = appendResult77;
				float pannertype135 = _PannerType;
				float dissctrl82 = _Control;
				float mulTime141 = _Time.y * 0.1;
				float2 appendResult146 = (float2(( mulTime141 * _MT_SpeedX ) , ( mulTime141 * _MT_SpeedY )));
				float2 MTspeed147 = appendResult146;
				float4 tex2DNode40 = tex2D( _MainTex, ( float3( uv0_MainTex ,  0.0 ) + ( Distort67 * _DistortType2 ) + float3( ( custom_uv76 * ( 1.0 - pannertype135 ) * dissctrl82 ) ,  0.0 ) + float3( MTspeed147 ,  0.0 ) ).xy );
				float3 desaturateInitialColor174 = tex2DNode40.rgb;
				float desaturateDot174 = dot( desaturateInitialColor174, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar174 = lerp( desaturateInitialColor174, desaturateDot174.xxx, _MT_Desaturate );
				float2 uv0_MaskTex = i.ase_texcoord2.xy * _MaskTex_ST.xy + _MaskTex_ST.zw;
				float4 tex2DNode24 = tex2D( _MaskTex, ( float3( uv0_MaskTex ,  0.0 ) + ( Distort67 * _DistortType3 ) ).xy );
				float3 desaturateInitialColor41 = tex2DNode24.rgb;
				float desaturateDot41 = dot( desaturateInitialColor41, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar41 = lerp( desaturateInitialColor41, desaturateDot41.xxx, 1.0 );
				float3 temp_output_165_0 = ( desaturateVar41 * tex2DNode24.a );
				float3 lerpResult168 = lerp( temp_output_165_0 , ( 1.0 - temp_output_165_0 ) , _MaskInvert);
				float alphactrl199 = _VertexCtrl_Diss;
				float lerpResult203 = lerp( i.ase_color.a , 1.0 , alphactrl199);
				float3 temp_cast_15 = (_DissolveInvert).xxx;
				float2 uv0_DissolveTex = i.ase_texcoord2.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
				float3 temp_output_79_0 = ( float3( uv0_DissolveTex ,  0.0 ) + float3( ( ( custom_uv76 * pannertype135 ) * dissctrl82 ) ,  0.0 ) + ( Distort67 * _DistortType1 ) );
				float3 temp_cast_20 = (0.5).xxx;
				float custom_z47 = uv11.z;
				float temp_output_105_0 = ( ( custom_z47 * _DissScaleControl ) + ( ( 1.0 - _DissScaleControl ) * _DissScale ) );
				float3 temp_cast_21 = (0.5).xxx;
				float3 lerpResult93 = lerp( ( temp_output_79_0 + ( ( temp_output_79_0 - temp_cast_20 ) / temp_output_105_0 ) ) , temp_cast_21 , temp_output_105_0);
				float mulTime157 = _Time.y * 0.1;
				float2 appendResult160 = (float2(( mulTime157 * _Diss_SpeedX ) , ( mulTime157 * _Diss_SpeedY )));
				float2 Dissspeed161 = appendResult160;
				float4 tex2DNode15 = tex2D( _DissolveTex, ( lerpResult93 + float3( Dissspeed161 ,  0.0 ) ).xy );
				float3 desaturateInitialColor20 = tex2DNode15.rgb;
				float desaturateDot20 = dot( desaturateInitialColor20, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar20 = lerp( desaturateInitialColor20, desaturateDot20.xxx, 1.0 );
				float custom_w48 = uv11.w;
				float lerpResult201 = lerp( ( ( _Diss_Amount * ( 1.0 - dissctrl82 ) ) + ( dissctrl82 * custom_w48 ) ) , ( 1.0 - i.ase_color.a ) , alphactrl199);
				float dissamount14 = lerpResult201;
				float temp_output_21_0 = ( dissamount14 * _Diss_Ramp );
				float3 temp_cast_25 = (( ( dissamount14 - _Diss_Ramp ) + temp_output_21_0 )).xxx;
				float3 temp_cast_26 = (( dissamount14 + temp_output_21_0 )).xxx;
				float3 temp_cast_27 = (0.0).xxx;
				float3 temp_cast_28 = (1.0).xxx;
				float3 diss37 = saturate( (temp_cast_27 + (abs( ( temp_cast_15 - ( desaturateVar20 * tex2DNode15.a ) ) ) - temp_cast_25) * (temp_cast_28 - temp_cast_27) / (temp_cast_26 - temp_cast_25)) );
				
				
				finalColor = float4( (saturate( ( Fresnel186 * ( ( float4( desaturateVar174 , 0.0 ) * _MainColor ) * tex2DNode40.a * float4( lerpResult168 , 0.0 ) * tex2DNode24.a * _MainColor * _Intensity * i.ase_color * lerpResult203 * float4( diss37 , 0.0 ) ) ) )).rgb , 0.0 );
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=17500
282;596;1251;428;7098.311;1437.337;3.222961;True;True
Node;AmplifyShaderEditor.SimpleTimeNode;54;-5887.388,-1043.378;Inherit;False;1;0;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;55;-5871.137,-949.3604;Inherit;False;Property;_Dist_SpeedX;Dist_SpeedX;26;0;Create;True;0;0;False;0;0;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;53;-5871.137,-869.2714;Inherit;False;Property;_Dist_SpeedY;Dist_SpeedY;27;0;Create;True;0;0;False;0;0;9.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;56;-5648.28,-1021.325;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;57;-5647.119,-906.4141;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;58;-5771.316,-1193.11;Inherit;False;0;62;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;59;-5475.334,-973.7355;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;60;-5305.87,-1014.36;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;62;-4766.127,-1024.079;Inherit;True;Property;_DistortTex;DistortTex;19;0;Create;True;0;0;True;0;-1;None;0724cd9f1e246ad4e82ced846b4d759a;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DesaturateOpNode;108;-4458.412,-1017.482;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;122;-4552.099,-313.6122;Inherit;False;Property;_Dist_AmountRadiation;Dist_AmountRadiation;25;0;Create;True;0;0;False;0;0;1.91;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;117;-4696.643,-634.3056;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;119;-4645.536,-500.7657;Inherit;False;Constant;_Vector0;Vector 0;21;0;Create;True;0;0;False;0;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleSubtractOpNode;127;-4369.511,-638.6546;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;129;-4264.944,-957.2263;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;115;-4036.267,-925.9602;Inherit;False;Constant;_Float4;Float 4;21;0;Create;True;0;0;False;0;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;128;-4312.457,-310.5066;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;63;-4040.975,-727.8785;Inherit;False;Property;_Dist_AmountY;Dist_AmountY;24;0;Create;True;0;0;False;0;0;0.35;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;116;-4417.104,-536.3111;Inherit;True;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;61;-4037.448,-818.6639;Inherit;False;Property;_Dist_AmountX;Dist_AmountX;23;0;Create;True;0;0;False;0;0;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;65;-3754.148,-955.0832;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT;0.1;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;123;-4096.43,-539.1911;Inherit;True;3;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;1;-5501.728,81.03851;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;64;-3751.872,-824.834;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT;0.1;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DynamicAppendNode;77;-5144.864,89.17884;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;66;-3584.936,-871.2815;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;131;-3758.754,-563.4638;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-4523.497,195.3391;Inherit;False;Property;_Control;Control;2;1;[Enum];Create;True;2;ValueCtrl;0;particleCtrl;1;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;76;-4986.183,78.86529;Inherit;False;custom_uv;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;130;-3386.115,-704.2383;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;134;-6992.368,722.2337;Inherit;False;Property;_PannerType;PannerType;9;1;[Enum];Create;True;2;MainTex;0;DissolveTex;1;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;71;-6787.392,611.2025;Inherit;False;76;custom_uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;67;-3203.392,-862.9868;Inherit;False;Distort;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;135;-6789.523,723.0646;Inherit;False;pannertype;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;82;-4307.065,192.8008;Inherit;False;dissctrl;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;85;-6450.436,897.4099;Inherit;False;67;Distort;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;84;-6437.252,989.5948;Inherit;False;Property;_DistortType1;DistortType1;20;1;[Enum];Create;True;2;NotAffect_Disss;0;Affect_Disss;1;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;132;-6497.566,667.1341;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;83;-6467.535,762.5092;Inherit;False;82;dissctrl;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;47;-5012.658,215.8645;Inherit;False;custom_z;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;107;-6214.722,1168.597;Inherit;False;Property;_DissScaleControl;DissScaleControl;16;1;[Enum];Create;True;2;ValueCtrl;0;particleCtrl;1;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;94;-6215.374,1306.914;Inherit;False;Property;_DissScale;DissScale(Z ctrl);12;0;Create;False;0;0;False;0;0.6175;0.6175;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;103;-6010.897,1230.875;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;92;-6055.207,1077.881;Inherit;False;47;custom_z;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;86;-6212.436,878.4099;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;81;-6295.392,711.4761;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;78;-6422.58,523.8233;Inherit;False;0;15;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;156;-5437.85,-1526.064;Inherit;False;Property;_Diss_SpeedX;Diss_SpeedX;17;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;104;-5826.897,1243.875;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;102;-5828.01,1097.336;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;90;-5907.78,910.8968;Inherit;False;Constant;_Float2;Float 2;17;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;79;-5996.563,654.429;Inherit;False;3;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;155;-5436.85,-1445.975;Inherit;False;Property;_Diss_SpeedY;Diss_SpeedY;18;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;157;-5454.101,-1620.081;Inherit;False;1;0;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;105;-5648.027,1126.931;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;159;-5214.993,-1598.028;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;158;-5213.832,-1483.118;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;89;-5752.081,844.3677;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DynamicAppendNode;160;-5042.048,-1550.439;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;48;-5014.083,324.6484;Inherit;False;custom_w;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;91;-5536.617,829.5848;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;7;-3936.435,162.9722;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;88;-5407.39,659.2918;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-4143.467,-38.45473;Inherit;False;Property;_Diss_Amount;Diss_Amount(W ctrl);13;0;Create;False;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;161;-4870.67,-1541.997;Inherit;False;Dissspeed;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;8;-4098.86,342.7779;Inherit;False;48;custom_w;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;93;-5255.676,859.9927;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;164;-5223.373,1060.284;Inherit;False;161;Dissspeed;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.VertexColorNode;197;-3815.815,415.9363;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-3751.435,120.9722;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;198;-3833.097,598.1374;Inherit;False;Property;_VertexCtrl_Diss;VertexCtrl_Diss;14;1;[Enum];Create;True;2;ValueCtrl;0;VertexAlpha;1;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-3886.435,293.9722;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;13;-3595.885,188.9722;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;200;-3611.137,427.5217;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;199;-3631.032,596.2017;Inherit;False;alphactrl;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;162;-4971.415,977.7779;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;201;-3376.535,215.5825;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;141;-5439.029,-1935.262;Inherit;False;1;0;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;15;-4635.193,827.5356;Inherit;True;Property;_DissolveTex;DissolveTex(XY ctrl offset);10;0;Create;False;0;0;False;0;-1;None;4ada5e4d36281c74f915ac1e764803b3;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;143;-5421.778,-1761.156;Inherit;False;Property;_MT_SpeedY;MT_SpeedY;8;0;Create;True;0;0;False;0;0;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;142;-5422.778,-1841.245;Inherit;False;Property;_MT_SpeedX;MT_SpeedX;7;0;Create;True;0;0;False;0;0;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;145;-5199.921,-1913.209;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;144;-5198.76,-1798.299;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;151;-2589.052,376.9514;Inherit;False;67;Distort;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;150;-2598.331,475.5398;Inherit;False;Property;_DistortType3;DistortType3;22;1;[Enum];Create;True;2;NotAffect_Mask;0;Affect_Mask;1;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DesaturateOpNode;20;-4328.788,828.5446;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;14;-3161.092,232.2802;Inherit;False;dissamount;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;19;-4212.491,1207.937;Inherit;False;14;dissamount;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;175;-1020.127,-850.9545;Inherit;False;Property;_Fresnel_Power;Fresnel_Power;34;0;Create;True;0;0;False;0;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;75;-4115.454,873.0577;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-4153.515,1406.923;Inherit;False;Property;_Diss_Ramp;Diss_Ramp;11;0;Create;True;0;0;False;0;0.1;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;176;-1016.127,-934.9545;Inherit;False;Property;_Fresnel_Scale;Fresnel_Scale;33;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-4154.443,721.1756;Inherit;False;Property;_DissolveInvert;DissolveInvert;15;1;[Enum];Create;True;2;Normal;0;Invert;1;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;140;-2134.306,-371.7118;Inherit;False;135;pannertype;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;152;-2383.731,239.3334;Inherit;False;0;24;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;146;-5026.976,-1865.62;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;153;-2407.63,424.1523;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-3925.532,1373.537;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;73;-1882.777,-671.8083;Inherit;False;67;Distort;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;23;-3888.814,839.4306;Inherit;False;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;22;-3922.629,1179.717;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;177;-612.9545,-1263.025;Inherit;False;Property;_FresnelColor_Out;FresnelColor_Out;32;0;Create;True;0;0;False;0;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FresnelNode;178;-674.6126,-1041.864;Inherit;True;Standard;WorldNormal;ViewDir;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;149;-1908.192,-274.9389;Inherit;False;82;dissctrl;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;137;-1885.286,-367.3535;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;138;-1920.691,-467.9258;Inherit;False;76;custom_uv;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;179;-622.7556,-1439.732;Inherit;False;Property;_FresnelColor_In;FresnelColor_In;31;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;147;-4855.598,-1857.178;Inherit;False;MTspeed;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;154;-2138.916,258.0477;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;95;-1890.24,-567.2379;Inherit;False;Property;_DistortType2;DistortType2;21;1;[Enum];Create;True;2;NotAffect_MT;0;Affect_MT;1;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;28;-3740.16,1343.852;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;25;-3696.157,1591.474;Inherit;False;Constant;_Float0;Float 0;6;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;24;-1565.005,229.7892;Inherit;True;Property;_MaskTex;MaskTex;28;0;Create;True;0;0;False;0;-1;None;c38cafc9672ea9e4fbfe18364982d88e;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;181;-199.1421,-1197.13;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;26;-3706.814,896.6307;Inherit;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;180;-335.2386,-729.9623;Inherit;False;Property;_Fresnel_Intensity;Fresnel_Intensity;35;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;29;-3747.588,1187.891;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;148;-1783.786,-199.2318;Inherit;False;147;MTspeed;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;69;-1903.597,-831.446;Inherit;False;0;40;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;30;-3698.886,1504.139;Inherit;False;Constant;_Float1;Float 1;6;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;96;-1701.356,-624.6073;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;139;-1654.511,-409.5401;Inherit;False;3;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;74;-1400.438,-497.3654;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TFHCRemapNode;33;-3238.433,1181.624;Inherit;False;5;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;1,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;1,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DesaturateOpNode;41;-1248.442,199.7032;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;182;157.8021,-958.4094;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;165;-1010.475,205.3305;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;40;-761.2735,-88.0443;Inherit;True;Property;_MainTex;MainTex;5;0;Create;True;0;0;False;0;-1;None;1816484866b17ab44858c171d9c13229;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;173;-448.5255,-5.649221;Inherit;False;Property;_MT_Desaturate;MT_Desaturate;3;1;[Enum];Create;True;2;Color;0;Gray;1;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;184;178.9626,-755.4578;Inherit;False;Property;_FresnelSwitch;FresnelSwitch;30;1;[Enum];Create;True;2;Fresnel_off;0;Fresnel_on;1;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;34;-3045.657,1181.67;Inherit;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;183;335.7946,-953.4575;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;43;-647.1816,407.0011;Inherit;False;Property;_MainColor;MainColor;4;0;Create;True;0;0;False;0;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;37;-2874.126,1176.053;Inherit;False;diss;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;196;542.861,-913.5093;Inherit;False;3;0;COLOR;1,1,1,0;False;1;COLOR;1,1,1,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;42;-597.1465,702.1254;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DesaturateOpNode;174;-201.7439,-80.50603;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;166;-854.5306,161.2176;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;167;-1041.732,348.5031;Inherit;False;Property;_MaskInvert;MaskInvert;29;1;[Enum];Create;True;2;Normal;0;Invert;1;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;202;-587.2336,893.2352;Inherit;False;199;alphactrl;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;39;-569.0296,987.3463;Inherit;False;37;diss;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;168;-662.0845,178.2814;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;186;751.2301,-923.4349;Inherit;True;Fresnel;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;194;-1.730675,-18.34272;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;203;-326.4556,673.6484;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;44;-579.4216,601.9767;Inherit;False;Property;_Intensity;Intensity;6;0;Create;True;0;0;False;0;1;175;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;188;-41.54359,-272.5369;Inherit;False;186;Fresnel;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;367.9402,27.99248;Inherit;False;9;9;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;COLOR;0,0,0,0;False;5;FLOAT;0;False;6;COLOR;0,0,0,0;False;7;FLOAT;0;False;8;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;195;613.8322,17.38718;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;46;817.538,63.54868;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;209;-142.2291,1441.648;Inherit;False;Property;_StencilWriteMask;Stencil Write Mask;37;1;[HideInInspector];Create;False;0;0;True;0;255;255;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;210;-144.2291,1522.648;Inherit;False;Property;_StencilReadMask;Stencil Read Mask;40;1;[HideInInspector];Create;False;0;0;True;0;255;255;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;205;-745.5876,619.1149;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;207;-153.2291,1274.648;Inherit;False;Property;_Stencil;Stencil ID;38;1;[HideInInspector];Create;False;0;0;True;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;206;-157.2626,1181.635;Inherit;False;Property;_StencilComp;Stencil Comparison;36;1;[HideInInspector];Create;False;0;0;True;0;8;8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;211;-137.2291,1611.648;Inherit;False;Property;_ColorMask;Color Mask;41;1;[HideInInspector];Create;False;0;0;True;0;15;15;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;169;-821.5735,-546.5009;Inherit;False;Property;_CullMode;Cull Mode;0;1;[Enum];Create;True;3;Option1;0;Option2;1;Option3;2;1;UnityEngine.Rendering.CullMode;True;0;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;208;-147.2291,1359.648;Inherit;False;Property;_StencilOp;Stencil Operation;39;1;[HideInInspector];Create;False;0;0;True;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;219;952.3235,57.84888;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;221;-130.6577,1717.388;Inherit;False;Property;_ZWirteMode;ZWirte Mode;1;1;[Enum];Create;True;2;Off;0;On;1;0;True;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;204;-1002.365,853.7012;Inherit;False;199;alphactrl;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;218;1193.33,29.48978;Float;False;True;-1;2;ASEMaterialInspector;100;1;G/G_dissolve_add_norotate;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;4;1;False;-1;1;False;-1;0;1;False;-1;1;False;-1;True;0;False;-1;0;False;-1;True;False;True;0;True;169;True;True;True;True;True;0;False;-1;True;True;255;True;207;255;True;210;255;True;209;7;True;206;1;True;208;0;False;-1;0;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;True;221;True;0;False;-1;True;True;0;False;-1;0;False;-1;True;2;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;2;0;False;False;False;False;False;False;False;False;False;True;1;LightMode=ForwardBase;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;1;True;False;;0
WireConnection;56;0;54;0
WireConnection;56;1;55;0
WireConnection;57;0;54;0
WireConnection;57;1;53;0
WireConnection;59;0;56;0
WireConnection;59;1;57;0
WireConnection;60;0;58;0
WireConnection;60;1;59;0
WireConnection;62;1;60;0
WireConnection;108;0;62;0
WireConnection;127;0;117;0
WireConnection;127;1;119;0
WireConnection;129;0;108;0
WireConnection;129;1;62;4
WireConnection;128;0;122;0
WireConnection;116;0;117;0
WireConnection;116;1;119;0
WireConnection;65;0;129;0
WireConnection;65;1;115;0
WireConnection;65;2;61;0
WireConnection;123;0;127;0
WireConnection;123;1;116;0
WireConnection;123;2;128;0
WireConnection;64;0;129;0
WireConnection;64;1;115;0
WireConnection;64;2;63;0
WireConnection;77;0;1;1
WireConnection;77;1;1;2
WireConnection;66;0;65;0
WireConnection;66;1;64;0
WireConnection;131;0;129;0
WireConnection;131;1;123;0
WireConnection;76;0;77;0
WireConnection;130;0;66;0
WireConnection;130;1;131;0
WireConnection;67;0;130;0
WireConnection;135;0;134;0
WireConnection;82;0;4;0
WireConnection;132;0;71;0
WireConnection;132;1;135;0
WireConnection;47;0;1;3
WireConnection;103;0;107;0
WireConnection;86;0;85;0
WireConnection;86;1;84;0
WireConnection;81;0;132;0
WireConnection;81;1;83;0
WireConnection;104;0;103;0
WireConnection;104;1;94;0
WireConnection;102;0;92;0
WireConnection;102;1;107;0
WireConnection;79;0;78;0
WireConnection;79;1;81;0
WireConnection;79;2;86;0
WireConnection;105;0;102;0
WireConnection;105;1;104;0
WireConnection;159;0;157;0
WireConnection;159;1;156;0
WireConnection;158;0;157;0
WireConnection;158;1;155;0
WireConnection;89;0;79;0
WireConnection;89;1;90;0
WireConnection;160;0;159;0
WireConnection;160;1;158;0
WireConnection;48;0;1;4
WireConnection;91;0;89;0
WireConnection;91;1;105;0
WireConnection;7;0;82;0
WireConnection;88;0;79;0
WireConnection;88;1;91;0
WireConnection;161;0;160;0
WireConnection;93;0;88;0
WireConnection;93;1;90;0
WireConnection;93;2;105;0
WireConnection;10;0;6;0
WireConnection;10;1;7;0
WireConnection;11;0;82;0
WireConnection;11;1;8;0
WireConnection;13;0;10;0
WireConnection;13;1;11;0
WireConnection;200;0;197;4
WireConnection;199;0;198;0
WireConnection;162;0;93;0
WireConnection;162;1;164;0
WireConnection;201;0;13;0
WireConnection;201;1;200;0
WireConnection;201;2;199;0
WireConnection;15;1;162;0
WireConnection;145;0;141;0
WireConnection;145;1;142;0
WireConnection;144;0;141;0
WireConnection;144;1;143;0
WireConnection;20;0;15;0
WireConnection;14;0;201;0
WireConnection;75;0;20;0
WireConnection;75;1;15;4
WireConnection;146;0;145;0
WireConnection;146;1;144;0
WireConnection;153;0;151;0
WireConnection;153;1;150;0
WireConnection;21;0;19;0
WireConnection;21;1;17;0
WireConnection;23;0;18;0
WireConnection;23;1;75;0
WireConnection;22;0;19;0
WireConnection;22;1;17;0
WireConnection;178;2;176;0
WireConnection;178;3;175;0
WireConnection;137;0;140;0
WireConnection;147;0;146;0
WireConnection;154;0;152;0
WireConnection;154;1;153;0
WireConnection;28;0;19;0
WireConnection;28;1;21;0
WireConnection;24;1;154;0
WireConnection;181;0;179;0
WireConnection;181;1;177;0
WireConnection;181;2;178;0
WireConnection;26;0;23;0
WireConnection;29;0;22;0
WireConnection;29;1;21;0
WireConnection;96;0;73;0
WireConnection;96;1;95;0
WireConnection;139;0;138;0
WireConnection;139;1;137;0
WireConnection;139;2;149;0
WireConnection;74;0;69;0
WireConnection;74;1;96;0
WireConnection;74;2;139;0
WireConnection;74;3;148;0
WireConnection;33;0;26;0
WireConnection;33;1;29;0
WireConnection;33;2;28;0
WireConnection;33;3;30;0
WireConnection;33;4;25;0
WireConnection;41;0;24;0
WireConnection;182;0;181;0
WireConnection;182;1;180;0
WireConnection;165;0;41;0
WireConnection;165;1;24;4
WireConnection;40;1;74;0
WireConnection;34;0;33;0
WireConnection;183;0;182;0
WireConnection;37;0;34;0
WireConnection;196;1;183;0
WireConnection;196;2;184;0
WireConnection;174;0;40;0
WireConnection;174;1;173;0
WireConnection;166;0;165;0
WireConnection;168;0;165;0
WireConnection;168;1;166;0
WireConnection;168;2;167;0
WireConnection;186;0;196;0
WireConnection;194;0;174;0
WireConnection;194;1;43;0
WireConnection;203;0;42;4
WireConnection;203;2;202;0
WireConnection;45;0;194;0
WireConnection;45;1;40;4
WireConnection;45;2;168;0
WireConnection;45;3;24;4
WireConnection;45;4;43;0
WireConnection;45;5;44;0
WireConnection;45;6;42;0
WireConnection;45;7;203;0
WireConnection;45;8;39;0
WireConnection;195;0;188;0
WireConnection;195;1;45;0
WireConnection;46;0;195;0
WireConnection;205;2;204;0
WireConnection;219;0;46;0
WireConnection;218;0;219;0
ASEEND*/
//CHKSM=D94A276F54B424F494AA3754252F95685FAFDDA5