//-----------------------------------------------------------------------------
// <copyright file="MainWindow.h" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------------

#pragma once

#include "resource.h"
#include <Windows.h>
#include <tchar.h>
#include <CommCtrl.h>
#include <string>
#include <sstream>
#include "time.h"
#include "math.h"

#include <NuiApi.h>

#include "OpenCVHelper.h"
#include "FrameRateTracker.h"

class CMainWindow
{
    // Constants:
    // First and last menu item identifiers for resolution radio buttons
    static const int COLOR_RESOLUTION_FIRST = IDM_COLOR_RESOLUTION_640x480;
    static const int COLOR_RESOLUTION_LAST = IDM_COLOR_RESOLUTION_1280x960;

    static const int DEPTH_RESOLUTION_FIRST = IDM_DEPTH_RESOLUTION_320x240;
    static const int DEPTH_RESOLUTION_LAST = IDM_DEPTH_RESOLUTION_640x480;

    // First and last menu item identifiers for filter radio buttons
    static const int COLOR_FILTER_FIRST = IDM_COLOR_FILTER_NOFILTER;
    static const int COLOR_FILTER_LAST = IDM_COLOR_FILTER_CANNYEDGE;

    static const int DEPTH_FILTER_FIRST = IDM_DEPTH_FILTER_NOFILTER;
    static const int DEPTH_FILTER_LAST = IDM_DEPTH_FILTER_CANNYEDGE;

	// Font size in points of the stream information
	static const int STREAM_INFO_TEXT_POINT_SIZE = 10;

	// Padding
	static const int BITMAP_VERTICAL_BORDER_PADDING = 10;
	static const int MENU_BAR_HORIZONTAL_BORDER_PADDING = 5;

public:
    // Functions:
    /// <summary>
    /// Constructor
    /// </summary>
    CMainWindow();

    /// <summary>
    /// Destructor
    /// </summary>
    ~CMainWindow();

    /// <summary>
    /// Runs the application
    /// </summary>
    /// <param name="hInstance">handle to the application instance</param>
    /// <param name="nCmdShow">whether to display minimized, maximized, or normally</param>
    /// <returns>WPARAM of final message as int</returns>
    int Run(HINSTANCE hInstance, int nCmdShow);

    /// <summary>
    /// Handles window messages, passes most to the class instance to handle
    /// </summary>
    /// <param name="hWnd">window receiving message</param>
    /// <param name="message">message</param>
    /// <param name="wParam">message data</param>
    /// <param name="lParam">additional message data</param>
    /// <returns>result of message processing</param>
    static LRESULT CALLBACK MessageRouter(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam);

