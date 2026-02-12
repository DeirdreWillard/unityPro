// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:2,bsrc:0,bdst:0,dpts:2,wrdp:False,dith:0,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:True,fnsp:True,fnfb:True,fsmp:False;n:type:ShaderForge.SFN_Final,id:3138,x:32719,y:32712,varname:node_3138,prsc:2|emission-3348-OUT;n:type:ShaderForge.SFN_Color,id:7241,x:32053,y:32571,ptovrint:False,ptlb:MainColor,ptin:_MainColor,varname:node_7241,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:1,c3:1,c4:1;n:type:ShaderForge.SFN_Tex2d,id:1191,x:31898,y:32724,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:node_1191,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-7629-UVOUT;n:type:ShaderForge.SFN_Multiply,id:3348,x:32519,y:32746,varname:node_3348,prsc:2|A-7241-RGB,B-2007-OUT,C-4669-OUT,D-8433-OUT,E-56-OUT;n:type:ShaderForge.SFN_ScreenPos,id:7629,x:31497,y:32713,varname:node_7629,prsc:2,sctp:2;n:type:ShaderForge.SFN_Tex2d,id:7649,x:31872,y:32978,ptovrint:False,ptlb:MaskTex,ptin:_MaskTex,varname:_MainTex_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-5073-UVOUT;n:type:ShaderForge.SFN_Desaturate,id:4669,x:32221,y:32964,varname:node_4669,prsc:2|COL-9865-OUT;n:type:ShaderForge.SFN_ScreenPos,id:5073,x:31497,y:32958,varname:node_5073,prsc:2,sctp:2;n:type:ShaderForge.SFN_Multiply,id:2007,x:32078,y:32744,varname:node_2007,prsc:2|A-1191-RGB,B-1191-A;n:type:ShaderForge.SFN_Multiply,id:9865,x:32049,y:32978,varname:node_9865,prsc:2|A-7649-RGB,B-7649-A;n:type:ShaderForge.SFN_ValueProperty,id:8433,x:32221,y:33149,ptovrint:False,ptlb:Intensity,ptin:_Intensity,varname:node_8433,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_VertexColor,id:1204,x:32052,y:33254,varname:node_1204,prsc:2;n:type:ShaderForge.SFN_Multiply,id:56,x:32250,y:33275,varname:node_56,prsc:2|A-1204-RGB,B-1204-A;proporder:7241-8433-1191-7649;pass:END;sub:END;*/

Shader "G/G_ScreenUV_add" {
    Properties {
        _MainColor ("MainColor", Color) = (1,1,1,1)
        _Intensity ("Intensity", Float ) = 1
        _MainTex ("MainTex", 2D) = "white" {}
        _MaskTex ("MaskTex", 2D) = "white" {}
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
            Cull Off
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            //#pragma multi_compile_fwdbase
            #pragma target 3.0
            uniform float4 _MainColor;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform sampler2D _MaskTex; uniform float4 _MaskTex_ST;
            uniform float _Intensity;
            struct VertexInput {
                float4 vertex : POSITION;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 vertexColor : COLOR;
                float4 projPos : TEXCOORD0;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.vertexColor = v.vertexColor;
                o.pos = UnityObjectToClipPos( v.vertex );
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
////// Lighting:
////// Emissive:
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(sceneUVs.rg, _MainTex));
                float4 _MaskTex_var = tex2D(_MaskTex,TRANSFORM_TEX(sceneUVs.rg, _MaskTex));
                float3 emissive = (_MainColor.rgb*(_MainTex_var.rgb*_MainTex_var.a)*dot((_MaskTex_var.rgb*_MaskTex_var.a),float3(0.3,0.59,0.11))*_Intensity*(i.vertexColor.rgb*i.vertexColor.a));
                float3 finalColor = emissive;
                return fixed4(finalColor,1);
            }
            ENDCG
        }
    }
    CustomEditor "ShaderForgeMaterialInspector"
}
