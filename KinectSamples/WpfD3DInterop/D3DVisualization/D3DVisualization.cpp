//------------------------------------------------------------------------------
// <copyright file="D3DVisualization.cpp" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

#include "D3DVisualization.h"

// Global Variables
CDepthD3D * pApplication;  // Application class

BOOL WINAPI DllMain(HINSTANCE hInstance,DWORD fwdReason, LPVOID lpvReserved) 
{
    return TRUE;
}

/// <summary>
/// Init global class instance
/// </summary>
extern HRESULT __cdecl Init(BSTR kinectID)
{
    pApplication = new CDepthD3D();

    HRESULT hr = S_OK;

    if ( FAILED( hr = pApplication->InitDevice() ) )
    {
        return hr;
    }

	if ( FAILED( hr = pApplication->CreateConnected(kinectID) ) )
    {
        return hr;
    }	

    return hr;
}

/// <summary>
/// Cleanup global class instance
/// </summary>
extern void __cdecl Cleanup()
{
    delete pApplication;
    pApplication = NULL;
}

/// <summary>
/// Render for global class instance
/// </summary>
extern HRESULT __cdecl Render(void * pResource)
{
    if ( NULL == pApplication )
	{
        return E_FAIL;
	}

    return pApplication->Render(pResource);
}

/// <summary>
/// Sets the R value of the camera from the depth center
/// R value represents the distance of the camera from the players
/// </summary>
extern HRESULT _cdecl SetCameraRadius(float r)
{
	if ( NULL == pApplication )
	{
		return E_FAIL;
	}

	pApplication->GetCamera()->SetRadius(r);
	return 0;
}

/// <summary>
/// Sets the Theta value of the camera from around the depth center
/// Theta represents the angle (in radians) of the camera around the 
/// center in the x-y plane (circling around players)
/// </summary>
extern HRESULT _cdecl SetCameraTheta(float theta)
{
	if ( NULL == pApplication )
	{
		return E_FAIL;
	}

	pApplication->GetCamera()->SetTheta(theta);
	return 0;
}

/// <summary>
/// Sets the Phi value of the camera
/// Phi represents angle (in radians) of the camera around the center 
/// in the y-z plane (over the top and below players)
/// </summary>
extern HRESULT _cdecl SetCameraPhi(float phi)
{
	if ( NULL == pApplication )
	{
		return E_FAIL;
	}

	pApplication->GetCamera()->SetPhi(phi);
	return 0;
}

/// <summary>
/// Constructor
/// </summary>
CDepthD3D::CDepthD3D()
{
    m_Width = 640;
    m_Height = 480;

    // get resolution as DWORDS, but store as LONGs to avoid casts later
    DWORD width = 0;
    DWORD height = 0;

    NuiImageResolutionToSize(cDepthResolution, width, height);
    m_depthWidth  = static_cast<LONG>(width);
    m_depthHeight = static_cast<LONG>(height);

    m_hInst = NULL;
    m_hWnd = NULL;
    m_featureLevel = D3D_FEATURE_LEVEL_11_0;
    m_pd3dDevice = NULL;
    m_pImmediateContext = NULL;
    m_pDepthStencil = NULL;
    m_pDepthStencilView = NULL;
    m_pVertexLayout = NULL;
    m_pVertexBuffer = NULL;
    m_pCBChangesEveryFrame = NULL;

    m_pVertexShader = NULL;
    m_pPixelShader = NULL;
    m_pGeometryShader = NULL;

    m_xyScale = 0.0f;

    m_pDepthTexture2D = NULL;
    m_pDepthTextureRV = NULL;

    m_pNuiSensor = NULL;
    m_hNextDepthFrameEvent = INVALID_HANDLE_VALUE;
    m_pDepthStreamHandle = INVALID_HANDLE_VALUE;

    m_bNearMode = false;

    m_bPaused = false;

    for (int i = 0; i < _MaxPlayerIndices; ++i)
    {
        m_minPlayerDepth[i] = NUI_IMAGE_DEPTH_MAXIMUM >> NUI_IMAGE_PLAYER_INDEX_SHIFT;
        m_maxPlayerDepth[i] = NUI_IMAGE_DEPTH_MINIMUM >> NUI_IMAGE_PLAYER_INDEX_SHIFT;
    }
}