	/// <summary>
    /// Returns the device connection id of the Kinect sensor that the sample is connected to
    /// </summary>
	/// <returns>device connection id of Kinect sensor</param>
	BSTR GetKinectDeviceConnectionId() const;

private:
    // Functions:
    /// <summary>
    /// Handles window messages for the class instance
    /// </summary>
    /// <param name="hWnd">window receiving message</param>
    /// <param name="message">message</param>
    /// <param name="wParam">message data</param>
    /// <param name="lParam">additional message data</param>
    /// <returns>result of message processing</returns>
    LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam);

	/// <summary>
	/// Callback to handle Kinect status changes, redirects to the class callback handler
	/// </summary>
	/// <param name="hrStatus">current status</param>
	/// <param name="instanceName">instance name of Kinect the status change is for</param>
	/// <param name="uniqueDeviceName">unique device name of Kinect the status change is for</param>
	/// <param name="pUserData">additional data</param>
	static void CALLBACK StatusProc(HRESULT hrStatus, const OLECHAR* instanceName, const OLECHAR* uniqueDeviceName, void * pUserData);

    /// <summary>
    /// Thread to handle Kinect processing, calls class instance thread processor
    /// </summary>
    /// <param name="lpParam">instance pointer</param>
    /// <returns>0</returns>
    static DWORD WINAPI ProcessThread(LPVOID lpParam);

    /// <summary>
    /// Thread to handle Kinect processing
    /// </summary>
    /// <returns>0</returns>
    DWORD WINAPI ProcessThread();

    /// <summary>
    /// Creates the main and status bar windows
    /// </summary>
    /// <param name="hInstance">handle to the application instance</param>
    /// <returns>S_OK if successful, E_FAIL otherwise</returns>
    HRESULT CreateMainWindow(HINSTANCE hInstance);
	
    /// <summary>
    /// Resizes the main window to fit the color and depth bitmaps
    /// </summary>
    void ResizeWindow();

    /// <summary>
    /// Paints the main window
    /// </summary>
    void PaintWindow();

    /// <summary>
    /// Initializes the settings, menu, and status bar
    /// </summary>
    /// <param name="hMenu">menu to initialize</param>
    void InitSettings(HMENU hMenu);

    /// <summary>
    /// Initializes the first available Kinect found
    /// </summary>
    /// <returns>S_OK if successful, E_FAIL otherwise</returns>
    HRESULT CreateFirstConnected();

    /// <summary>
    /// Initializes the color bitmap and OpenCV matrix
    /// </summary>
    /// <returns>S_OK if successful, E_FAIL otherwise</returns>
    HRESULT CreateColorImage();

    /// <summary>
    /// Initializes the depth bitmap and OpenCV matrix
    /// </summary>
    /// <returns>S_OK if successful, E_FAIL otherwise</returns>
    HRESULT CreateDepthImage();

    /// <summary>
    /// Initializes the specified bitmap to the specified size
    /// </summary>
    /// <param name="size">the desired size</param>
    /// <param name="phBitmap">pointer to handle of the bitmap to initialize</param>
    /// <param name="pBmi">pointer to BITMAPINFO to initialize</param>
    /// <param name="pBitmapBits">pointer to bits for bitmap data</param>
    /// <param name="nID">ID of string resource of failure message</param>
    /// <returns>S_OK if successful, E_FAIL otherwise</param>
    HRESULT CreateBitmap(Size size, HBITMAP* phBitmap, BITMAPINFO* pBmi, void* pBitmapBits, UINT nID);

    /// <summary>
    /// Updates the specified bitmap using the Mat
    /// </summary>
    /// <param name="pImg">pointer to Mat with image data</param>
    /// <param name="phBitmap">pointer to handle of the bitmap to update</param>
    /// <param name="pBmi">pointer to BITMAPINFO for updated bitmap</param>
    void UpdateBitmap(Mat* pImg, HBITMAP* phBitmap, BITMAPINFO* pBmi);

	/// <summary>
    /// Paints the given bitmap to the target device context at the given (x,y).
	/// This method also paints the given stream information onto the bitmap
    /// </summary>
    /// <param name="hTarget">handle to target device context</param>
    /// <param name="hBitmap">handle to source bitmap that will be painted to device context</param>
    /// <param name="x">x coordinate of where to paint topleft corner of source bitmap</param>
	/// <param name="y">y coordinate of where to paint topleft corner of source bitmap</param>
	/// <param name="streamInfo">steam information to paint onto the bitmap</param>
	void PaintBitmap(HDC hTarget, HBITMAP hBitmap, int x, int y, LPCWSTR streamInfo);

    /// <summary>
    /// Sets the status bar message to a string from the string table
    /// </summary>
    /// <param name="nID">ID of string resource</param>
    void SetStatusMessage(UINT nID);

	/// <summary>
	/// Disable the menus at the top of the window
	/// </summary>
	void DisableMenus();

	/// <summary>
    /// Creates a font handle for the font used in the stream information text
    /// </summary>
	void CreateStreamInformationFont();

	/// <summary>
    /// Converts a given NUI_IMAGE_RESOLUTION into a human readable string
    /// </summary>
    /// <param name="resolution">NUI_IMAGE_RESOLUTION to convert into string</param>
	std::wstring NuiImageResolutionToString(NUI_IMAGE_RESOLUTION resolution);

	/// <summary>
    /// Converts a given filter ID into a human readable string
    /// </summary>
    /// <param name="filterID">ID of filter to convert into string</param>
	std::wstring FilterIDToString(int filterID);

	/// <summary>
    /// Converts a given frame rate into a string
    /// </summary>
    /// <param name="value">frame rate to convert into string</param>
	std::wstring FrameRateToString(double frameRate);

	/// <summary>
    /// Generates a string containing stream information from the given parameters
    /// </summary>
    /// <param name="resolution">resolution of images coming from stream</param>
	/// <param name="filterID">id of the filter being applied to stream</param>
	/// <param name="frameRate">actual frame rate of stream after filtering is applied</param>
	std::wstring GenerateStreamInformation(NUI_IMAGE_RESOLUTION resolution, int filterID, double frameRate);

	/// <summary>
    /// Computes framerate based on the interval between two timings taken with clock()
    /// </summary>
    /// <param name="startClock">starting clock value of interval</param>
	/// <param name="endClock">ending clock value of interval</param>
	double CalculateFrameRate(clock_t startClock, clock_t endClock);

    // Variables:
    // Program information
    HINSTANCE m_hInstance;                      // Current instance
    HDC m_hdc;                                  // Device context
    HWND m_hWndMain;                            // Main window
    HWND m_hWndStatus;                          // Status bar
	HFONT m_hStreamInfoFont;					// Font for the stream info text

    // Helpers
    Microsoft::KinectBridge::OpenCVFrameHelper m_frameHelper;
    OpenCVHelper m_openCVHelper;

    // App settings
    bool m_bIsColorPaused;
    NUI_IMAGE_RESOLUTION m_colorResolution;
	int m_colorFilterID;

    bool m_bIsDepthPaused;
    bool m_bIsDepthNearMode;
    NUI_IMAGE_RESOLUTION m_depthResolution;
	int m_depthFilterID;

    bool m_bIsSkeletonSeatedMode;
    bool m_bIsSkeletonDrawColor;
    bool m_bIsSkeletonDrawDepth;

	// Frame rate tracking
	FrameRateTracker m_colorFrameRateTracker;
	FrameRateTracker m_depthFrameRateTracker;

	// OpenCV matrices
	Mat m_colorMat;
	Mat m_depthMat;

    // Bitmaps
    BITMAPINFO m_bmiColor;
    void* m_pColorBitmapBits;
    HBITMAP m_hColorBitmap;

    BITMAPINFO m_bmiDepth;
    void* m_pDepthBitmapBits;
    HBITMAP m_hDepthBitmap;

    // Window processing thread handles
    HANDLE m_hProcessStopEvent;
    HANDLE m_hProcessThread;

	// Mutexes that control access to m_colorResolution and m_depthResolution
    HANDLE m_hColorResolutionMutex;
    HANDLE m_hDepthResolutionMutex;

	// Mutexes that control access to m_hColorBitmap and m_hDepthBitmap
	HANDLE m_hColorBitmapMutex;
	HANDLE m_hDepthBitmapMutex;

	// Mutex that controls painting
	HANDLE m_hPaintWindowMutex;
};