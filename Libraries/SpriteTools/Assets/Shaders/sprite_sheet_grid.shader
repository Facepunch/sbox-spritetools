
HEADER
{
	Description = "";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	VrForward();
	Depth(); 
	ToolsVis( S_MODE_TOOLS_VIS );
	ToolsWireframe( "vr_tools_wireframe.shader" );
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 1
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 0
	#endif
	
	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
	#define CUSTOM_MATERIAL_INPUTS
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;

		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v );
		i.vTintColor = extraShaderData.vTint;

		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );

		return FinalizeVertex( i );
	}
}

PS
{
	#include "common/pixel.hlsl"
	
	float4 g_vGridColor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 1.00, 1.00, 1.00 ); >;
	float4 g_vBackgroundColor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 0.00, 0.00, 0.00, 0.00 ); >;
	float g_flLineThickness < UiGroup( ",0/,0/0" ); Default1( 2 ); Range1( 0, 1 ); >;
	float2 g_vImageSize < UiGroup( ",0/,0/0" ); Default2( 512,512 ); Range2( 0,0, 1,1 ); >;
	float2 g_vFrameSize < UiGroup( ",0/,0/0" ); Default2( 32,32 ); Range2( 0,0, 1,1 ); >;
	float2 g_vSeparation < UiGroup( ",0/,0/0" ); Default2( 0,0 ); Range2( 0,0, 1,1 ); >;
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m = Material::Init();
		m.Albedo = float3( 1, 1, 1 );
		m.Normal = float3( 0, 0, 1 );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
		
		float4 l_0 = g_vGridColor;
		float4 l_1 = g_vBackgroundColor;
		float l_2 = g_flLineThickness;
		float2 l_3 = g_vImageSize;
		float2 l_4 = float2( l_2, l_2 ) / l_3;
		float2 l_5 = i.vTextureCoords.xy * float2( 1, 1 );
		float2 l_6 = g_vFrameSize;
		float2 l_7 = g_vSeparation;
		float2 l_8 = l_6 + l_7;
		float2 l_9 = l_5 / l_8;
		float2 l_10 = frac( l_9 );
		float2 l_11 = step( l_4, l_10 );
		float l_12 = l_11.x;
		float l_13 = l_11.y;
		float l_14 = max( l_12, l_13 );
		float4 l_15 = lerp( l_0, l_1, l_14 );
		float l_16 = l_15.w;
		
		m.Albedo = l_15.xyz;
		m.Opacity = l_16;
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		
		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );

		// Result node takes normal as tangent space, convert it to world space now
		m.Normal = TransformNormal( m.Normal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		// for some toolvis shit
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
        m.TextureCoords = i.vTextureCoords.xy;
		
		return ShadingModelStandard::Shade( i, m );
	}
}
