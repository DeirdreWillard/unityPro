Shader "UI/GuideMask"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255		
		_ColorMask("Color Mask", Float) = 15
		//中心
		_Origin1("Circle",Vector) = (0,0,0,0)
		_Origin2("Circle",Vector) = (0,0,0,0)
		_Origin3("Circle",Vector) = (0,0,0,0)
		//裁剪方式 0圆形 1圆形
		_MaskType("Type",Float) = 0		
	}

		SubShader
	{
		Tags
	{
		"Queue" = "Transparent"
		"IgnoreProjector" = "True"
		"RenderType" = "Transparent"
		"PreviewType" = "Plane"
		"CanUseSpriteAtlas" = "True"
	}

		Stencil
	{
		Ref[_Stencil]
		Comp[_StencilComp]
		Pass[_StencilOp]
		ReadMask[_StencilReadMask]
		WriteMask[_StencilWriteMask]
	}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask[_ColorMask]

		Pass
	{
		Name "Default"
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 2.0

#include "UnityCG.cginc"
#include "UnityUI.cginc"



		struct appdata_t
	{
		float4 vertex : POSITION;
		float4 color : COLOR;
		float2 texcoord : TEXCOORD0;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct v2f
	{
		float4 vertex : SV_POSITION;
		fixed4 color : COLOR;
		float2 texcoord : TEXCOORD0;
		float4 worldPosition : TEXCOORD1;
		UNITY_VERTEX_OUTPUT_STEREO
	};

	fixed4 _Color;
	fixed4 _TextureSampleAdd;
	float4 _ClipRect;
	float4 _Origin1;
	float4 _Origin2;
	float4 _Origin3;
	float _MaskType;	

	v2f vert(appdata_t IN)
	{
		v2f OUT;
		UNITY_SETUP_INSTANCE_ID(IN);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
		OUT.worldPosition = IN.vertex;
		OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
		OUT.texcoord = IN.texcoord;
		OUT.color = IN.color * _Color;
		return OUT;
	}

	sampler2D _MainTex;

	fixed4 frag(v2f IN) : SV_Target
	{
		half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

		if (_MaskType == 0) {
			if (distance(IN.worldPosition.xy, _Origin1.xy) <= _Origin1.z)
			{
				color.a = 0;
			}			
		}
		else {
			//UnityGet2DClipping这个函数实现了判断2D空间中的一点是否在一个矩形区域中
			if (UnityGet2DClipping(IN.worldPosition.xy, _Origin1))
			{
				color.a = 0;
			}

			if(_Origin2.x != 0 && _Origin2.y != 0 && _Origin2.z != 0 && _Origin2.w != 0){
				if(UnityGet2DClipping(IN.worldPosition.xy, _Origin2)){
					color.a = 0;
				}
			}

			if(_Origin3.x != 0 && _Origin3.y != 0 && _Origin3.z != 0 && _Origin3.w != 0){
				if(UnityGet2DClipping(IN.worldPosition.xy, _Origin3)){
					color.a = 0;
				}
			}
		}

		return color;
	}
		ENDCG
	}
	}
}