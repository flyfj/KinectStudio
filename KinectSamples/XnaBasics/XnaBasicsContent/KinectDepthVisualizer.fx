//------------------------------------------------------------------------------
// <copyright file="KinectDepthVisualizer.fx" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

sampler sprite : register(s0);

//--------------------------------------------------------------------------------------
// Constants
//--------------------------------------------------------------------------------------
// use the minimum of near mode and standard
static const int MinDepthValue = 300 << 3;

// use the maximum of near mode and standard
static const int MaxDepthValue = 4000 << 3;

// RGBA color coefficients for each player index
// 0 being no player
static const float4 playerColorCoefficients[] = 
{
    float4(1.0,  1.0,  1.0,  1.0), 
    float4(1.0,  0.2,  0.2,  1.0),
    float4(0.2,  1.0,  0.2,  1.0),
    float4(0.2,  0.2,  1.0,  1.0),
    float4(1.0,  1.0,  0.2,  1.0),
    float4(1.0,  0.2,  1.0,  1.0),
    float4(0.2,  1.0,  1.0,  1.0)
};

//--------------------------------------------------------------------------------------
// Pixel Shader
// 
// Takes in a a 16 bit depth value packed into a BGRA4444 pixel
// Outputs color as a 4 component float
//--------------------------------------------------------------------------------------
float4 DepthToRGB(float2 texCoord : TEXCOORD0) : COLOR0
{
    // We can't easily sample non-normalized data,
    // Texture is a short packed stuffed into a BGRA4444 (4 bit per component normalized)
    // It's not really BGRA4444 which is why we have to reverse the normalization here
    float4 color = tex2D(sprite, texCoord);

    // convert from [0,1] to [0,15]
    color *= 15.01f;

    // unpack the individual components into a single int
    int4 iColor = (int4)color;
    int depthV = iColor.w * 4096 + iColor.x * 256 + iColor.y * 16 + iColor.z;

    // player mask is in the lower 8 bits
    int playerId = depthV % 8;

    // linearally interpolate across the depth range
    float gray = ((float)depthV - MinDepthValue) / ((float)MaxDepthValue - MinDepthValue);

    // colorize the gray based on player mask
    return float4(gray, gray, gray, 1.0f) * playerColorCoefficients[playerId];
}

technique KinectDepth
{
    pass KinectDepth
    {
        PixelShader = compile ps_2_0 DepthToRGB();
    }
}
