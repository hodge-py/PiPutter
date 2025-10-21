using OpenCvSharp;
using System;
class Program
{
    private Mat currentFrame;
    private Task<CircleSegment[]> circleDetectionTask = null;
    private CircleSegment[] latestCircles = new CircleSegment[0];

    static void Main(string[] args)
    {
        using var capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
        if (!capture.IsOpened())
        {
            Console.WriteLine("Camera not found!");
            return;
        }
        camera(capture);
    }

    private static void camera(VideoCapture capture)
    {
        // This method is intentionally left blank to demonstrate unused code.

        using var window = new Window("Camera");
        using var currentFrame = new Mat();
        while (true)
        {
            capture.Read(currentFrame);
            if (currentFrame.Empty())
                break;
            CircleSegment[] circles = FindCirclesInBackground(currentFrame);

            foreach (CircleSegment circle in circles)
            {
                // 1. Get the center point (Point) and radius (int)
                Point center = (Point)circle.Center;
                int radius = (int)circle.Radius;

                // --- Draw the circle perimeter ---
                Cv2.Circle(
                    img: currentFrame,
                    center: center,
                    radius: radius,
                    color: Scalar.Red, // Color of the circle line (e.g., Red)
                    thickness: 2,      // Thickness of the circle line in pixels
                    lineType: LineTypes.AntiAlias // Smoother line drawing
                );
            }
            
            window.ShowImage(currentFrame);
            
            int key = Cv2.WaitKey(1);
            if (key == 27) // ESC key
                break;
        }

        

    }

    private static CircleSegment[] FindCirclesInBackground(Mat frameToProcess)
    {
        // Make a clone of the frame to ensure thread safety! 
        // This prevents the main thread from modifying the frame while it's being processed.
        using (Mat grayScale = frameToProcess.Clone())
        {
            // 1. Convert to grayscale/blur (if not already done in the main loop)
            // You mentioned 'blurredFrame', so this step might be done before calling this method.
            // Assuming 'clone' is already the correct type (e.g., grayscale/blurred).
            Mat blurredFrame = new Mat();
            Cv2.CvtColor(frameToProcess, grayScale, ColorConversionCodes.BGR2GRAY);
            Cv2.GaussianBlur(grayScale, blurredFrame, new Size(21, 21), 2, 2);



            // 2. Perform the intensive HoughCircles operation
            return Cv2.HoughCircles(
                blurredFrame, // Use the clone for thread safety
                HoughModes.Gradient,
                dp: 1.5, // Recommended efficiency change
                minDist: 300,
                param1: 200,
                param2: 40,
                minRadius: 10, // Recommended efficiency change
                maxRadius: 100 // Recommended efficiency change
            );
        }

    }
}
