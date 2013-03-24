//-----------------------------------------------------------------------------
// <copyright file="MainWindow.cpp" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------------

#include "MainWindow.h"

using namespace cv;
using namespace Microsoft::KinectBridge;
using namespace std;

// Entry point for the application
int APIENTRY _tWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPTSTR lpCmdLine, int nCmdShow)
{
    UNREFERENCED_PARAMETER(hPrevInstance);
    UNREFERENCED_PARAMETER(lpCmdLine);

    CMainWindow application;
    return application.Run(hInstance, nCmdShow);
}

/// <summary>
/// Constructor
/// </summary>
CMainWindow::CMainWindow() :
    m_hInstance(NULL),
    m_hdc(NULL),
    m_hWndMain(NULL),
    m_hWndStatus(NULL),
    m_hStreamInfoFont(NULL),
    m_bIsColorPaused(false),
    m_colorResolution(NUI_IMAGE_RESOLUTION_INVALID),
    m_bIsDepthPaused(false),
    m_bIsDepthNearMode(false),
    m_depthResolution(NUI_IMAGE_RESOLUTION_INVALID),
    m_bIsSkeletonSeatedMode(false),
    m_bIsSkeletonDrawColor(false),
    m_bIsSkeletonDrawDepth(false),
    m_depthFilterID(IDM_DEPTH_FILTER_NOFILTER),
    m_colorFilterID(IDM_COLOR_FILTER_NOFILTER),
    m_pColorBitmapBits(NULL),
    m_hColorBitmap(NULL),
    m_pDepthBitmapBits(NULL),
    m_hDepthBitmap(NULL),
    m_hProcessStopEvent(NULL),
    m_hProcessThread(NULL),
    m_hColorResolutionMutex(NULL),
    m_hDepthResolutionMutex(NULL),
    m_hColorBitmapMutex(NULL),
    m_hDepthBitmapMutex(NULL),
    m_hPaintWindowMutex(NULL)
{
}

/// <summary>
/// Destructor
/// </summary>
CMainWindow::~CMainWindow()
{
    if (m_hProcessStopEvent)
    {
        // Signal processing thread to stop
        SetEvent(m_hProcessStopEvent);

        if (m_hProcessThread)
        {
            WaitForSingleObject(m_hProcessThread, INFINITE);
            CloseHandle(m_hProcessThread);
        }

        CloseHandle(m_hProcessStopEvent);
    }

    // Delete created handles and allocated data
    if (m_hDepthResolutionMutex)
    {
        CloseHandle(m_hDepthResolutionMutex);
    }

    if (m_hColorResolutionMutex)
    {
        CloseHandle(m_hColorResolutionMutex);
    }

    if (m_hdc)
    {
        DeleteDC(m_hdc);
    }

    // Destroy stream info font
    if (m_hStreamInfoFont)
    {
        DeleteObject(m_hStreamInfoFont);
    }

    // Destroy color and depth bitmap mutexes
    if (m_hColorBitmapMutex)
    {
        CloseHandle(m_hColorBitmapMutex);
    }

    if (m_hDepthBitmapMutex)
    {
        CloseHandle(m_hDepthBitmapMutex);
    }

    // Destroy paint window mutex
    if (m_hPaintWindowMutex)
    {
        CloseHandle(m_hPaintWindowMutex);
    }
}

/// <summary>
/// Runs the application
/// </summary>
/// <param name="hInstance">handle to the application instance</param>
/// <param name="nCmdShow">whether to display minimized, maximized, or normally</param>
/// <returns>WPARAM of final message as int</returns>
int CMainWindow::Run(HINSTANCE hInstance, int nCmdShow)
{
    // Create application window
    if (FAILED(CreateMainWindow(hInstance)))
    {
        return 0;
    }

    // Create mutexes
    m_hColorResolutionMutex = CreateMutex(NULL, FALSE, NULL);
    m_hDepthResolutionMutex = CreateMutex(NULL, FALSE, NULL);
    m_hColorBitmapMutex = CreateMutex(NULL, FALSE, NULL);
    m_hDepthBitmapMutex = CreateMutex(NULL, FALSE, NULL);
    m_hPaintWindowMutex = CreateMutex(NULL, FALSE, NULL);

    // Initialize default menu options and resolutions
    InitSettings(GetMenu(m_hWndMain));

    // Create bitmaps and fonts
    CreateStreamInformationFont();
    CreateColorImage();
    CreateDepthImage();

    // Perform Kinect initialization
    // If Kinect initialization succeeded, start the event processing thread
    // that will update the screen with depth and color images
    if (SUCCEEDED(CreateFirstConnected()))
    {
        // Create window processing thread
        m_hProcessStopEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
        m_hProcessThread = CreateThread(NULL, 0, ProcessThread, this, 0, NULL);

        NuiSetDeviceStatusCallback( &CMainWindow::StatusProc, this );
    }
    // If Kinect initialization failed, disable the menus
    else
    {
        DisableMenus();
    }

    // Display window
    ResizeWindow();
    ShowWindow(m_hWndMain, nCmdShow);

    // Main message loop:
    MSG msg;
    while (GetMessage(&msg, NULL, 0, 0))
    {
        // Process message
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    return static_cast<int>(msg.wParam);
}

/// <summary>
/// Handles window messages, passes most to the class instance to handle
/// </summary>
/// <param name="hWnd">window receiving message</param>
/// <param name="message">message</param>
/// <param name="wParam">message data</param>
/// <param name="lParam">additional message data</param>
/// <returns>result of message processing</param>
LRESULT CALLBACK CMainWindow::MessageRouter(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    CMainWindow* pThis = NULL;

    if (WM_NCCREATE == uMsg)
    {
        LPCREATESTRUCT lpcs = reinterpret_cast<LPCREATESTRUCT>(lParam);
        pThis = reinterpret_cast<CMainWindow*>(lpcs->lpCreateParams);
        SetWindowLongPtr(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pThis));
    }
    else
    {
        pThis = reinterpret_cast<CMainWindow*>(GetWindowLongPtr(hWnd, GWLP_USERDATA));
    }

    if (pThis)
    {
        return pThis->WndProc(hWnd, uMsg, wParam, lParam);
    }

    return 0;
}

