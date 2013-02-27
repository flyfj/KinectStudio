//------------------------------------------------------------------------------
// <copyright file="D3DVizualization.h" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

#pragma once

#include <windows.h>

// This file requires the installation of the DirectX SDK, a link for which is included in the Toolkit Browser
#include <d3d11.h>
#include <xnamath.h>

#include <NuiApi.h>

#include "OrbitCamera.h"
#include "DX11Utils.h"
#include "resource.h"

static const int		    		    _MaxPlayerIndices = 8;

/// <summary>
/// Constant buffer for shader
/// </summary>
struct CBChangesEveryFrame
{
    XMMATRIX ViewProjection;
    XMVECTOR PlayerDepthMinMax[_MaxPlayerIndices];

    XMFLOAT4 XYScale;
};

extern "C" {
    __declspec(dllexport) HRESULT __cdecl Init(BSTR);
}

extern "C" {
    __declspec(dllexport) void __cdecl Cleanup();
}

extern "C" {
    __declspec(dllexport) HRESULT __cdecl Render(void * pResource);
}

extern "C" {
    __declspec(dllexport) HRESULT __cdecl SetCameraRadius(float r);
}

extern "C" {
    __declspec(dllexport) HRESULT __cdecl SetCameraTheta(float theta);
}

extern "C" {
    __declspec(dllexport) HRESULT __cdecl SetCameraPhi(float phi);
}

HRESULT Init();

class CDepthD3D
{
    static const NUI_IMAGE_RESOLUTION   cDepthResolution = NUI_IMAGE_RESOLUTION_640x480;

public:
    /// <summary>
    /// Constructor
    /// </summary>
    CDepthD3D();

    /// <summary>
    /// Destructor
    /// </summary>
    ~CDepthD3D();

    /// <summary>
    /// Register class and create window
    /// </summary>
    /// <returns>S_OK for success, or failure code</returns>
    HRESULT                             InitWindow(HINSTANCE hInstance, int nCmdShow);

    /// <summary>
    /// Create Direct3D device and swap chain
    /// </summary>
    /// <returns>S_OK for success, or failure code</returns>
    HRESULT                             InitDevice();

    /// <summary>
    /// Create the connected Kinect passed in 
    /// </summary>
    /// <returns>S_OK on success, otherwise failure code</returns>
    HRESULT                             CreateConnected(BSTR kinectID);
 
    /// <summary>
    /// Renders a frame
    /// </summary>
    /// <returns>S_OK for success, or failure code</returns>
    HRESULT                             Render(void * pResource);

    /// <summary>
    /// Create buffers
    /// </summary>
    /// <returns>S_OK for success, or failure code</returns>
    HRESULT                             CreateBuffers(void);

    /// <summary>
    /// Destroy buffers
    /// </summary>
    /// <returns>S_OK for success, or failure code</returns>
    HRESULT                             DestroyBuffers(void);

	/// <summary>
	/// Method for retreiving the camera
	/// </summary>
	/// <returns>Pointer to the camera</returns>
	CCamera*									GetCamera();

	// Special function definitions to ensure alingment between c# and c++ 
    void* operator new(size_t size)
	{
		return _aligned_malloc(size, 16);
	}
 
	void operator delete(void *p)
	{
		_aligned_free(p);
	}

private:
    // 3d camera
    CCamera                             m_camera;

    HINSTANCE                           m_hInst;
    HWND                                m_hWnd;
    D3D_FEATURE_LEVEL                   m_featureLevel;
    ID3D11Device*                       m_pd3dDevice;
    ID3D11DeviceContext*                m_pImmediateContext;
    ID3D11Texture2D*                    m_pDepthStencil;
    ID3D11DepthStencilView*             m_pDepthStencilView;
    ID3D11InputLayout*                  m_pVertexLayout;
    ID3D11Buffer*                       m_pVertexBuffer;
    ID3D11Buffer*                       m_pCBChangesEveryFrame;
    XMMATRIX                            m_projection;

    ID3D11VertexShader*                 m_pVertexShader;
    ID3D11PixelShader*                  m_pPixelShader;
    ID3D11GeometryShader*               m_pGeometryShader;

    LONG                                m_depthWidth;
    LONG                                m_depthHeight;
    
    float                               m_xyScale;

    // Initial window resolution
    UINT                                 m_Width;
    UINT                                 m_Height;

    // Kinect
    INuiSensor*                         m_pNuiSensor;
    HANDLE                              m_hNextDepthFrameEvent;
    HANDLE                              m_pDepthStreamHandle;

    float						   	    m_minPlayerDepth[_MaxPlayerIndices];
    float								m_maxPlayerDepth[_MaxPlayerIndices];

    // for passing depth data as a texture
    ID3D11Texture2D*                    m_pDepthTexture2D;
    ID3D11ShaderResourceView*           m_pDepthTextureRV;

    bool                                m_bNearMode;

    // if the application is paused, for example in the minimized case
    bool                                m_bPaused;

    /// <summary>
    /// Toggles between near and default mode
    /// Does nothing on a non-Kinect for Windows device
    /// </summary>
    /// <returns>S_OK for success, or failure code</returns>
    HRESULT                             ToggleNearMode();

    /// <summary>
    /// Process depth data received from Kinect
    /// </summary>
    /// <returns>S_OK for success, or failure code</returns>
    HRESULT                             ProcessDepth();

    /// <summary>
    /// Compile and set layout for shaders
    /// </summary>
    /// <returns>S_OK for success, or failure code</returns>
    HRESULT                             LoadShaders();
};