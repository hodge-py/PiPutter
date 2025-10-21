using OpenCvSharp;
using System;

class Program
{
    static void Main()
    {




    }
    
    // Global or Class-level fields to manage the state
private Mat currentFrame;
private Task<CircleSegment[]> circleDetectionTask = null;
private CircleSegment[] latestCircles = new CircleSegment[0];

// This runs in your main thread (e.g., in a timer or a continuous while loop)
private void ProcessWebcamFrame()
{
    // 1. Grab the new frame
    // Assuming 'camera' is your VideoCapture object
    Mat newFrame = new Mat(); 
    camera.Read(newFrame); 

    if (newFrame.Empty()) return;

    currentFrame = newFrame; 
    
    // --- CONCURRENCY LOGIC ---
    
    // 2. Check if a detection task is already running
    if (circleDetectionTask == null || circleDetectionTask.IsCompleted)
    {
        // If the previous task finished, get the results
        if (circleDetectionTask != null && circleDetectionTask.IsCompleted)
        {
            try
            {
                // Get the result from the completed task
                latestCircles = circleDetectionTask.Result; 
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occurred in the background task
                Console.WriteLine($"Error during circle detection: {ex.Message}");
                latestCircles = new CircleSegment[0];
            }
        }

        // Start a *new* task using the current frame.
        // **IMPORTANT:** Clone the frame *before* passing it to the task to prevent race conditions.
        circleDetectionTask = Task.Run(() => FindCirclesInBackground(currentFrame.Clone()));
    }
    
    // 3. Draw the latest known circles on the current frame
    // This happens *every* frame, even while the new detection task is running.
    DrawCircles(currentFrame, latestCircles);

    // 4. Display the frame in the UI (e.g., using a PictureBox or similar)
    // Cv2.ImShow("Webcam Feed", currentFrame); 
}
}