/// <summary>
/// Returns the device connection id of the Kinect sensor that the sample is connected to
/// </summary>
/// <returns>device connection id of Kinect sensor</param>
BSTR CMainWindow::GetKinectDeviceConnectionId() const
{
    return m_frameHelper.GetKinectDeviceConnectionId();
}

/// <summary>
/// Handles window messages for the class instance
/// </summary>
/// <param name="hWnd">window receiving message</param>
/// <param name="message">message</param>
/// <param name="wParam">message data</param>
/// <param name="lParam">additional message data</param>
/// <returns>result of message processing</returns>
LRESULT CALLBACK CMainWindow::WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    // Parse message
    switch (message)
    {
        // Menu item message:
    case WM_COMMAND:
        {
            HMENU hMenu = GetMenu(hWnd);
            int wmID = LOWORD(wParam);
            int wmEvent = HIWORD(wParam);

            // Parse menu selection
            switch (wmID)
            {
            case IDM_COLOR_PAUSE:
                {
                    m_bIsColorPaused = !m_bIsColorPaused;
                    CheckMenuItem(hMenu, wmID, m_bIsColorPaused ? MF_CHECKED : MF_UNCHECKED);
                }
                break;
            case IDM_COLOR_RESOLUTION_640x480:
                {
                    // Update instance variable for processing thread to see, using mutex for synchronization
                    WaitForSingleObject(m_hColorResolutionMutex, INFINITE);
                    m_colorResolution = NUI_IMAGE_RESOLUTION_640x480;
                    ReleaseMutex(m_hColorResolutionMutex);
                    CheckMenuRadioItem(hMenu, COLOR_RESOLUTION_FIRST, COLOR_RESOLUTION_LAST, wmID, MF_BYCOMMAND);
                }
                break;
            case IDM_COLOR_RESOLUTION_1280x960:
                {
                    // Update instance variable for processing thread to see, using mutex for synchronization
                    WaitForSingleObject(m_hColorResolutionMutex, INFINITE);
                    m_colorResolution = NUI_IMAGE_RESOLUTION_1280x960;
                    ReleaseMutex(m_hColorResolutionMutex);
                    CheckMenuRadioItem(hMenu, COLOR_RESOLUTION_FIRST, COLOR_RESOLUTION_LAST, wmID, MF_BYCOMMAND);
                }
                break;
            case IDM_COLOR_FILTER_NOFILTER:
            case IDM_COLOR_FILTER_GAUSSIANBLUR:
            case IDM_COLOR_FILTER_DILATE:
            case IDM_COLOR_FILTER_ERODE:
            case IDM_COLOR_FILTER_CANNYEDGE:
                {
                    m_colorFilterID = wmID;
                    m_openCVHelper.SetColorFilter(wmID);
                    CheckMenuRadioItem(hMenu, COLOR_FILTER_FIRST, COLOR_FILTER_LAST, wmID, MF_BYCOMMAND);
                }
                break;
            case IDM_DEPTH_PAUSE:
                {
                    m_bIsDepthPaused= !m_bIsDepthPaused;
                    CheckMenuItem(hMenu, wmID, m_bIsDepthPaused ? MF_CHECKED : MF_UNCHECKED);
                }
                break;
            case IDM_DEPTH_NEARMODE:
                {
                    // Update depth stream, checking for failures
                    HRESULT hr = m_frameHelper.SetDepthStreamFlag(NUI_IMAGE_STREAM_FLAG_ENABLE_NEAR_MODE, !m_bIsDepthNearMode);
                    if (hr == E_NUI_HARDWARE_FEATURE_UNAVAILABLE)
                    {
                        SetStatusMessage(IDS_ERROR_KINECT_NONEARMODE);
                        break;
                    }
                    else if (FAILED(hr))
                    {
                        SetStatusMessage(IDS_ERROR_KINECT_NEARMODE);
                        break;
                    }

                    m_bIsDepthNearMode = !m_bIsDepthNearMode;
                    CheckMenuItem(hMenu, wmID, m_bIsDepthNearMode ? MF_CHECKED : MF_UNCHECKED);
                }
                break;
            case IDM_DEPTH_RESOLUTION_320x240:
                {
                    // Update instance variable for processing thread to see, using mutex for synchronization
                    WaitForSingleObject(m_hDepthResolutionMutex, INFINITE);
                    m_depthResolution = NUI_IMAGE_RESOLUTION_320x240;
                    ReleaseMutex(m_hDepthResolutionMutex);
                    CheckMenuRadioItem(hMenu, DEPTH_RESOLUTION_FIRST, DEPTH_RESOLUTION_LAST, wmID, MF_BYCOMMAND);
                }
                break;
            case IDM_DEPTH_RESOLUTION_640x480:
                {
                    // Update instance variable for processing thread to see, using mutex for synchronization
                    WaitForSingleObject(m_hDepthResolutionMutex, INFINITE);
                    m_depthResolution = NUI_IMAGE_RESOLUTION_640x480;
                    ReleaseMutex(m_hDepthResolutionMutex);
                    CheckMenuRadioItem(hMenu, DEPTH_RESOLUTION_FIRST, DEPTH_RESOLUTION_LAST, wmID, MF_BYCOMMAND);
                }
                break;
            case IDM_DEPTH_FILTER_NOFILTER:
            case IDM_DEPTH_FILTER_GAUSSIANBLUR:
            case IDM_DEPTH_FILTER_DILATE:
            case IDM_DEPTH_FILTER_ERODE:
            case IDM_DEPTH_FILTER_CANNYEDGE:
                {
                    m_depthFilterID = wmID;
                    CheckMenuRadioItem(hMenu, DEPTH_FILTER_FIRST, DEPTH_FILTER_LAST, wmID, MF_BYCOMMAND);
                    m_openCVHelper.SetDepthFilter(wmID);
                }
                break;
            case IDM_SKELETON_SEATEDMODE:
                {
                    // Update skeleton tracking flag, checking for failures
                    HRESULT hr = m_frameHelper.SetSkeletonTrackingFlag(NUI_SKELETON_TRACKING_FLAG_ENABLE_SEATED_SUPPORT, !m_bIsSkeletonSeatedMode);
                    if (FAILED(hr))
                    {
                        SetStatusMessage(IDS_ERROR_KINECT_SEATEDMODE);
                        break;
                    }

                    m_bIsSkeletonSeatedMode = !m_bIsSkeletonSeatedMode;
                    CheckMenuItem(hMenu, IDM_SKELETON_SEATEDMODE, m_bIsSkeletonSeatedMode ? MF_CHECKED : MF_UNCHECKED);

                }
                break;
            case IDM_SKELETON_DRAW_COLOR:
                {
                    m_bIsSkeletonDrawColor = !m_bIsSkeletonDrawColor;
                    CheckMenuItem(hMenu, wmID, m_bIsSkeletonDrawColor ? MF_CHECKED : MF_UNCHECKED);
                }
                break;
            case IDM_SKELETON_DRAW_DEPTH:
                {
                    m_bIsSkeletonDrawDepth = !m_bIsSkeletonDrawDepth;
                    CheckMenuItem(hMenu, wmID, m_bIsSkeletonDrawDepth ? MF_CHECKED : MF_UNCHECKED);
                }
                break;
            default:
                return DefWindowProc(hWnd, message, wParam, lParam);
            }
            break;
        }
    case WM_PAINT:
        {
            PaintWindow();
        }
        break;
    case WM_DESTROY:
        {
            PostQuitMessage(0);
        }
        break;
    default:
        return DefWindowProc(hWnd, message, wParam, lParam);
    }

    return 0;
}

