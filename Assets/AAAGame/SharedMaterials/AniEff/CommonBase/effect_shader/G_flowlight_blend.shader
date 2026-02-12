// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:3,bdst:7,dpts:2,wrdp:False,dith:0,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:True,fnsp:True,fnfb:True,fsmp:False;n:type:ShaderForge.SFN_Final,id:3138,x:32808,y:32633,varname:node_3138,prsc:2|emission-8397-OUT,alpha-5766-OUT;n:type:ShaderForge.SFN_Color,id:7241,x:31841,y:33141,ptovrint:False,ptlb:TexColor,ptin:_TexColor,varname:node_7241,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:1,c3:1,c4:1;n:type:ShaderForge.SFN_Tex2d,id:635,x:30879,y:32755,ptovrint:False,ptlb:Tex1,ptin:_Tex1,varname:node_635,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:2,isnm:False|UVIN-4468-OUT;n:type:ShaderForge.SFN_Tex2d,id:71,x:30883,y:33065,ptovrint:False,ptlb:Tex2,ptin:_Tex2,varname:node_71,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:2,isnm:False|UVIN-2772-OUT;n:type:ShaderForge.SFN_Tex2d,id:1926,x:32099,y:33175,ptovrint:False,ptlb:MaskTex,ptin:_MaskTex,varname:node_1926,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Time,id:573,x:30030,y:32325,varname:node_573,prsc:2;n:type:ShaderForge.SFN_Multiply,id:8668,x:30267,y:32459,varname:node_8668,prsc:2|A-573-TSL,B-3146-OUT;n:type:ShaderForge.SFN_ValueProperty,id:3146,x:30030,y:32505,ptovrint:False,ptlb:Tex1_SpeedX,ptin:_Tex1_SpeedX,varname:node_3146,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Time,id:7022,x:30030,y:32558,varname:node_7022,prsc:2;n:type:ShaderForge.SFN_Multiply,id:339,x:30267,y:32614,varname:node_339,prsc:2|A-7022-TSL,B-755-OUT;n:type:ShaderForge.SFN_ValueProperty,id:755,x:30030,y:32738,ptovrint:False,ptlb:Tex1_SpeedY,ptin:_Tex1_SpeedY,varname:_Tex_SpeedX_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Append,id:8972,x:30462,y:32549,varname:node_8972,prsc:2|A-8668-OUT,B-339-OUT;n:type:ShaderForge.SFN_Add,id:4468,x:30680,y:32842,varname:node_4468,prsc:2|A-8972-OUT,B-3637-OUT,C-3244-OUT;n:type:ShaderForge.SFN_Time,id:2746,x:30017,y:33212,varname:node_2746,prsc:2;n:type:ShaderForge.SFN_Multiply,id:1725,x:30253,y:33346,varname:node_1725,prsc:2|A-2746-TSL,B-3381-OUT;n:type:ShaderForge.SFN_ValueProperty,id:3381,x:30017,y:33392,ptovrint:False,ptlb:Tex2_SpeedX,ptin:_Tex2_SpeedX,varname:_Tex1_SpeedX_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Time,id:8659,x:30016,y:33445,varname:node_8659,prsc:2;n:type:ShaderForge.SFN_Multiply,id:1017,x:30253,y:33501,varname:node_1017,prsc:2|A-8659-TSL,B-967-OUT;n:type:ShaderForge.SFN_ValueProperty,id:967,x:30016,y:33625,ptovrint:False,ptlb:Tex2_SpeedY,ptin:_Tex2_SpeedY,varname:_Tex1_SpeedY_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Append,id:5460,x:30448,y:33436,varname:node_5460,prsc:2|A-1725-OUT,B-1017-OUT;n:type:ShaderForge.SFN_Add,id:2772,x:30685,y:33171,varname:node_2772,prsc:2|A-5166-OUT,B-5460-OUT,C-3244-OUT;n:type:ShaderForge.SFN_Add,id:9010,x:31684,y:32906,varname:node_9010,prsc:2|A-8270-OUT,B-5266-OUT;n:type:ShaderForge.SFN_VertexColor,id:3472,x:32272,y:32863,varname:node_3472,prsc:2;n:type:ShaderForge.SFN_Power,id:8270,x:31522,y:32787,varname:node_8270,prsc:2|VAL-857-OUT,EXP-3698-OUT;n:type:ShaderForge.SFN_ValueProperty,id:3698,x:31366,y:32935,ptovrint:False,ptlb:Tex1_Pow,ptin:_Tex1_Pow,varname:node_3698,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_ValueProperty,id:1046,x:31379,y:33231,ptovrint:False,ptlb:Tex2_Pow,ptin:_Tex2_Pow,varname:_Tex1_Pow_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_Power,id:5266,x:31527,y:33063,varname:node_5266,prsc:2|VAL-3985-OUT,EXP-1046-OUT;n:type:ShaderForge.SFN_Multiply,id:5179,x:32061,y:32906,varname:node_5179,prsc:2|A-6380-OUT,B-2023-OUT,C-7241-RGB,D-312-RGB;n:type:ShaderForge.SFN_ValueProperty,id:2023,x:31841,y:33063,ptovrint:False,ptlb:Intensity,ptin:_Intensity,varname:node_2023,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_Tex2d,id:2778,x:29238,y:33072,ptovrint:False,ptlb:DistortTex,ptin:_DistortTex,varname:node_2778,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-9355-OUT;n:type:ShaderForge.SFN_Multiply,id:3244,x:29518,y:33191,varname:node_3244,prsc:2|A-2778-R,B-7876-OUT,C-6842-OUT;n:type:ShaderForge.SFN_ValueProperty,id:7876,x:29238,y:33286,ptovrint:False,ptlb:DistortAmount,ptin:_DistortAmount,varname:node_7876,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_ScreenPos,id:1665,x:28367,y:32634,varname:node_1665,prsc:2,sctp:2;n:type:ShaderForge.SFN_Vector1,id:6842,x:29238,y:33364,varname:node_6842,prsc:2,v1:0.01;n:type:ShaderForge.SFN_Multiply,id:7665,x:31060,y:32779,varname:node_7665,prsc:2|A-635-RGB,B-635-A;n:type:ShaderForge.SFN_Multiply,id:162,x:31066,y:33065,varname:node_162,prsc:2|A-71-RGB,B-71-A;n:type:ShaderForge.SFN_Time,id:12,x:28205,y:33195,varname:node_12,prsc:2;n:type:ShaderForge.SFN_Multiply,id:2689,x:28442,y:33329,varname:node_2689,prsc:2|A-12-TSL,B-5261-OUT;n:type:ShaderForge.SFN_Time,id:4223,x:28205,y:33428,varname:node_4223,prsc:2;n:type:ShaderForge.SFN_Multiply,id:3577,x:28442,y:33484,varname:node_3577,prsc:2|A-4223-TSL,B-9922-OUT;n:type:ShaderForge.SFN_Append,id:508,x:28637,y:33419,varname:node_508,prsc:2|A-2689-OUT,B-3577-OUT;n:type:ShaderForge.SFN_Add,id:9355,x:28891,y:33247,varname:node_9355,prsc:2|A-7556-OUT,B-508-OUT;n:type:ShaderForge.SFN_ValueProperty,id:5261,x:28205,y:33346,ptovrint:False,ptlb:Distort_SpeedX,ptin:_Distort_SpeedX,varname:node_5261,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_ValueProperty,id:9922,x:28205,y:33587,ptovrint:False,ptlb:Distort_SpeedY,ptin:_Distort_SpeedY,varname:_Distort_SpeedX_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Tex2d,id:9891,x:31851,y:32730,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:node_9891,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-5768-OUT;n:type:ShaderForge.SFN_Add,id:1267,x:32272,y:32736,varname:node_1267,prsc:2|A-9891-RGB,B-5179-OUT;n:type:ShaderForge.SFN_Tex2d,id:312,x:31841,y:33303,ptovrint:False,ptlb:FlowlightMask,ptin:_FlowlightMask,varname:node_312,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-8992-OUT;n:type:ShaderForge.SFN_Clamp01,id:6380,x:31841,y:32906,varname:node_6380,prsc:2|IN-9010-OUT;n:type:ShaderForge.SFN_Set,id:1764,x:28554,y:32634,varname:SUV,prsc:2|IN-1665-UVOUT;n:type:ShaderForge.SFN_Get,id:2383,x:28368,y:33146,varname:node_2383,prsc:2|IN-1764-OUT;n:type:ShaderForge.SFN_Get,id:5923,x:31427,y:33373,varname:node_5923,prsc:2|IN-1764-OUT;n:type:ShaderForge.SFN_Get,id:4447,x:30087,y:33024,varname:node_4447,prsc:2|IN-1764-OUT;n:type:ShaderForge.SFN_SwitchProperty,id:3637,x:30400,y:32911,ptovrint:False,ptlb:Tex1_SUV,ptin:_Tex1_SUV,varname:node_3637,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False|A-1793-OUT,B-4447-OUT;n:type:ShaderForge.SFN_SwitchProperty,id:5166,x:30400,y:33133,ptovrint:False,ptlb:Tex2_SUV,ptin:_Tex2_SUV,varname:node_5166,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False|A-1793-OUT,B-4447-OUT;n:type:ShaderForge.SFN_SwitchProperty,id:7556,x:28573,y:33097,ptovrint:False,ptlb:DistortTex_SUV,ptin:_DistortTex_SUV,varname:node_7556,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False|A-7856-OUT,B-2383-OUT;n:type:ShaderForge.SFN_TexCoord,id:9353,x:28367,y:32791,varname:node_9353,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Set,id:1059,x:28554,y:32791,varname:UV,prsc:2|IN-9353-UVOUT;n:type:ShaderForge.SFN_Get,id:7856,x:28367,y:33068,varname:node_7856,prsc:2|IN-1059-OUT;n:type:ShaderForge.SFN_Get,id:1793,x:30087,y:32946,varname:node_1793,prsc:2|IN-1059-OUT;n:type:ShaderForge.SFN_Get,id:9028,x:31427,y:33303,varname:node_9028,prsc:2|IN-1059-OUT;n:type:ShaderForge.SFN_SwitchProperty,id:8992,x:31637,y:33303,ptovrint:False,ptlb:FlowlightMask_SUV,ptin:_FlowlightMask_SUV,varname:node_8992,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False|A-9028-OUT,B-5923-OUT;n:type:ShaderForge.SFN_Multiply,id:5766,x:32529,y:33012,varname:node_5766,prsc:2|A-9891-A,B-3035-OUT,C-1926-A,D-3472-A;n:type:ShaderForge.SFN_Desaturate,id:3035,x:32293,y:33033,varname:node_3035,prsc:2|COL-1926-RGB;n:type:ShaderForge.SFN_Multiply,id:8397,x:32526,y:32736,varname:node_8397,prsc:2|A-1267-OUT,B-3472-RGB;n:type:ShaderForge.SFN_Add,id:857,x:31250,y:32788,varname:node_857,prsc:2|A-7665-OUT,B-2138-OUT;n:type:ShaderForge.SFN_Add,id:3985,x:31262,y:33065,varname:node_3985,prsc:2|A-162-OUT,B-2138-OUT;n:type:ShaderForge.SFN_Vector1,id:2138,x:31048,y:32950,varname:node_2138,prsc:2,v1:0.001;n:type:ShaderForge.SFN_Time,id:1143,x:30852,y:32110,varname:node_1143,prsc:2;n:type:ShaderForge.SFN_Multiply,id:7534,x:31084,y:32233,varname:node_7534,prsc:2|A-1143-TSL,B-2935-OUT;n:type:ShaderForge.SFN_ValueProperty,id:2935,x:30852,y:32267,ptovrint:False,ptlb:MT_SpeedX,ptin:_MT_SpeedX,varname:node_2935,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Time,id:3690,x:30851,y:32333,varname:node_3690,prsc:2;n:type:ShaderForge.SFN_Multiply,id:5844,x:31084,y:32456,varname:node_5844,prsc:2|A-3690-TSL,B-9996-OUT;n:type:ShaderForge.SFN_ValueProperty,id:9996,x:30851,y:32490,ptovrint:False,ptlb:MT_SpeedY,ptin:_MT_SpeedY,varname:_MT_SpeedX_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Append,id:827,x:31299,y:32315,varname:node_827,prsc:2|A-7534-OUT,B-5844-OUT;n:type:ShaderForge.SFN_Add,id:5768,x:31501,y:32222,varname:node_5768,prsc:2|A-6271-OUT,B-827-OUT;n:type:ShaderForge.SFN_Get,id:6271,x:31063,y:32026,varname:node_6271,prsc:2|IN-1059-OUT;proporder:9891-2935-9996-7241-3698-1046-2023-635-3637-3146-755-71-5166-3381-967-2778-7556-7876-5261-9922-312-8992-1926;pass:END;sub:END;*/

