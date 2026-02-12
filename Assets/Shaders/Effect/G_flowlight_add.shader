// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:0,bdst:0,dpts:2,wrdp:False,dith:0,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:True,fnsp:True,fnfb:True,fsmp:False;n:type:ShaderForge.SFN_Final,id:3138,x:32719,y:32712,varname:node_3138,prsc:2|emission-4156-OUT;n:type:ShaderForge.SFN_Color,id:7241,x:31823,y:32668,ptovrint:False,ptlb:MainColor,ptin:_MainColor,varname:node_7241,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:1,c3:1,c4:1;n:type:ShaderForge.SFN_Tex2d,id:635,x:31173,y:32753,ptovrint:False,ptlb:Tex1,ptin:_Tex1,varname:node_635,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-4468-OUT;n:type:ShaderForge.SFN_Tex2d,id:71,x:31177,y:33063,ptovrint:False,ptlb:Tex2,ptin:_Tex2,varname:node_71,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-2772-OUT;n:type:ShaderForge.SFN_Tex2d,id:1926,x:31541,y:33266,ptovrint:False,ptlb:MaskTex,ptin:_MaskTex,varname:node_1926,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False;n:type:ShaderForge.SFN_TexCoord,id:9337,x:30294,y:32948,varname:node_9337,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Time,id:573,x:30063,y:32486,varname:node_573,prsc:2;n:type:ShaderForge.SFN_Multiply,id:8668,x:30300,y:32620,varname:node_8668,prsc:2|A-573-TSL,B-3146-OUT;n:type:ShaderForge.SFN_ValueProperty,id:3146,x:30063,y:32666,ptovrint:False,ptlb:Tex1_SpeedX,ptin:_Tex1_SpeedX,varname:node_3146,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Time,id:7022,x:30063,y:32719,varname:node_7022,prsc:2;n:type:ShaderForge.SFN_Multiply,id:339,x:30300,y:32775,varname:node_339,prsc:2|A-7022-TSL,B-755-OUT;n:type:ShaderForge.SFN_ValueProperty,id:755,x:30063,y:32899,ptovrint:False,ptlb:Tex1_SpeedY,ptin:_Tex1_SpeedY,varname:_Tex_SpeedX_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Append,id:8972,x:30495,y:32710,varname:node_8972,prsc:2|A-8668-OUT,B-339-OUT;n:type:ShaderForge.SFN_Add,id:4468,x:30781,y:32838,varname:node_4468,prsc:2|A-8972-OUT,B-9337-UVOUT,C-3244-OUT;n:type:ShaderForge.SFN_Time,id:2746,x:30061,y:32995,varname:node_2746,prsc:2;n:type:ShaderForge.SFN_Multiply,id:1725,x:30297,y:33129,varname:node_1725,prsc:2|A-2746-TSL,B-3381-OUT;n:type:ShaderForge.SFN_ValueProperty,id:3381,x:30061,y:33175,ptovrint:False,ptlb:Tex2_SpeedX,ptin:_Tex2_SpeedX,varname:_Tex1_SpeedX_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Time,id:8659,x:30060,y:33228,varname:node_8659,prsc:2;n:type:ShaderForge.SFN_Multiply,id:1017,x:30297,y:33284,varname:node_1017,prsc:2|A-8659-TSL,B-967-OUT;n:type:ShaderForge.SFN_ValueProperty,id:967,x:30060,y:33408,ptovrint:False,ptlb:Tex2_SpeedY,ptin:_Tex2_SpeedY,varname:_Tex1_SpeedY_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Append,id:5460,x:30492,y:33219,varname:node_5460,prsc:2|A-1725-OUT,B-1017-OUT;n:type:ShaderForge.SFN_Add,id:2772,x:30786,y:33167,varname:node_2772,prsc:2|A-9337-UVOUT,B-5460-OUT,C-3244-OUT;n:type:ShaderForge.SFN_Add,id:9010,x:31823,y:32923,varname:node_9010,prsc:2|A-8270-OUT,B-5266-OUT;n:type:ShaderForge.SFN_Multiply,id:4156,x:32341,y:32906,varname:node_4156,prsc:2|A-7241-RGB,B-5179-OUT,C-4266-OUT,D-3472-RGB,E-3472-A;n:type:ShaderForge.SFN_VertexColor,id:3472,x:31954,y:33305,varname:node_3472,prsc:2;n:type:ShaderForge.SFN_Power,id:8270,x:31526,y:32777,varname:node_8270,prsc:2|VAL-7665-OUT,EXP-3698-OUT;n:type:ShaderForge.SFN_ValueProperty,id:3698,x:31177,y:32963,ptovrint:False,ptlb:Tex1_Pow,ptin:_Tex1_Pow,varname:node_3698,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_ValueProperty,id:1046,x:31176,y:33256,ptovrint:False,ptlb:Tex2_Pow,ptin:_Tex2_Pow,varname:_Tex1_Pow_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_Power,id:5266,x:31527,y:33063,varname:node_5266,prsc:2|VAL-162-OUT,EXP-1046-OUT;n:type:ShaderForge.SFN_Multiply,id:5179,x:32024,y:32923,varname:node_5179,prsc:2|A-9010-OUT,B-2023-OUT;n:type:ShaderForge.SFN_ValueProperty,id:2023,x:31823,y:33065,ptovrint:False,ptlb:Intensity,ptin:_Intensity,varname:node_2023,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_Multiply,id:4266,x:31773,y:33223,varname:node_4266,prsc:2|A-1926-RGB,B-1926-A;n:type:ShaderForge.SFN_Tex2d,id:2778,x:29238,y:33072,ptovrint:False,ptlb:DistortTex(ScreenUV),ptin:_DistortTexScreenUV,varname:node_2778,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-9355-OUT;n:type:ShaderForge.SFN_Multiply,id:3244,x:29518,y:33191,varname:node_3244,prsc:2|A-2778-R,B-7876-OUT,C-6842-OUT;n:type:ShaderForge.SFN_ValueProperty,id:7876,x:29238,y:33286,ptovrint:False,ptlb:DistortAmount,ptin:_DistortAmount,varname:node_7876,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_ScreenPos,id:1665,x:28442,y:33075,varname:node_1665,prsc:2,sctp:2;n:type:ShaderForge.SFN_Vector1,id:6842,x:29238,y:33364,varname:node_6842,prsc:2,v1:0.01;n:type:ShaderForge.SFN_Multiply,id:7665,x:31354,y:32777,varname:node_7665,prsc:2|A-635-RGB,B-635-A;n:type:ShaderForge.SFN_Multiply,id:162,x:31360,y:33063,varname:node_162,prsc:2|A-71-RGB,B-71-A;n:type:ShaderForge.SFN_Time,id:12,x:28205,y:33195,varname:node_12,prsc:2;n:type:ShaderForge.SFN_Multiply,id:2689,x:28442,y:33329,varname:node_2689,prsc:2|A-12-TSL,B-5261-OUT;n:type:ShaderForge.SFN_Time,id:4223,x:28205,y:33428,varname:node_4223,prsc:2;n:type:ShaderForge.SFN_Multiply,id:3577,x:28442,y:33484,varname:node_3577,prsc:2|A-4223-TSL,B-9922-OUT;n:type:ShaderForge.SFN_Append,id:508,x:28637,y:33419,varname:node_508,prsc:2|A-2689-OUT,B-3577-OUT;n:type:ShaderForge.SFN_Add,id:9355,x:28891,y:33247,varname:node_9355,prsc:2|A-1665-UVOUT,B-508-OUT;n:type:ShaderForge.SFN_ValueProperty,id:5261,x:28205,y:33346,ptovrint:False,ptlb:Distort_SpeedX,ptin:_Distort_SpeedX,varname:node_5261,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_ValueProperty,id:9922,x:28205,y:33587,ptovrint:False,ptlb:Distort_SpeedY,ptin:_Distort_SpeedY,varname:_Distort_SpeedX_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;proporder:7241-3698-1046-2023-635-3146-755-71-3381-967-1926-2778-7876-5261-9922;pass:END;sub:END;*/