/// <summary>
/// Callback to handle Kinect status changes
/// </summary>
/// <param name="hrStatus">current status</param>
/// <param name="instanceName">instance name of Kinect the status change is for</param>
/// <param name="uniqueDeviceName">unique device name of Kinect the status change is for</param>
/// <param name="pUserData">additional data</param>
void CALLBACK CMainWindow::StatusProc(HRESULT hrStatus, const OLECHAR* instanceName, const OLECHAR* uniqueDeviceName, void * pUserData)
{
    CMainWindow* window = reinterpret_cast<CMainWindow *>(pUserData);

    // Check if the Kinect sensor who's status changed is the one the sample is currently connected to.
    // Otherwise, we can ignore the status change. This check is done in case there are multiple Kinect sensors
    // plugged into the machine.
    // If the status change is not S_OK, then disable the menus.
    BSTR deviceConnectionId = window->GetKinectDeviceConnectionId();

    int compareResult = lstrcmp(instanceName, deviceConnectionId);
    if (compareResult == 0 && FAILED(hrStatus))
    {
        window->DisableMenus();
    }

}

/// <summary>
/// Thread to handle Kinect processing, calls class instance thread processor
/// </summary>
/// <param name="lpParam">instance pointer</param>
/// <returns>0</returns>
DWORD WINAPI CMainWindow::ProcessThread(LPVOID lpParam)
{
    // Use class instance thread processor
    CMainWindow* pThis = reinterpret_cast<CMainWindow*>(lpParam);
    return pThis->ProcessThread();
}