/// <summary>
/// Destructor
/// </summary>
CDepthD3D::~CDepthD3D()
{
    if (NULL != m_pNuiSensor)
    {
        m_pNuiSensor->NuiShutdown();
        m_pNuiSensor->Release();
    }

    if (m_pImmediateContext) 
    {
        m_pImmediateContext->ClearState();
    }
    
    SAFE_RELEASE(m_pCBChangesEveryFrame);
    SAFE_RELEASE(m_pGeometryShader);
    SAFE_RELEASE(m_pPixelShader);
    SAFE_RELEASE(m_pVertexBuffer);
    SAFE_RELEASE(m_pVertexLayout);
    SAFE_RELEASE(m_pVertexShader);
    DestroyBuffers();
    SAFE_RELEASE(m_pDepthTexture2D);
    SAFE_RELEASE(m_pDepthTextureRV);
    SAFE_RELEASE(m_pImmediateContext);
    SAFE_RELEASE(m_pd3dDevice);

    CloseHandle(m_hNextDepthFrameEvent);
}

/// <summary>
/// Create the connected Kinect passed in 
/// </summary>
/// <returns>indicates success or failure</returns>
HRESULT CDepthD3D::CreateConnected(BSTR kinectID)
{
    INuiSensor * pNuiSensor;
    HRESULT hr;

    int iSensorCount = 0;
    hr = NuiGetSensorCount(&iSensorCount);
    if (FAILED(hr) ) { return hr; }

	int i;
    // Look at each Kinect sensor
    for (i = 0; i < iSensorCount; ++i)
    {
        // Create the sensor so we can check status, if we can't create it, move on to the next
        hr = NuiCreateSensorByIndex(i, &pNuiSensor);
        if (FAILED(hr))
        {
            continue;
        }
		if (wcscmp(pNuiSensor->NuiUniqueId(), kinectID) != 0)
		{
			pNuiSensor->Release();
			continue;
		}

        // Get the status of the sensor, and if connected, then we can initialize it
        hr = pNuiSensor->NuiStatus();
        if (S_OK == hr)
        {
            m_pNuiSensor = pNuiSensor;
            break;
        }

        // This sensor wasn't OK, so release it since we're not using it
        pNuiSensor->Release();
    }

    if (NULL == m_pNuiSensor)
    {
        return E_FAIL;
    }

    // Initialize the Kinect and specify that we'll be using depth
    hr = m_pNuiSensor->NuiInitialize(NUI_INITIALIZE_FLAG_USES_DEPTH_AND_PLAYER_INDEX); 
    if (FAILED(hr) ) { return hr; }

    // Create an event that will be signaled when depth data is available
    m_hNextDepthFrameEvent = CreateEvent(NULL, TRUE, FALSE, NULL);

    // Open a depth image stream to receive depth frames
    hr = m_pNuiSensor->NuiImageStreamOpen(
        NUI_IMAGE_TYPE_DEPTH_AND_PLAYER_INDEX,
        NUI_IMAGE_RESOLUTION_640x480,
        0,
        2,
        m_hNextDepthFrameEvent,
        &m_pDepthStreamHandle);
    if (FAILED(hr) ) { return hr; }

    // Start with near mode on
    ToggleNearMode();

    return hr;
}

/// <summary>
/// Toggles between near and default mode
/// Does nothing on a non-Kinect for Windows device
/// </summary>
/// <returns>S_OK for success, or failure code</returns>
HRESULT CDepthD3D::ToggleNearMode()
{
    HRESULT hr = E_FAIL;

    if ( m_pNuiSensor )
    {
        hr = m_pNuiSensor->NuiImageStreamSetImageFrameFlags(m_pDepthStreamHandle, m_bNearMode ? 0 : NUI_IMAGE_STREAM_FLAG_ENABLE_NEAR_MODE);

        if ( SUCCEEDED(hr) )
        {
            m_bNearMode = !m_bNearMode;
        }
    }

    return hr;
}

