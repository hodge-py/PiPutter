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
        capture.FrameWidth = 640;
        capture.FrameHeight = 640;
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

            window.ShowImage(currentFrame);
            
            int key = Cv2.WaitKey(1);
            if (key == 27) // ESC key
                break;
        }

        

    }

}