/// <summary>
/// Thread to handle Kinect processing
/// </summary>
/// <returns>0</returns>
DWORD WINAPI CMainWindow::ProcessThread()
{
    // Store local copies of resolutions to check for changes
    NUI_IMAGE_RESOLUTION colorResolution = m_colorResolution;
    NUI_IMAGE_RESOLUTION depthResolution = m_depthResolution;

    // Initialize array of events to wait for
    HANDLE hEvents[4] = {m_hProcessStopEvent, NULL, NULL, NULL};
    int numEvents;
    if (m_frameHelper.IsInitialized())
    {
        m_frameHelper.GetColorHandle(hEvents + 1);
        m_frameHelper.GetDepthHandle(hEvents + 2);
        m_frameHelper.GetSkeletonHandle(hEvents + 3);
        numEvents = 4;
    }
    else
    {
        numEvents = 1;
    }

    // Main update loop
    bool continueProcessing = true;
    while (continueProcessing)
    {
        // Use a mutex to check for update to color resolution
        WaitForSingleObject(m_hColorResolutionMutex, INFINITE);
        NUI_IMAGE_RESOLUTION newColorResolution = m_colorResolution;
        ReleaseMutex(m_hColorResolutionMutex);

        // Reopen color image stream if necessary
        if (colorResolution != newColorResolution)
        {
            // Stop painting while we change resolution
            WaitForSingleObject(m_hPaintWindowMutex, INFINITE);

            colorResolution = newColorResolution;

            HRESULT hr = m_frameHelper.SetColorFrameResolution(colorResolution);
            if (FAILED(hr))
            {
                SetStatusMessage(IDS_ERROR_KINECT_COLOR);
            }

            // Start painting again
            ReleaseMutex(m_hPaintWindowMutex);

            ResizeWindow();
            CreateColorImage();
        }

        // Use a mutex to check for update to depth resolution
        WaitForSingleObject(m_hDepthResolutionMutex, INFINITE);
        NUI_IMAGE_RESOLUTION newDepthResolution = m_depthResolution;
        ReleaseMutex(m_hDepthResolutionMutex);

        // Reopen depth image stream if necessary
        if (depthResolution != newDepthResolution)
        {
            // Stop painting while we change resolution
            WaitForSingleObject(m_hPaintWindowMutex, INFINITE);

            depthResolution = newDepthResolution;

            HRESULT hr = m_frameHelper.SetDepthFrameResolution(depthResolution);
            if (FAILED(hr))
            {
                SetStatusMessage(IDS_ERROR_KINECT_DEPTH);
            }

            // Start painting again
            ReleaseMutex(m_hPaintWindowMutex);

            ResizeWindow();
            CreateDepthImage();
        }

        // Wait for any event to be signalled
        int eventId = WaitForMultipleObjects(numEvents, hEvents, FALSE, 100);

        // No events were signalled in time
        if (WAIT_TIMEOUT == eventId)
        {
            continue;
        }

        // Stop event was signalled
        if (WAIT_OBJECT_0 == eventId)
        {
            continueProcessing = false;
            break;
        }

        // Update image outputs
        if (m_frameHelper.IsInitialized()) 
        {
            // Update skeleton frame
            NUI_SKELETON_FRAME skeletonFrame;
            if (((m_bIsSkeletonDrawDepth && !m_bIsDepthPaused) || (m_bIsSkeletonDrawColor && !m_bIsColorPaused))
                && SUCCEEDED(m_frameHelper.UpdateSkeletonFrame())) 
            {
                m_frameHelper.GetSkeletonFrame(&skeletonFrame);
            }

            // Update color frame
            if (!m_bIsColorPaused && SUCCEEDED(m_frameHelper.UpdateColorFrame())) 
            {
                HRESULT hr = m_frameHelper.GetColorImage(&m_colorMat);
                if (FAILED(hr))
                {
                    continue;
                }

                // Apply filter to color stream
                hr = m_openCVHelper.ApplyColorFilter(&m_colorMat);
                if (FAILED(hr))
                {
                    continue;
                }

                // Draw skeleton onto color stream
                if (m_bIsSkeletonDrawColor) 
                {
                    hr = m_openCVHelper.DrawSkeletonsInColorImage(&m_colorMat, &skeletonFrame, colorResolution, depthResolution);
                    if (FAILED(hr))
                    {
                        continue;
                    }
                }

                // Update bitmap for drawing
                WaitForSingleObject(m_hColorBitmapMutex, INFINITE);
                UpdateBitmap(&m_colorMat, &m_hColorBitmap, &m_bmiColor);
                ReleaseMutex(m_hColorBitmapMutex);

                // Notify frame rate tracker that new frame has been rendered
                m_colorFrameRateTracker.Tick();
            }

            // Update depth frame
            if (!m_bIsDepthPaused && SUCCEEDED(m_frameHelper.UpdateDepthFrame())) 
            {
                HRESULT hr = m_frameHelper.GetDepthImageAsArgb(&m_depthMat);
                if (FAILED(hr))
                {
                    continue;
                }

                // Apply filter to depth stream
                hr = m_openCVHelper.ApplyDepthFilter(&m_depthMat);
                if (FAILED(hr))
                {
                    continue;
                }

                // Draw skeleton onto depth stream
                if (m_bIsSkeletonDrawDepth)
                {
                    hr = m_openCVHelper.DrawSkeletonsInDepthImage(&m_depthMat, &skeletonFrame, depthResolution);
                    if (FAILED(hr))
                    {
                        continue;
                    }
                }

                // Update bitmap for drawing
                WaitForSingleObject(m_hDepthBitmapMutex, INFINITE);
                UpdateBitmap(&m_depthMat, &m_hDepthBitmap, &m_bmiDepth);
                ReleaseMutex(m_hDepthBitmapMutex);

                // Notify frame rate tracker that new frame has been rendered
                m_depthFrameRateTracker.Tick();
            }

            // Tell the window to paint the new bitmap
            WaitForSingleObject(m_hPaintWindowMutex, INFINITE);
            InvalidateRect(m_hWndMain, NULL, false);
            ReleaseMutex(m_hPaintWindowMutex);
        }
    }

    return 0;
}