/// <summary>
/// Compile and set layout for shaders
/// </summary>
/// <returns>S_OK for success, or failure code</returns>
HRESULT CDepthD3D::LoadShaders()
{
    // Compile the geometry shader
    ID3D10Blob* pBlob = NULL;
    HRESULT hr = CompileShaderFromFile(L"D3DVisualization.fx", "GS", "gs_4_0", &pBlob);
    if ( FAILED(hr) ) { return hr; };

    // Create the geometry shader
    hr = m_pd3dDevice->CreateGeometryShader(pBlob->GetBufferPointer(), pBlob->GetBufferSize(), NULL, &m_pGeometryShader);
    SAFE_RELEASE(pBlob);
    if ( FAILED(hr) ) { return hr; }

    // Compile the pixel shader
    hr = CompileShaderFromFile(L"D3DVisualization.fx", "PS", "ps_4_0", &pBlob);
    if ( FAILED(hr) ) { return hr; }

    // Create the pixel shader
    hr = m_pd3dDevice->CreatePixelShader(pBlob->GetBufferPointer(), pBlob->GetBufferSize(), NULL, &m_pPixelShader);
    SAFE_RELEASE(pBlob);
    if ( FAILED(hr) ) { return hr; }

    // Compile the vertex shader
    hr = CompileShaderFromFile(L"D3DVisualization.fx", "VS", "vs_4_0", &pBlob);
    if ( FAILED(hr) ) { return hr; }

    // Create the vertex shader
    hr = m_pd3dDevice->CreateVertexShader(pBlob->GetBufferPointer(), pBlob->GetBufferSize(), NULL, &m_pVertexShader);
    if ( SUCCEEDED(hr) )
    {
        // Define the vertex input layout
        D3D11_INPUT_ELEMENT_DESC layout[] = { { "POSITION", 0, DXGI_FORMAT_R16_SINT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0 } };

        // Create the vertex input layout
        hr = m_pd3dDevice->CreateInputLayout(layout, ARRAYSIZE(layout), pBlob->GetBufferPointer(), pBlob->GetBufferSize(), &m_pVertexLayout);
    }

    SAFE_RELEASE(pBlob);
    if ( FAILED(hr) ) { return hr; }

    // Set the input vertex layout
    // In this case we don't actually use it for anything
    // All the work is done in the geometry shader, but we need something here
    // We only need to set this once since we have only one vertex format
    m_pImmediateContext->IASetInputLayout(m_pVertexLayout);

    return hr;
}