Shader "G/G_flowlight_add" {
    Properties {
        _MainColor ("MainColor", Color) = (1,1,1,1)
        _Tex1_Pow ("Tex1_Pow", Float ) = 1
        _Tex2_Pow ("Tex2_Pow", Float ) = 1
        _Intensity ("Intensity", Float ) = 1
        _Tex1 ("Tex1", 2D) = "white" {}
        _Tex1_SpeedX ("Tex1_SpeedX", Float ) = 0
        _Tex1_SpeedY ("Tex1_SpeedY", Float ) = 0
        _Tex2 ("Tex2", 2D) = "white" {}
        _Tex2_SpeedX ("Tex2_SpeedX", Float ) = 0
        _Tex2_SpeedY ("Tex2_SpeedY", Float ) = 0
        _MaskTex ("MaskTex", 2D) = "white" {}
        _DistortTexScreenUV ("DistortTex(ScreenUV)", 2D) = "white" {}
        _DistortAmount ("DistortAmount", Float ) = 0
        _Distort_SpeedX ("Distort_SpeedX", Float ) = 0
        _Distort_SpeedY ("Distort_SpeedY", Float ) = 0
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend One One
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            //#pragma multi_compile_fwdbase
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x 
            #pragma target 3.0
            uniform float4 _MainColor;
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
            uniform sampler2D _DistortTexScreenUV; uniform float4 _DistortTexScreenUV_ST;
            uniform float _DistortAmount;
            uniform float _Distort_SpeedX;
            uniform float _Distort_SpeedY;
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
                COMPUTE_EYEDEPTH(o.projPos.z);
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
////// Lighting:
////// Emissive:
                float4 node_573 = _Time;
                float4 node_7022 = _Time;
                float4 node_12 = _Time;
                float4 node_4223 = _Time;
                float2 node_9355 = (sceneUVs.rg+float2((node_12.r*_Distort_SpeedX),(node_4223.r*_Distort_SpeedY)));
                float4 _DistortTexScreenUV_var = tex2D(_DistortTexScreenUV,TRANSFORM_TEX(node_9355, _DistortTexScreenUV));
                float node_3244 = (_DistortTexScreenUV_var.r*_DistortAmount*0.01);
                float2 node_4468 = (float2((node_573.r*_Tex1_SpeedX),(node_7022.r*_Tex1_SpeedY))+i.uv0+node_3244);
                float4 _Tex1_var = tex2D(_Tex1,TRANSFORM_TEX(node_4468, _Tex1));
                float4 node_2746 = _Time;
                float4 node_8659 = _Time;
                float2 node_2772 = (i.uv0+float2((node_2746.r*_Tex2_SpeedX),(node_8659.r*_Tex2_SpeedY))+node_3244);
                float4 _Tex2_var = tex2D(_Tex2,TRANSFORM_TEX(node_2772, _Tex2));
                float4 _MaskTex_var = tex2D(_MaskTex,TRANSFORM_TEX(i.uv0, _MaskTex));
                float3 emissive = (_MainColor.rgb*((pow((_Tex1_var.rgb*_Tex1_var.a),_Tex1_Pow)+pow((_Tex2_var.rgb*_Tex2_var.a),_Tex2_Pow))*_Intensity)*(_MaskTex_var.rgb*_MaskTex_var.a)*i.vertexColor.rgb*i.vertexColor.a);
                float3 finalColor = emissive;
                return fixed4(finalColor,1);
            }
            ENDCG
        }
    }
    CustomEditor "ShaderForgeMaterialInspector"
}