/// <summary>
/// Creates the main and status bar windows
/// </summary>
/// <param name="hInstance">handle to the application instance</param>
/// <returns>S_OK if successful, E_FAIL otherwise</returns>
HRESULT CMainWindow::CreateMainWindow(HINSTANCE hInstance)
{
    // Load window strings
    TCHAR szTitle[100], szWindowClass[100];
    LoadString(hInstance, IDS_APPTITLE, szTitle, _countof(szTitle));
    LoadString(hInstance, IDS_KINECTOPENCV_WINDOW_CLASS, szWindowClass, _countof(szWindowClass));

    // Register window class
    WNDCLASSEX wcex;
    wcex.cbSize = sizeof(WNDCLASSEX);
    wcex.style = CS_HREDRAW | CS_VREDRAW;
    wcex.lpfnWndProc = MessageRouter;
    wcex.cbClsExtra = 0;
    wcex.cbWndExtra = 0;
    wcex.hInstance = hInstance;
    wcex.hIcon = LoadIcon(hInstance, MAKEINTRESOURCE(IDI_KINECTOPENCV));
    wcex.hCursor = LoadCursor(NULL, IDC_ARROW);
    wcex.hbrBackground = (HBRUSH)(COLOR_WINDOW+1);
    wcex.lpszMenuName = MAKEINTRESOURCE(IDC_KINECTOPENCV);
    wcex.lpszClassName = szWindowClass;
    wcex.hIconSm = NULL;

    if (!RegisterClassExW(&wcex))
    {
        return E_FAIL;
    }

    // Create main window, storing handle in global variable
    m_hWndMain = CreateWindowExW(0, szWindowClass, szTitle, WS_OVERLAPPED | WS_SYSMENU | WS_MINIMIZEBOX,
        CW_USEDEFAULT, 0, CW_USEDEFAULT, 0, NULL, NULL, hInstance, this);
    if (!m_hWndMain)
    {
        return E_FAIL;
    }

    // Initialize device context and store in global variable
    if (!m_hdc) 
    {
        m_hdc = CreateCompatibleDC(GetWindowDC(m_hWndMain));
    }

    // Create status bar, storing handle in global variable
    InitCommonControls();
    m_hWndStatus = CreateWindowExW(0, STATUSCLASSNAME, NULL, WS_CHILD, 0, 0, 0, 0, m_hWndMain,
        0, hInstance, NULL);
    if (!m_hWndStatus)
    {
        return E_FAIL;
    }

    // Store instance handle in global variable
    m_hInstance = hInstance;

    return S_OK;
}

/// <summary>
/// Resizes the main window to fit the color and depth bitmaps
/// </summary>
void CMainWindow::ResizeWindow()
{
    RECT statusRect;
    DWORD colorWidth, colorHeight, depthWidth, depthHeight;
    GetWindowRect(m_hWndStatus, &statusRect);
    m_frameHelper.GetColorFrameSize(&colorWidth, &colorHeight);
    m_frameHelper.GetDepthFrameSize(&depthWidth, &depthHeight);

    // Calculate the desired size of the client rectangle
    int width = colorWidth + depthWidth + 3 * BITMAP_VERTICAL_BORDER_PADDING;
    int height = max(colorHeight, depthHeight) + (statusRect.bottom - statusRect.top) + MENU_BAR_HORIZONTAL_BORDER_PADDING;

    // Calculate width and height of the window based on our desired client rectangle size
    RECT windowRect;
    windowRect.left = 0;
    windowRect.top = 0;
    windowRect.right = width;
    windowRect.bottom = height;
    AdjustWindowRect(&windowRect, WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX, FALSE);

    // Update window size based on stream resolutions
    SetWindowPos(m_hWndMain, NULL, 0, 0, 
        windowRect.right - windowRect.left, 
        windowRect.bottom - windowRect.top, 
        SWP_NOMOVE | SWP_NOZORDER);

    // Resize status bar to match new size
    SendMessage(m_hWndStatus, WM_SIZE, 0, 0);
}