/// <summary>
/// Create Direct3D device and swap chain
/// </summary>
/// <returns>S_OK for success, or failure code</returns>
HRESULT CDepthD3D::InitDevice()
{
    HRESULT hr = S_OK;
    
    UINT createDeviceFlags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;

    // Likely won't be very performant in reference
    D3D_DRIVER_TYPE driverTypes[] =
    {
        D3D_DRIVER_TYPE_HARDWARE,
        D3D_DRIVER_TYPE_WARP,
        D3D_DRIVER_TYPE_REFERENCE,
    };
    UINT numDriverTypes = ARRAYSIZE(driverTypes);

    // DX10 or 11 devices are suitable
    D3D_FEATURE_LEVEL featureLevels[] =
    {
        D3D_FEATURE_LEVEL_11_0,
        D3D_FEATURE_LEVEL_10_1,
        D3D_FEATURE_LEVEL_10_0,
    };
    UINT numFeatureLevels = ARRAYSIZE(featureLevels);

    for (UINT driverTypeIndex = 0; driverTypeIndex < numDriverTypes; ++driverTypeIndex)
    {
        hr = D3D11CreateDevice(NULL, driverTypes[driverTypeIndex], NULL, createDeviceFlags, featureLevels, numFeatureLevels,
            D3D11_SDK_VERSION, &m_pd3dDevice, &m_featureLevel, &m_pImmediateContext);

        if ( SUCCEEDED(hr) )
        {
            break;
        }
    }

    if ( FAILED(hr) )
    {
        MessageBox(NULL, L"Could not create a Direct3D 10 or 11 device.", L"Error", MB_ICONHAND | MB_OK);
        return hr;
    }

    CreateBuffers();
  

    // Create depth texture
    D3D11_TEXTURE2D_DESC depthTexDesc = {0};
    depthTexDesc.Width = m_depthWidth;
    depthTexDesc.Height = m_depthHeight;
    depthTexDesc.MipLevels = 1;
    depthTexDesc.ArraySize = 1;
    depthTexDesc.Format = DXGI_FORMAT_R16_SINT;
    depthTexDesc.SampleDesc.Count = 1;
    depthTexDesc.SampleDesc.Quality = 0;
    depthTexDesc.Usage = D3D11_USAGE_DYNAMIC;
    depthTexDesc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
    depthTexDesc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
    depthTexDesc.MiscFlags = 0;

    hr = m_pd3dDevice->CreateTexture2D(&depthTexDesc, NULL, &m_pDepthTexture2D);
    if ( FAILED(hr) ) { return hr; }
    
    hr = m_pd3dDevice->CreateShaderResourceView(m_pDepthTexture2D, NULL, &m_pDepthTextureRV);
    if ( FAILED(hr) ) { return hr; }

    hr = LoadShaders();

    if ( FAILED(hr) )
    {
        MessageBox(NULL, L"Could not load shaders.", L"Error", MB_ICONHAND | MB_OK);
        return hr;
    }

    // Create the vertex buffer
    D3D11_BUFFER_DESC bd = {0};
    bd.Usage = D3D11_USAGE_DEFAULT;
    bd.ByteWidth = sizeof(short);
    bd.BindFlags = D3D11_BIND_VERTEX_BUFFER;
    bd.CPUAccessFlags = 0;

    hr = m_pd3dDevice->CreateBuffer(&bd, NULL, &m_pVertexBuffer);
    if ( FAILED(hr) ) { return hr; }

    // Set vertex buffer
    UINT stride = 0;
    UINT offset = 0;
    m_pImmediateContext->IASetVertexBuffers(0, 1, &m_pVertexBuffer, &stride, &offset);

    // Set primitive topology
    m_pImmediateContext->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_POINTLIST);
    
    // Create the constant buffers
    bd.Usage = D3D11_USAGE_DEFAULT;
    bd.ByteWidth = sizeof(CBChangesEveryFrame);
    bd.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
    bd.CPUAccessFlags = 0;
    hr = m_pd3dDevice->CreateBuffer(&bd, NULL, &m_pCBChangesEveryFrame);
    if ( FAILED(hr) ) { return hr; }
   
    // Calculate correct XY scaling factor so that our vertices are correctly placed in the world
    // This helps us to unproject from the Kinect's depth camera back to a 3d world
    // Since the Horizontal and Vertical FOVs are proportional with the sensor's resolution along those axes
    // We only need to do this for horizontal
    // I.e. tan(horizontalFOV)/depthWidth == tan(verticalFOV)/depthHeight
    // Essentially we're computing the vector that light comes in on for a given pixel on the depth camera
    // We can then scale our x&y depth position by this and the depth to get how far along that vector we are
    const float DegreesToRadians = 3.14159265359f / 180.0f;
    m_xyScale = tanf(NUI_CAMERA_DEPTH_NOMINAL_HORIZONTAL_FOV * DegreesToRadians * 0.5f) / (m_depthWidth * 0.5f);  

    // Set rasterizer state to disable backface culling
    D3D11_RASTERIZER_DESC rasterDesc;
    rasterDesc.FillMode = D3D11_FILL_SOLID;
    rasterDesc.CullMode = D3D11_CULL_NONE;
    rasterDesc.FrontCounterClockwise = true;
    rasterDesc.DepthBias = false;
    rasterDesc.DepthBiasClamp = 0;
    rasterDesc.SlopeScaledDepthBias = 0;
    rasterDesc.DepthClipEnable = true;
    rasterDesc.ScissorEnable = false;
    rasterDesc.MultisampleEnable = false;
    rasterDesc.AntialiasedLineEnable = false;
    
    ID3D11RasterizerState* pState = NULL;

    hr = m_pd3dDevice->CreateRasterizerState(&rasterDesc, &pState);
    if ( FAILED(hr) ) { return hr; }

    m_pImmediateContext->RSSetState(pState);

    SAFE_RELEASE(pState);

    return S_OK;
}

