using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;
using System.IO;
using OpenCvSharp.Extensions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Drawing.Printing;

class Program
{
    private Mat currentFrame;
    private Task<CircleSegment[]> circleDetectionTask = null;
    private CircleSegment[] latestCircles = new CircleSegment[0];

    static void Main(string[] args)
    {
        using var capture = new VideoCapture(0);
        //System.Threading.Thread.Sleep(1000);
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
        using var gray = new Mat();
        using var blurred = new Mat();
        DateTime lastDetectionTime = DateTime.Now;
        bool isPaused = false;
        Point2f center1 = new Point2f(-1, -1);
        Point2f center2 = new Point2f(-1, -1);
        double time1 = 0;
        double time2 = 0;
        const double PixelsPerCm = 100.0; // Your calibrated value
        DateTime pauseStartTime = DateTime.Now;

        // Variables for State Control
        int captureFrameCount = 0;
        //DateTime pauseStartTime;
        const int PauseDurationSeconds = 5;

        const int BufferSize = 5; // The number of consecutive frames to use for the calculation
        Queue<Point2f> centerBuffer = new Queue<Point2f>(BufferSize); // Stores the center points
        Queue<double> timeBuffer = new Queue<double>(BufferSize);      // Stores the timestamps
        //const double PixelsPerCm = 20.0; // Your calibrated value

        // Variables for State Control (Same as before)
        //bool isPaused = false;
        //DateTime pauseStartTime;
        //const int PauseDurationSeconds = 5;



        while (true)
        {
            capture.Read(currentFrame);
            if (currentFrame.Empty())
                break;
            //bool golfBallDetected = DetectGolfBall(currentFrame);
            //Console.WriteLine(golfBallDetected ? "Golf Ball Detected!" : "No Golf Ball.");
        Mat hsv = new Mat();
        Cv2.CvtColor(currentFrame, hsv, ColorConversionCodes.BGR2HSV);

        // 2. Define the color range for a WHITE golf ball
        // These ranges can be tuned based on your lighting/environment.
        // White is generally a high Value (V) in HSV.
        Scalar lowerWhite = new Scalar(27, 27, 150);   // Hue, Saturation, Value
        Scalar upperWhite = new Scalar(180, 50, 255); // A low saturation for white/gray

        // 3. Create a mask that only shows the white areas
        Mat mask = new Mat();
        Cv2.InRange(hsv, lowerWhite, upperWhite, mask);

        // 4. Clean up the mask (optional but recommended for better shape detection)
        // Reduce noise
        Cv2.Erode(mask, mask, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)));
        // Close small gaps
        Cv2.Dilate(mask, mask, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(8, 8)));

        // 5. Find contours (shapes) in the mask
        Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(mask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        


        // 6. Iterate through the contours to find the golf ball
        foreach (var contour in contours)
            {
            if (isPaused)
            {
                // Check if 5 seconds have elapsed
                //Console.WriteLine((DateTime.Now - pauseStartTime).TotalSeconds);
                if ((DateTime.Now - pauseStartTime).TotalSeconds >= PauseDurationSeconds)
                {
                    Console.WriteLine((DateTime.Now - pauseStartTime).TotalSeconds);
                    isPaused = false;
                    pauseStartTime = DateTime.Now;
                    centerBuffer.Clear();
                    timeBuffer.Clear(); 
                    Console.WriteLine("\nPause finished. Ready for new detection.");
                }
                // Skip all processing and detection until the pause is over
                //Cv2.ImShow("Webcam Feed", frame); // Still show the frame
                //if (Cv2.WaitKey(1) == 'q') { break; }
            }
            else{
                // Get the ball's center and the current time
                Cv2.MinEnclosingCircle(contour, out Point2f currentCenter, out float radius);
                double currentTime = Cv2.GetTickCount() / Cv2.GetTickFrequency(); // High-precision time in seconds
                
                centerBuffer.Enqueue(currentCenter);
                timeBuffer.Enqueue(currentTime);
                
                Console.WriteLine(centerBuffer.Count);
                    // 2. Maintain buffer size

                if (centerBuffer.Count > BufferSize)
                    {
                        centerBuffer.Dequeue(); // Remove the oldest point
                        timeBuffer.Dequeue();   // Remove the oldest time
                    }

                    // 3. Check if we have enough data to calculate
                    if (centerBuffer.Count == BufferSize)
                    {
                        CalculateAndDisplaySpeed(centerBuffer, timeBuffer, PixelsPerCm, BufferSize);

                        // --- START PAUSE AFTER A SUCCESSFUL MEASUREMENT ---
                        isPaused = true;
                        pauseStartTime = DateTime.Now;
                        Console.WriteLine($"Detection paused for {PauseDurationSeconds} seconds...");
                    }

    /*                else
                    {
                        // If the ball is lost or disappears, clear the buffer to prevent calculating
                        // speed between two points that are far apart in time or location.
                        centerBuffer.Clear();
                        timeBuffer.Clear();
                    } */
            }
        }
            window.ShowImage(currentFrame);

            int key = Cv2.WaitKey(1);
            if (key == 27) // ESC key
                break;
        }

    }


private static void CalculateAndDisplaySpeed(Queue<Point2f> centers, Queue<double> times, double PixelsPerCm,int BufferSize)
        {
        // Get the oldest point/time (start)
    Point2f centerStart = centers.Peek(); 
    double timeStart = times.Peek(); 

    // Get the newest point/time (end)
    Point2f centerEnd = centers.Last();
    double timeEnd = times.Last();

    // --- 1. CALCULATE DIFFERENTIALS AND DISTANCE ---
    double dx = centerEnd.X - centerStart.X;
    double dy = centerEnd.Y - centerStart.Y;
    
    double distancePixels = Math.Sqrt(dx * dx + dy * dy);
    double distanceCm = distancePixels / PixelsPerCm;

    // --- 2. TIME ELAPSED ---
    double timeSeconds = timeEnd - timeStart;

    // --- 3. HORIZONTAL ANGLE CALCULATION ---
    double angleRadians = Math.Atan2(dy, dx);
    double angleDegrees = angleRadians * (180.0 / Math.PI);
    if (angleDegrees < 0) { angleDegrees += 360; }

    // --- 4. SPEED CALCULATION ---
    if (timeSeconds > 0)
    {
        double speedCmPerSecond = distanceCm / timeSeconds;
        double speedMetersPerSecond = speedCmPerSecond / 100.0;

        // --- 5. DISPLAY RESULTS ---
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n--- AVERAGE SPEED & ANGLE CALCULATED (Over {BufferSize} frames) ---");
        Console.WriteLine($"Net Distance: {distanceCm:F2} cm");
        Console.WriteLine($"Total Time Elapsed: {timeSeconds * 1000:F2} ms");
        Console.WriteLine($"Average Rolling Speed: {speedMetersPerSecond:F2} m/s");
        Console.WriteLine($"Horizontal Angle: {angleDegrees:F2} degrees");
        Console.ResetColor();
    }
    else
    {
        Console.WriteLine("Error: Time elapsed was zero or negative.");
    }
    }



}