/// <summary>
/// Paints the main window
/// </summary>
void CMainWindow::PaintWindow()
{   
    WaitForSingleObject(m_hPaintWindowMutex, INFINITE);

    // Determine dimensions of window
    RECT windowRect;
    GetClientRect(m_hWndMain, &windowRect); 
    DWORD windowWidth = windowRect.right - windowRect.left;
    DWORD windowHeight = windowRect.bottom - windowRect.top;

    // Set up a buffer to hold the contents of the window
    PAINTSTRUCT ps;	
    HDC hdc = BeginPaint(m_hWndMain, &ps);
    HDC hdcBuffer = CreateCompatibleDC(hdc); 
    HBITMAP hBitmap = CreateCompatibleBitmap(hdc, windowWidth, windowHeight); 
    HGDIOBJ hOldBitmap = SelectObject(hdcBuffer, hBitmap);
    FillRect(hdcBuffer, &windowRect, GetSysColorBrush(COLOR_WINDOW));

    // Get color stream information text
    WaitForSingleObject(m_hColorResolutionMutex, INFINITE);
    wstring colorStreamInfoText = GenerateStreamInformation(m_colorResolution, m_colorFilterID, m_colorFrameRateTracker.CurrentFPS());
    ReleaseMutex(m_hColorResolutionMutex);

    // Paint color bitmap
    WaitForSingleObject(m_hColorBitmapMutex, INFINITE);
    PaintBitmap(hdcBuffer, m_hColorBitmap, BITMAP_VERTICAL_BORDER_PADDING, MENU_BAR_HORIZONTAL_BORDER_PADDING, colorStreamInfoText.c_str());
    ReleaseMutex(m_hColorBitmapMutex);

    // Store width of color bitmap to properly position depth bitmap
    BITMAP bmColor;
    GetObject(m_hColorBitmap, sizeof(bmColor), &bmColor);
    DWORD colorBitmapWidth = bmColor.bmWidth;

    // Get depth stream information text
    WaitForSingleObject(m_hDepthResolutionMutex, INFINITE);
    wstring depthStreamInfoText = GenerateStreamInformation(m_depthResolution, m_depthFilterID, m_depthFrameRateTracker.CurrentFPS());
    ReleaseMutex(m_hDepthResolutionMutex);

    // Paint depth bitmap
    WaitForSingleObject(m_hDepthBitmapMutex, INFINITE);
    PaintBitmap(hdcBuffer, m_hDepthBitmap, colorBitmapWidth + 2 * BITMAP_VERTICAL_BORDER_PADDING, MENU_BAR_HORIZONTAL_BORDER_PADDING, depthStreamInfoText.c_str());
    ReleaseMutex(m_hDepthBitmapMutex);

    // Determine size of status bar
    RECT statusRect;
    GetWindowRect(m_hWndStatus, &statusRect);

    // Create buffer to hold status bar bitmap
    HDC hdcStatusBarBuffer = CreateCompatibleDC(hdc);
    HBITMAP hStatusBarBitmap = CreateCompatibleBitmap(hdc, windowWidth, (statusRect.bottom - statusRect.top));
    HGDIOBJ hOldStatusBarBitmap = SelectObject(hdcStatusBarBuffer, hStatusBarBitmap);

    // Paint status bar buffer contents to main window buffer
    SendMessage(m_hWndStatus, WM_PRINT, (WPARAM) hdcStatusBarBuffer, PRF_CHILDREN | PRF_CLIENT | PRF_ERASEBKGND | PRF_NONCLIENT | PRF_OWNED);
    BitBlt(hdcBuffer, 0, windowHeight - (statusRect.bottom - statusRect.top), windowWidth, (statusRect.bottom - statusRect.top), hdcStatusBarBuffer, 0, 0, SRCCOPY);

    // Clean up status bar buffer
    SelectObject(hdcStatusBarBuffer, hOldStatusBarBitmap);
    DeleteDC(hdcStatusBarBuffer);
    DeleteObject(hStatusBarBitmap);

    // Paint contents of main window buffer to the window
    BitBlt(hdc, windowRect.left, windowRect.top, windowRect.right - windowRect.left, windowRect.bottom - windowRect.top, hdcBuffer, 0, 0, SRCCOPY);

    // Clean up main window buffer
    SelectObject(hdcBuffer, hOldBitmap);
    DeleteDC(hdcBuffer); 
    DeleteObject(hBitmap); 
    EndPaint(m_hWndMain, &ps); 

    ReleaseMutex(m_hPaintWindowMutex);
}

/// <summary>
/// Initializes the settings, menu, and status bar
/// </summary>
/// <param name="hMenu">menu to initialize</param>
void CMainWindow::InitSettings(HMENU hMenu)
{
    // Set default color resolution, checking the appropriate radio buttons
    m_colorResolution = NUI_IMAGE_RESOLUTION_640x480;
    m_frameHelper.SetColorFrameResolution(m_colorResolution);
    CheckMenuRadioItem(hMenu, COLOR_RESOLUTION_FIRST, COLOR_RESOLUTION_LAST, IDM_COLOR_RESOLUTION_640x480, MF_BYCOMMAND);

    // Set default depth resolution, checking the appropriate radio buttons
    m_depthResolution = NUI_IMAGE_RESOLUTION_640x480;
    m_frameHelper.SetDepthFrameResolution(m_depthResolution);
    CheckMenuRadioItem(hMenu, DEPTH_RESOLUTION_FIRST, DEPTH_RESOLUTION_LAST, IDM_DEPTH_RESOLUTION_640x480, MF_BYCOMMAND);

    // Check default filter radio buttons
    CheckMenuRadioItem(hMenu, COLOR_FILTER_FIRST, COLOR_FILTER_LAST, IDM_COLOR_FILTER_NOFILTER, MF_BYCOMMAND);
    CheckMenuRadioItem(hMenu, DEPTH_FILTER_FIRST, DEPTH_FILTER_LAST, IDM_DEPTH_FILTER_NOFILTER, MF_BYCOMMAND);
}

/// <summary>
/// Initializes the first available Kinect found
/// </summary>
/// <returns>S_OK if successful, E_FAIL otherwise</returns>
HRESULT CMainWindow::CreateFirstConnected()
{
    // If Kinect is already initialized, return
    if (m_frameHelper.IsInitialized()) 
    {
        return S_OK;
    }

    HRESULT hr;

    // Get number of Kinect sensors
    int sensorCount = 0;
    hr = NuiGetSensorCount(&sensorCount);
    if (FAILED(hr)) 
    {
        return hr;
    }

    // If no sensors, update status bar to report failure and return
    if (sensorCount == 0)
    {
        SetStatusMessage(IDS_ERROR_KINECT_NOKINECT);
        return E_FAIL;
    }

    // Iterate through Kinect sensors until one is successfully initialized
    for (int i = 0; i < sensorCount; ++i) 
    {
        INuiSensor* sensor = NULL;
        hr = NuiCreateSensorByIndex(i, &sensor);
        if (SUCCEEDED(hr))
        {
            hr = m_frameHelper.Initialize(sensor);
            if (SUCCEEDED(hr)) 
            {
                // Report success
                SetStatusMessage(IDS_STATUS_INITSUCCESS);
                return S_OK;
            }
            else
            {
                // Uninitialize KinectHelper to show that Kinect is not ready
                m_frameHelper.UnInitialize();
            }
        }
    }

    // Report failure
    SetStatusMessage(IDS_ERROR_KINECT_INIT);
    return E_FAIL;
}