HRESULT CDepthD3D::CreateBuffers()
{
    HRESULT hr;

    // Create depth stencil texture
    D3D11_TEXTURE2D_DESC descDepth = {0};
    descDepth.Width = m_Width;
    descDepth.Height = m_Height;
    descDepth.MipLevels = 1;
    descDepth.ArraySize = 1;
    descDepth.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
    descDepth.SampleDesc.Count = 1;
    descDepth.SampleDesc.Quality = 0;
    descDepth.Usage = D3D11_USAGE_DEFAULT;
    descDepth.BindFlags = D3D11_BIND_DEPTH_STENCIL;
    descDepth.CPUAccessFlags = 0;
    descDepth.MiscFlags = 0;
    hr = m_pd3dDevice->CreateTexture2D(&descDepth, NULL, &m_pDepthStencil);
    if ( FAILED(hr) ) { return hr; }

    // Create the depth stencil view
    D3D11_DEPTH_STENCIL_VIEW_DESC descDSV;
    ZeroMemory( &descDSV, sizeof(descDSV) );
    descDSV.Format = descDepth.Format;
    descDSV.ViewDimension = D3D11_DSV_DIMENSION_TEXTURE2D;
    descDSV.Texture2D.MipSlice = 0;
    hr = m_pd3dDevice->CreateDepthStencilView(m_pDepthStencil, &descDSV, &m_pDepthStencilView);
    if ( FAILED(hr) ) { return hr; }

    // Setup the viewport
    D3D11_VIEWPORT vp;
    vp.Width = static_cast<FLOAT>(m_Width);
    vp.Height = static_cast<FLOAT>(m_Height);
    vp.MinDepth = 0.0f;
    vp.MaxDepth = 1.0f;
    vp.TopLeftX = 0;
    vp.TopLeftY = 0;
    m_pImmediateContext->RSSetViewports(1, &vp);

    // Initialize the projection matrix
    m_projection = XMMatrixPerspectiveFovLH(XM_PIDIV4, m_Width / static_cast<FLOAT>(m_Height), 0.1f, 100.f);

    return S_OK;
}

HRESULT CDepthD3D::DestroyBuffers()
{
    SAFE_RELEASE(m_pDepthStencil);
    SAFE_RELEASE(m_pDepthStencilView);

    return S_OK;
}