Shader "G/G_flowlight_blend" {
    Properties {
        _MainTex ("MainTex", 2D) = "white" {}
        [MaterialToggle] _Gray ("Gray", Float ) = 0
        _MT_GrayPow ("MT_Gray Pow", Float ) = 1
        _MT_SpeedX ("MT_SpeedX", Float ) = 0
        _MT_SpeedY ("MT_SpeedY", Float ) = 0
        _TexColor ("TexColor", Color) = (1,1,1,1)
        _Tex1_Pow ("Tex1_Pow", Float ) = 1
        _Tex2_Pow ("Tex2_Pow", Float ) = 1
        _Intensity ("Intensity", Float ) = 1
        _Tex1 ("Tex1", 2D) = "black" {}
        [MaterialToggle] _Tex1_SUV ("Tex1_SUV", Float) = 0
        _Tex1_SpeedX ("Tex1_SpeedX", Float) = 0
        _Tex1_SpeedY ("Tex1_SpeedY", Float) = 0
        _Tex2 ("Tex2", 2D) = "black" {}
        [MaterialToggle] _Tex2_SUV ("Tex2_SUV", Float) = 0
        _Tex2_SpeedX ("Tex2_SpeedX", Float) = 0
        _Tex2_SpeedY ("Tex2_SpeedY", Float) = 0
        _DistortTex ("DistortTex", 2D) = "white" {}
        [MaterialToggle] _DistortTex_SUV ("DistortTex_SUV", Float) = 0
        _DistortAmount ("DistortAmount", Float) = 0
        _Distort_SpeedX ("Distort_SpeedX", Float) = 0
        _Distort_SpeedY ("Distort_SpeedY", Float) = 0
        _FlowlightMask ("FlowlightMask", 2D) = "white" {}
        [MaterialToggle] _FlowlightMask_SUV ("FlowlightMask_SUV", Float) = 0
        _MaskTex ("MaskTex", 2D) = "white" {}
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5

        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil ("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask ("Color Mask", Float) = 15
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

Stencil
{
    Ref [_Stencil]
    Comp [_StencilComp]
    Pass [_StencilOp] 
    ReadMask [_StencilReadMask]
    WriteMask [_StencilWriteMask]
}


        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            //#pragma multi_compile_fwdbase
            #pragma target 2.0
            uniform float4 _TexColor;
            uniform sampler2D _Tex1; uniform float4 _Tex1_ST;
            uniform sampler2D _Tex2; uniform float4 _Tex2_ST;
            uniform sampler2D _MaskTex; uniform float4 _MaskTex_ST;
            uniform float _Tex1_SpeedX;
            uniform float _Tex1_SpeedY;
            uniform float _Tex2_SpeedX;
            uniform float _Tex2_SpeedY;
            uniform float _Tex1_Pow;
            uniform float _Tex2_Pow;
            uniform float _Intensity;
            uniform sampler2D _DistortTex; uniform float4 _DistortTex_ST;
            uniform float _DistortAmount;
            uniform float _Distort_SpeedX;
            uniform float _Distort_SpeedY;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform sampler2D _FlowlightMask; uniform float4 _FlowlightMask_ST;
            uniform fixed _Tex1_SUV;
            uniform fixed _Tex2_SUV;
            uniform fixed _DistortTex_SUV;
            uniform fixed _FlowlightMask_SUV;
            uniform float _MT_SpeedX;
            uniform float _MT_SpeedY;
            uniform float _MT_GrayPow;
            uniform fixed _Gray;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
                float4 projPos : TEXCOORD1;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.pos = UnityObjectToClipPos( v.vertex );
                o.projPos = ComputeScreenPos (o.pos);
                // 移除 COMPUTE_EYEDEPTH，部分移动设备不支持
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
////// Lighting:
////// Emissive:
                float2 UV = i.uv0;
                float4 node_1143 = _Time;
                float4 node_3690 = _Time;
                float2 node_5768 = (UV+float2((node_1143.r*_MT_SpeedX),(node_3690.r*_MT_SpeedY)));
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(node_5768, _MainTex));
                float4 node_573 = _Time;
                float4 node_7022 = _Time;
                float2 node_1793 = UV;
                float2 SUV = sceneUVs.rg;
                float2 node_4447 = SUV;
                float4 node_12 = _Time;
                float4 node_4223 = _Time;
                float2 node_9355 = (lerp( UV, SUV, _DistortTex_SUV )+float2((node_12.r*_Distort_SpeedX),(node_4223.r*_Distort_SpeedY)));
                float4 _DistortTex_var = tex2D(_DistortTex,TRANSFORM_TEX(node_9355, _DistortTex));
                float node_3244 = (_DistortTex_var.r*_DistortAmount*0.01);
                float2 node_4468 = (float2((node_573.r*_Tex1_SpeedX),(node_7022.r*_Tex1_SpeedY))+lerp( node_1793, node_4447, _Tex1_SUV )+node_3244);
                float4 _Tex1_var = tex2D(_Tex1,TRANSFORM_TEX(node_4468, _Tex1));
                float node_2138 = 0.001;
                float4 node_2746 = _Time;
                float4 node_8659 = _Time;
                float2 node_2772 = (lerp( node_1793, node_4447, _Tex2_SUV )+float2((node_2746.r*_Tex2_SpeedX),(node_8659.r*_Tex2_SpeedY))+node_3244);
                float4 _Tex2_var = tex2D(_Tex2,TRANSFORM_TEX(node_2772, _Tex2));
                float2 _FlowlightMask_SUV_var = lerp( UV, SUV, _FlowlightMask_SUV );
                float4 _FlowlightMask_var = tex2D(_FlowlightMask,TRANSFORM_TEX(_FlowlightMask_SUV_var, _FlowlightMask));
                float3 emissive = ((_MainTex_var.rgb+(saturate((pow(((_Tex1_var.rgb*_Tex1_var.a)+node_2138),_Tex1_Pow)+pow(((_Tex2_var.rgb*_Tex2_var.a)+node_2138),_Tex2_Pow)))*_Intensity*_TexColor.rgb*_FlowlightMask_var.rgb*saturate(lerp( 1.0, (_MainTex_var.b*pow(dot(_MainTex_var.rgb,float3(0.3,0.59,0.11)),_MT_GrayPow)), _Gray ))))*i.vertexColor.rgb);
                float3 finalColor = emissive;
                float4 _MaskTex_var = tex2D(_MaskTex,TRANSFORM_TEX(i.uv0, _MaskTex));
                return fixed4(finalColor,(_MainTex_var.a*dot(_MaskTex_var.rgb,float3(0.3,0.59,0.11))*_MaskTex_var.a*i.vertexColor.a));
            }
            ENDCG
        }
    }
    CustomEditor "ShaderForgeMaterialInspector"
}