/// <summary>
/// Initializes the color bitmap
/// </summary>
/// <returns>S_OK if successful, E_FAIL otherwise</returns>
HRESULT CMainWindow::CreateColorImage()
{
    if (m_hColorBitmap)
    {
        DeleteObject(m_hColorBitmap);
    }

    DWORD width, height;
    m_frameHelper.GetColorFrameSize(&width, &height);

    Size size(width, height);
    m_colorMat.create(size, m_frameHelper.COLOR_TYPE);

    // Create the bitmap
    WaitForSingleObject(m_hColorBitmapMutex, INFINITE);
    HRESULT hr = CreateBitmap(size, &m_hColorBitmap, &m_bmiColor, m_pColorBitmapBits, IDS_ERROR_BITMAP_COLOR);
    ReleaseMutex(m_hColorBitmapMutex);

    return hr;
}

/// <summary>
/// Initializes the depth bitmap
/// </summary>
/// <returns>S_OK if successful, E_FAIL otherwise</returns>
HRESULT CMainWindow::CreateDepthImage()
{
    if (m_hDepthBitmap)
    {
        DeleteObject(m_hDepthBitmap);
    }

    DWORD width, height;
    m_frameHelper.GetDepthFrameSize(&width, &height);

    Size size(width, height);
    m_depthMat.create(size, m_frameHelper.DEPTH_RGB_TYPE);

    // Create the bitmap
    WaitForSingleObject(m_hDepthBitmapMutex, INFINITE);
    HRESULT hr = CreateBitmap(size, &m_hDepthBitmap, &m_bmiDepth, m_pDepthBitmapBits, IDS_ERROR_BITMAP_DEPTH);
    ReleaseMutex(m_hDepthBitmapMutex);

    return hr;
}

/// <summary>
/// Initializes the specified bitmap to the specified size
/// </summary>
/// <param name="size">the desired size</param>
/// <param name="phBitmap">pointer to handle of the bitmap to initialize</param>
/// <param name="pBmi">pointer to BITMAPINFO to initialize</param>
/// <param name="pBitmapBits">pointer to bits for bitmap data</param>
/// <param name="nID">ID of string resource of failure message</param>
/// <returns>S_OK if successful, E_FAIL otherwise</param>
HRESULT CMainWindow::CreateBitmap(Size size, HBITMAP* phBitmap, BITMAPINFO* pBmi, void* pBitmapBits, UINT nID)
{
    // Initialize device context and store in global variable
    if (!m_hdc) 
    {
        return E_NOT_VALID_STATE;
    }

    // Initialize bitmap based on resolution
    memset(pBmi, 0, sizeof(BITMAPINFO));
    pBmi->bmiHeader.biSize = sizeof(pBmi->bmiHeader);
    // Use negative height to indicate that bitmap is top-down
    pBmi->bmiHeader.biHeight = -size.height;
    pBmi->bmiHeader.biWidth = size.width;
    pBmi->bmiHeader.biPlanes = 1;
    pBmi->bmiHeader.biBitCount = 32;
    pBmi->bmiHeader.biSizeImage = pBmi->bmiHeader.biHeight * pBmi->bmiHeader.biWidth 
        * pBmi->bmiHeader.biPlanes * 4;
    *phBitmap = CreateDIBSection(m_hdc, pBmi, DIB_RGB_COLORS, &pBitmapBits, NULL, 0);

    // If initialization fails, update status bar
    if (!(*phBitmap))
    {
        SetStatusMessage(nID);
        return E_FAIL;
    }

    return S_OK;
}

/// <summary>
/// Updates the specified bitmap using the Mat
/// </summary>
/// <param name="pImg">pointer to Mat with image data</param>
/// <param name="phBitmap">pointer to handle of the bitmap to update</param>
/// <param name="pBmi">pointer to BITMAPINFO for updated bitmap</param>
void CMainWindow::UpdateBitmap(Mat* pImg, HBITMAP* phBitmap, BITMAPINFO* pBmi)
{
    int height = -pBmi->bmiHeader.biHeight;

    // Update bitmap
    SetDIBits(m_hdc, *phBitmap, 0, height, pImg->ptr(), pBmi, DIB_RGB_COLORS);
}

/// <summary>
/// Paints the given bitmap to the target device context at the given (x,y)
/// </summary>
/// <param name="hTarget">handle to target device context</param>
/// <param name="hBitmap">handle to source bitmap that will be painted to device context</param>
/// <param name="x">x coordinate of where to paint topleft corner of source bitmap</param>
/// <param name="y">y coordinate of where to paint topleft corner of source bitmap</param>
/// <param name="streamInfo">steam information to paint onto the bitmap</param>
void CMainWindow::PaintBitmap(HDC hTarget, HBITMAP hBitmap, int x, int y, LPCWSTR streamInfo)
{
    // Paint the bitmap
    BITMAP bm;
    HDC hdcMem = CreateCompatibleDC(hTarget);
    HGDIOBJ hPreviousBitmap = SelectObject(hdcMem, hBitmap);
    GetObject(hBitmap, sizeof(bm), &bm);
    BitBlt(hTarget, x, y, bm.bmWidth, bm.bmHeight, hdcMem, 0, 0, SRCCOPY);

    // Select the appropriate font
    HGDIOBJ hPreviousFont = SelectObject(hTarget, m_hStreamInfoFont);

    // Paint stream information text
    RECT rect;
    rect.left = x + 5;
    rect.top = y + 5;
    rect.bottom = y + bm.bmHeight - 10;
    rect.right = x + bm.bmWidth - 10;
    DrawText(hTarget, streamInfo, -1, &rect, DT_LEFT );

    // Put back the old font
    SelectObject(hTarget, hPreviousFont);

    // Delete device context for bitmap
    SelectObject(hdcMem, hPreviousBitmap);
    DeleteDC(hdcMem);
}