/// <summary>
/// Process depth data received from Kinect
/// </summary>
/// <returns>S_OK for success, or failure code</returns>
HRESULT CDepthD3D::ProcessDepth()
{
    NUI_IMAGE_FRAME imageFrame;

    HRESULT hr = m_pNuiSensor->NuiImageStreamGetNextFrame(m_pDepthStreamHandle, 0, &imageFrame);
    if ( FAILED(hr) ) { return hr; }
    
    NUI_LOCKED_RECT LockedRect;
    hr = imageFrame.pFrameTexture->LockRect(0, &LockedRect, NULL, 0);
    if ( FAILED(hr) ) { return hr; }

    short minPlayerDepthThisFrame[_MaxPlayerIndices];
    short maxPlayerDepthThisFrame[_MaxPlayerIndices];
   
    short minDepth = m_bNearMode ? NUI_IMAGE_DEPTH_MINIMUM_NEAR_MODE :NUI_IMAGE_DEPTH_MINIMUM;
    short maxDepth = m_bNearMode ? NUI_IMAGE_DEPTH_MAXIMUM_NEAR_MODE :NUI_IMAGE_DEPTH_MAXIMUM;

	// Prepair to find the actual min/max starting with an absurd value
    for (int player = 0; player < _MaxPlayerIndices; ++player)
    {
        minPlayerDepthThisFrame[player] = maxDepth;
        maxPlayerDepthThisFrame[player] = minDepth;
    }

    // copy to our d3d 11 depth texture
    D3D11_MAPPED_SUBRESOURCE msT;
    hr = m_pImmediateContext->Map(m_pDepthTexture2D, NULL, D3D11_MAP_WRITE_DISCARD, NULL, &msT);
    if ( FAILED(hr) ) { return hr; }

    memcpy(msT.pData, LockedRect.pBits, LockedRect.size);    
    m_pImmediateContext->Unmap(m_pDepthTexture2D, NULL);

    short * pBufferRun = (short*)LockedRect.pBits;
    short * pBufferRunEnd = pBufferRun + LockedRect.size/sizeof(short);

    while (pBufferRun != pBufferRunEnd)
    {
        short depth = *pBufferRun++;

        if (depth <= maxDepth && depth >= minDepth)
        {
            int player = depth & NUI_IMAGE_PLAYER_INDEX_MASK;

            if (depth < minPlayerDepthThisFrame[player])
            {
                minPlayerDepthThisFrame[player] = depth;
            }
            else if (depth > maxPlayerDepthThisFrame[player])
            {
                maxPlayerDepthThisFrame[player] = depth;
            }
        }
    }

    hr = imageFrame.pFrameTexture->UnlockRect(0);
    if ( FAILED(hr) ) { return hr; };

    hr = m_pNuiSensor->NuiImageStreamReleaseFrame(m_pDepthStreamHandle, &imageFrame);

    // perform temporal range smoothing
    // these min/max values get passed to the shader
    // so it can interpolate over the min and max rather than the full possible range
    // this makes detail easier to see
    for (int player = 0; player < _MaxPlayerIndices; ++player)
    {
        minPlayerDepthThisFrame[player] >>= NUI_IMAGE_PLAYER_INDEX_SHIFT;
        maxPlayerDepthThisFrame[player] >>= NUI_IMAGE_PLAYER_INDEX_SHIFT;

        const float _LastFrameMinMaxWeight = 9.f;
        const float _TotalMinMaxWeight = 10.f;

        m_minPlayerDepth[player] = (m_minPlayerDepth[player] * _LastFrameMinMaxWeight + minPlayerDepthThisFrame[player]) / _TotalMinMaxWeight;
        m_maxPlayerDepth[player] = (m_maxPlayerDepth[player] * _LastFrameMinMaxWeight + maxPlayerDepthThisFrame[player]) / _TotalMinMaxWeight;
    }
	
	float camera_min = maxDepth;
	float camera_max = minDepth;
	for (int player = 0; player <_MaxPlayerIndices; ++player)
	{
		if (camera_min > m_minPlayerDepth[player])
		{
			camera_min = m_minPlayerDepth[player];
		}
		
		if (camera_max < m_maxPlayerDepth[player])
		{
			camera_max = m_maxPlayerDepth[player];
		}

		// Set the center depth of the camera to half way between the camera min and max depth 
		// then scale such that the max depth is 2 units away
		m_camera.SetCenterDepth(2 * (camera_min + (camera_max - camera_min) / 2) / (camera_max - camera_min));
	}

    return hr;
}

