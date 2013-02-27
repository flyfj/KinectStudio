namespace WpfD3DInterop
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Magnifier Image Settings
        private const double MagImageScale = 1.25; // Scale of image to magnified ellipse
        private const double MagImageOffset = 0.12; // Offset of magnified ellipse within image

        // Unit conversion
        private const float DegreesToRadians = (float)Math.PI / 180;

        // State Management
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();
        private bool magnify = true;

        // Magnifier Settings (filled by default slider vlaues)
        private double magSize;
        private double magScale;
        
        public MainWindow()
        {
            this.InitializeComponent();
            this.host.Loaded += new RoutedEventHandler(this.Host_Loaded);
            this.host.SizeChanged += new SizeChangedEventHandler(this.Host_SizeChanged);

            SensorChooserUI.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.KinectChanged += this.KinectChanged;
        }

        private static bool Init(string kinectId)
        {
            bool initSucceeded = NativeMethods.InvokeWithDllProtection(() => NativeMethods.Init(kinectId)) >= 0;
            
            if (!initSucceeded)
            {
                MessageBox.Show("Failed to initialize.", "WPF D3D Interop", MessageBoxButton.OK, MessageBoxImage.Error);

                if (Application.Current != null)
                {
                    Application.Current.Shutdown();
                }
            }

            return initSucceeded;
        }

        private static void Cleanup()
        {
            NativeMethods.InvokeWithDllProtection(NativeMethods.Cleanup);
        }

        private static int Render(IntPtr resourcePointer)
        {
            return NativeMethods.InvokeWithDllProtection(() => NativeMethods.Render(resourcePointer));      
        }

        private static int SetCameraRadius(float radius)
        {
            return NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetCameraRadius(radius));
        }

        private static int SetCameraTheta(float theta)
        {
            return NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetCameraTheta(theta));    
        }

        private static int SetCameraPhi(float phi)
        {
            return NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetCameraPhi(phi));
        }

        #region Callbacks
        private void Host_Loaded(object sender, RoutedEventArgs e)
        {
            this.sensorChooser.Start();

            // Setup the Magnifier Size
            MagEllipse.Height = this.magSize;
            MagEllipse.Width = this.magSize;
            Scale.Value = this.magScale;

            // Add mouse over event
            host.MouseMove += this.MagElement_MouseMove;
            ImageHost.MouseMove += this.MagElement_MouseMove;
            MagEllipse.MouseMove += this.MagElement_MouseMove;
            MagImage.MouseMove += this.MagElement_MouseMove;

            host.MouseLeave += this.MagElement_MouseLeave;
            MagEllipse.MouseLeave += this.MagElement_MouseLeave;
            ImageHost.MouseLeave += this.MagElement_MouseLeave;
            MagImage.MouseLeave += this.MagElement_MouseLeave;

            MagBox.Checked += this.MagBox_Checked;
            MagBox.Unchecked += this.MagBox_Unchecked;
        }

        private void Host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // TODO: handle non-96 DPI
            int surfWidth = (int)(host.ActualWidth < 0 ? 0 : Math.Ceiling(host.ActualWidth));
            int surfHeight = (int)(host.ActualHeight < 0 ? 0 : Math.Ceiling(host.ActualHeight));

            InteropImage.SetPixelSize(surfWidth, surfHeight);
        }

        private void Scale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.magScale = e.NewValue;
        }

        private void Size_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.magSize = e.NewValue;

            // Setup the Magnifier Size
            this.MagEllipse.Height = this.magSize;
            this.MagEllipse.Width = this.magSize;
        }

        private void MagBox_Checked(object sender, RoutedEventArgs e)
        {
            this.magnify = true;

            MagCurserToggle1.Cursor = System.Windows.Input.Cursors.None;
            MagCurserToggle2.Cursor = System.Windows.Input.Cursors.None;
            host.Cursor = System.Windows.Input.Cursors.None;
        }

        private void MagBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.magnify = false;

            MagCurserToggle1.Cursor = System.Windows.Input.Cursors.Arrow;
            MagCurserToggle2.Cursor = System.Windows.Input.Cursors.Arrow;
            host.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private void MagElement_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.magnify)
            {
                Point point = Mouse.GetPosition(host);

                if (!(point.X < 0 || point.Y < 0 || point.X > host.ActualWidth || point.Y > host.ActualHeight))
                {
                    // Draw the Magnified ellipse on top of image
                    System.Windows.Controls.Canvas.SetTop(this.MagEllipse, point.Y - (this.magSize / 2));
                    System.Windows.Controls.Canvas.SetLeft(this.MagEllipse, point.X - (this.magSize / 2));

                    // Set the magnifier image on top of magnified ellipse 
                    System.Windows.Controls.Canvas.SetTop(this.MagImage, point.Y - (this.magSize * (.5 + MagImageOffset)));
                    System.Windows.Controls.Canvas.SetLeft(this.MagImage, point.X - (this.magSize * (.5 + MagImageOffset)));
                    MagImage.Width = this.magSize * MagImageScale;

                    MagEllipse.Visibility = System.Windows.Visibility.Visible;
                    MagImage.Visibility = System.Windows.Visibility.Visible;

                    double magViewboxSize = this.magSize / this.magScale;
                    MagBrush.Viewbox = new Rect(point.X - (.5 * magViewboxSize), point.Y - (.5 * magViewboxSize), magViewboxSize, magViewboxSize);
                }
                else
                {
                    MagEllipse.Visibility = Visibility.Hidden;
                    MagImage.Visibility = Visibility.Hidden;
                }
            }
        }

        private void MagElement_MouseLeave(object sender, MouseEventArgs e)
        {
            MagEllipse.Visibility = Visibility.Hidden;
            MagImage.Visibility = Visibility.Hidden;
        }

        private void Radius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetCameraRadius((float)e.NewValue);
        }

        private void Theta_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetCameraTheta((float)e.NewValue * DegreesToRadians);
        }

        private void Phi_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetCameraPhi((float)e.NewValue * DegreesToRadians);
        }
        #endregion Callbacks

        #region Helpers
        private void UpdateD3D(object sender, EventArgs e)
        {
            InteropImage.RequestRender();
        }

        private void InitializeKinect(string kinectId)
        {
            if (!Init(kinectId))
            {
                // Something went wrong
                return;
            }
            
            InteropImage.WindowOwner = (new System.Windows.Interop.WindowInteropHelper(this)).Handle;
            InteropImage.OnRender = this.DoRender;
            CompositionTarget.Rendering += this.UpdateD3D;

            // Set up camera
            SetCameraRadius((float)RadiusSlider.Value);
            SetCameraPhi((float)PhiSlider.Value * DegreesToRadians);
            SetCameraTheta((float)ThetaSlider.Value * DegreesToRadians);

            // Start rendering now!
            InteropImage.RequestRender();
        }

        private void UninitializeKinect()
        {
            Cleanup();

            CompositionTarget.Rendering -= this.UpdateD3D;
        }
        #endregion Helpers

        private void DoRender(IntPtr surface)
        {
            Render(surface);
        }

        /// <summary>
        /// Event handler for KinectSensorChooser's KinectChanged event.
        /// </summary>
        /// <param name="sender">
        /// Object sending the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        private void KinectChanged(object sender, KinectChangedEventArgs e)
        {
            if (null != e.OldSensor)
            {
                this.UninitializeKinect();
            }

            // verify we received a valid sensor
            // note that UniqueKinectId may be null in some cases
            // if the sensor is valid, pass it down to the native side
            if (null != e.NewSensor && null != e.NewSensor.UniqueKinectId)
            {
                string kinectId = string.Copy(e.NewSensor.UniqueKinectId);
                e.NewSensor.Stop();
                this.InitializeKinect(kinectId);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.UninitializeKinect();

            host.MouseMove -= this.MagElement_MouseMove;
            ImageHost.MouseMove -= this.MagElement_MouseMove;
            MagEllipse.MouseMove -= this.MagElement_MouseMove;
            MagImage.MouseMove -= this.MagElement_MouseMove;

            host.MouseLeave -= this.MagElement_MouseLeave;
            MagEllipse.MouseLeave -= this.MagElement_MouseLeave;
            ImageHost.MouseLeave -= this.MagElement_MouseLeave;
            MagImage.MouseLeave -= this.MagElement_MouseLeave;

            MagBox.Checked -= this.MagBox_Checked;
            MagBox.Unchecked -= this.MagBox_Unchecked;
        }

        private static class NativeMethods
        {
            /// <summary>
            /// Variable used to track whether the missing dependency dialog has been displayed,
            /// used to prevent multiple notifications of the same failure.
            /// </summary>
            private static bool errorHasDisplayed;

            [DllImport("D3DVisualization.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int Init([MarshalAs(UnmanagedType.BStr)] string kinectId);

            [DllImport("D3DVisualization.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void Cleanup();

            [DllImport("D3DVisualization.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int Render(IntPtr resourcePointer);

            [DllImport("D3DVisualization.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SetCameraRadius(float radius);

            [DllImport("D3DVisualization.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SetCameraTheta(float theta);

            [DllImport("D3DVisualization.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SetCameraPhi(float phi);

            /// <summary>
            /// Method used to invoke an Action that will catch DllNotFoundExceptions and display a warning dialog.
            /// </summary>
            /// <param name="action">The Action to invoke.</param>
            public static void InvokeWithDllProtection(Action action)
            {
                InvokeWithDllProtection(
                    () => 
                    { 
                        action.Invoke();
                        return 0;
                    }); 
            }

            /// <summary>
            /// Method used to invoke A Func that will catch DllNotFoundExceptions and display a warning dialog.
            /// </summary>
            /// <param name="func">The Func to invoke.</param>
            /// <returns>The return value of func, or default(T) if a DllNotFoundException was caught.</returns>
            /// <typeparam name="T">The return type of the func.</typeparam>
            public static T InvokeWithDllProtection<T>(Func<T> func)
            {
                try
                {
                    return func.Invoke();
                }
                catch (DllNotFoundException)
                {
                    if (!errorHasDisplayed)
                    {
                        MessageBox.Show("This sample requires installation of the DirectX runtime to run properly.", "WPF D3D Interop", MessageBoxButton.OK, MessageBoxImage.Error);
                        errorHasDisplayed = true;

                        if (Application.Current != null)
                        {
                            Application.Current.Shutdown();                            
                        }
                    }
                }

                return default(T);
            }
        }
    }
}