/// <summary>
/// Sets the status bar message to a string from the string table
/// </summary>
/// <param name="nID">ID of string resource</param>
void CMainWindow::SetStatusMessage(UINT nID)
{
    static TCHAR szRes[512];

    LoadStringW(m_hInstance, nID, szRes, _countof(szRes));
    SendMessageW(m_hWndStatus, SB_SETTEXT, 0, reinterpret_cast<LPARAM>(szRes));
}

/// <summary>
/// Disable the menus at the top of the window
/// </summary>
void CMainWindow::DisableMenus()
{
    HMENU hMenu = GetMenu(m_hWndMain);

    for (int i = 0; i < GetMenuItemCount(hMenu); ++i)
    {
        EnableMenuItem(hMenu, i, MF_BYPOSITION | MF_GRAYED);
    }

    // Signal processing thread to stop
    if (m_hProcessStopEvent)
    {
        SetEvent(m_hProcessStopEvent);
    }

    DrawMenuBar(m_hWndMain);
    InvalidateRect(m_hWndMain, NULL, false);
}

/// <summary>
/// Creates a font handle for the font used in the stream information text
/// </summary>
void CMainWindow::CreateStreamInformationFont()
{
    // Create menu font
    LOGFONT font;
    font.lfWeight = FW_NORMAL;
    font.lfHeight = -MulDiv(STREAM_INFO_TEXT_POINT_SIZE, GetDeviceCaps(m_hdc, LOGPIXELSY), 72);
    font.lfEscapement = 0;
    font.lfOrientation = 0;
    font.lfItalic = FALSE;
    font.lfUnderline = FALSE;
    font.lfStrikeOut = FALSE;

    wcscpy_s(font.lfFaceName, ARRAYSIZE(font.lfFaceName), L"Segoe UI");

    m_hStreamInfoFont = CreateFontIndirect(&font);
}

/// <summary>
/// Converts a given NUI_IMAGE_RESOLUTION into a human readable string
/// </summary>
/// <param name="resolution">NUI_IMAGE_RESOLUTION to convert into string</param>
wstring CMainWindow::NuiImageResolutionToString(NUI_IMAGE_RESOLUTION resolution)
{
    wstring text = _TEXT("Resolution: ");

    switch (resolution)
    {
    case NUI_IMAGE_RESOLUTION_1280x960:
        text += _TEXT("1280x960");
        break;

    case NUI_IMAGE_RESOLUTION_640x480:
        text += _TEXT("640x480");
        break;

    case NUI_IMAGE_RESOLUTION_320x240:
        text += _TEXT("320x240");
        break;

    case NUI_IMAGE_RESOLUTION_80x60:
        text += _TEXT("80x60");
        break;

    default:
        text += _TEXT("unknown");
        break;
    }

    return text;
}

/// <summary>
/// Converts a given filter ID into a human readable string
/// </summary>
/// <param name="filterID">ID of filter to convert into string</param>
wstring CMainWindow::FilterIDToString(int filterID)
{
    wstring text = _T("Filter: ");

    switch (filterID)
    {
    case IDM_COLOR_FILTER_NOFILTER:
    case IDM_DEPTH_FILTER_NOFILTER:
        text += _TEXT("None");
        break;

    case IDM_COLOR_FILTER_GAUSSIANBLUR:
    case IDM_DEPTH_FILTER_GAUSSIANBLUR:
        text += _TEXT("Gaussian Blur");
        break;

    case IDM_COLOR_FILTER_DILATE:
    case IDM_DEPTH_FILTER_DILATE:
        text += _TEXT("Dilate");
        break;

    case IDM_COLOR_FILTER_ERODE:
    case IDM_DEPTH_FILTER_ERODE:
        text += _TEXT("Erode");
        break;

    case IDM_COLOR_FILTER_CANNYEDGE:
    case IDM_DEPTH_FILTER_CANNYEDGE:
        text += _TEXT("Canny Edge");
        break;

    default:
        text += _TEXT("Unknown");
        break;
    }

    return text;
}

/// <summary>
/// Converts a given frame rate into a string
/// </summary>
/// <param name="value">frame rate to convert into string</param>
wstring CMainWindow::FrameRateToString(double frameRate)
{
    wostringstream stream;
    stream << frameRate;
    return _TEXT("FPS: ") + stream.str();
}

/// <summary>
/// Generates a string containing stream information from the given parameters
/// </summary>
/// <param name="resolution">resolution of images coming from stream</param>
/// <param name="filterID">id of the filter being applied to stream</param>
/// <param name="frameRate">actual frame rate of stream after filtering is applied</param>
wstring CMainWindow::GenerateStreamInformation(NUI_IMAGE_RESOLUTION resolution, int filterID, double frameRate)
{
    wstring streamInfoText = NuiImageResolutionToString(resolution);
    streamInfoText += _TEXT("\r\n") + FilterIDToString(filterID);
    streamInfoText += _TEXT("\r\n") + FrameRateToString(frameRate);

    return streamInfoText;
}

/// <summary>
/// Computes framerate based on the interval between two timings taken with clock()
/// </summary>
/// <param name="startClock">starting clock value of interval</param>
/// <param name="endClock">ending clock value of interval</param>
double CMainWindow::CalculateFrameRate(clock_t startClock, clock_t endClock)
{
    return floor(1 / ((endClock - startClock) / static_cast<double>(CLOCKS_PER_SEC) ));
}