/// <summary>
/// Renders a frame
/// </summary>
/// <returns>S_OK for success, or failure code</returns>
HRESULT CDepthD3D::Render(void * pResource)
{
    HRESULT hr = S_OK;

    if (m_bPaused)
    {
        return hr;
    }

    if ( WAIT_OBJECT_0 == WaitForSingleObject(m_hNextDepthFrameEvent, 0) )
    {
        ProcessDepth();
    }

    IUnknown *pUnk = (IUnknown*)pResource;

    IDXGIResource * pDXGIResource;
    hr = pUnk->QueryInterface(__uuidof(IDXGIResource), (void**)&pDXGIResource);
    if (FAILED(hr))
    {
        return hr;
    }

    HANDLE sharedHandle;
    hr = pDXGIResource->GetSharedHandle(&sharedHandle);
    if (FAILED(hr))
    {
        return hr;
    }

    pDXGIResource->Release();

    IUnknown * tempResource11;
    hr = m_pd3dDevice->OpenSharedResource(sharedHandle, __uuidof(ID3D11Resource), (void**)(&tempResource11)); 
    if (FAILED(hr))
    {
        return hr;
    }

    ID3D11Texture2D * pOutputResource;
    hr = tempResource11->QueryInterface(__uuidof(ID3D11Texture2D), (void**)(&pOutputResource)); 
    if (FAILED(hr))
    {
        return hr;
    }
    tempResource11->Release(); 

    D3D11_RENDER_TARGET_VIEW_DESC rtDesc;
    rtDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
    rtDesc.ViewDimension = D3D11_RTV_DIMENSION_TEXTURE2D;
    rtDesc.Texture2D.MipSlice = 0;

    ID3D11RenderTargetView* pRenderTargetView;
    hr = m_pd3dDevice->CreateRenderTargetView(pOutputResource, &rtDesc, &pRenderTargetView);
    if (FAILED(hr))
    {
        return hr;
    }

    D3D11_TEXTURE2D_DESC outputResourceDesc;
    pOutputResource->GetDesc(&outputResourceDesc);
    if ( outputResourceDesc.Width != m_Width || outputResourceDesc.Height != m_Height )
    {
        m_Width = outputResourceDesc.Width;
        m_Height = outputResourceDesc.Height;
        DestroyBuffers();
        CreateBuffers();
    }

    m_pImmediateContext->OMSetRenderTargets(1, &pRenderTargetView, m_pDepthStencilView);

    // Clear the back buffer
    static float ClearColor[4] = { 0.0f, 0.0f, 0.0f, 0.0f };
    m_pImmediateContext->ClearRenderTargetView(pRenderTargetView, ClearColor);

    // Clear the depth buffer to 1.0 (max depth)
    m_pImmediateContext->ClearDepthStencilView(m_pDepthStencilView, D3D11_CLEAR_DEPTH, 1.0f, 0);

    // Update the view matrix
    m_camera.Update();

    XMMATRIX viewProjection = XMMatrixMultiply(m_camera.View, m_projection);

    // Update variables that change once per frame
    CBChangesEveryFrame cb;
    cb.ViewProjection = XMMatrixTranspose(viewProjection);
    for (int i = 0; i < _MaxPlayerIndices; ++i)
    {
        // precalculate the denominator used by the shader and pass it in the z coordinate
        float depthRange = m_maxPlayerDepth[i] - m_minPlayerDepth[i];
        float log2DepthRange = log( m_maxPlayerDepth[i] - m_minPlayerDepth[i] ) / log(2.f);
        cb.PlayerDepthMinMax[i] = XMVectorSet(m_minPlayerDepth[i], m_maxPlayerDepth[i], depthRange, log2DepthRange);
    }

    cb.XYScale = XMFLOAT4(m_xyScale, -m_xyScale, 0.f, 0.f);
    m_pImmediateContext->UpdateSubresource(m_pCBChangesEveryFrame, 0, NULL, &cb, 0, 0);

    // Set up shaders
    m_pImmediateContext->VSSetShader(m_pVertexShader, NULL, 0);

    m_pImmediateContext->GSSetShader(m_pGeometryShader, NULL, 0);
    m_pImmediateContext->GSSetConstantBuffers(0, 1, &m_pCBChangesEveryFrame);
    m_pImmediateContext->GSSetShaderResources(0, 1, &m_pDepthTextureRV);

    m_pImmediateContext->PSSetShader(m_pPixelShader, NULL, 0);

    // Draw the scene
    m_pImmediateContext->Draw(m_depthWidth * m_depthHeight, 0);

    if ( NULL != pRenderTargetView )
    {
        pRenderTargetView->Release();
    }

    if ( NULL != pOutputResource )
    {
        pOutputResource->Release();
    }

    if ( NULL != m_pImmediateContext )
    {
        m_pImmediateContext->Flush();
    }

	return 0;
}

/// <summary>
/// Method for retreiving the camera
/// </summary>
/// <returns>Pointer to the camera</returns>
CCamera* CDepthD3D::GetCamera()
{
	return &m_camera;
}