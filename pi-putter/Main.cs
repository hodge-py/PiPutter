using Godot;
using System;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using System.IO;
using OpenCvSharp.Extensions;
using System.Collections.Generic; // Add this line
using System.Linq;
using System.Threading.Tasks;

public partial class Main : Node3D
{
	
	private const string GolfBallNodePath = "GolfBall";
	
	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		GD.Print("hey");
		//await Task.Delay(1000);
		//Main2();
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{	
		float epslion = 0.001f;
		RigidBody3D ballNode = GetNode<RigidBody3D>("GolfBall");
		//GD.Print(ballNode.LinearVelocity.Length());
		if (ballNode.LinearVelocity.Length() < epslion) {
			Main2();
		}
	}
	
	private void Main2()
	{
		using var capture = new VideoCapture(0);
		List<double> finalList = new List<double>();
		//System.Threading.Thread.Sleep(1000);
		if (!capture.IsOpened())
		{
			GD.Print("Camera not found!");
			return;
		}
		finalList = camera(capture);
		Cv2.DestroyAllWindows();
		GD.Print(finalList[0]);
		GD.Print(finalList[1]);
		
		RigidBody3D ballNode = GetNode<RigidBody3D>("GolfBall"); 
		Vector3 launch = new Vector3((float)finalList[1],0f,-(float)finalList[0]);
		ballNode.LinearVelocity = launch;
	}
	
	private static List<double> camera(VideoCapture capture)
	{
		// This method is intentionally left blank to demonstrate unused code.

		// using var window = new Cv2.Window("Camera");
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
		List<double> speedAngle = new List<double>();

		// Variables for State Control
		int captureFrameCount = 0;
		//DateTime pauseStartTime;
		const int PauseDurationSeconds = 5;

		const int BufferSize = 10; // The number of consecutive frames to use for the calculation
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
			//GD.Print(golfBallDetected ? "Golf Ball Detected!" : "No Golf Ball.");
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
				//GD.Print((DateTime.Now - pauseStartTime).TotalSeconds);
				if ((DateTime.Now - pauseStartTime).TotalSeconds >= PauseDurationSeconds)
				{
					GD.Print((DateTime.Now - pauseStartTime).TotalSeconds);
					isPaused = false;
					pauseStartTime = DateTime.Now;
					centerBuffer.Clear();
					timeBuffer.Clear(); 
					GD.Print("\nPause finished. Ready for new detection.");
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
				
				GD.Print(centerBuffer.Count);
					// 2. Maintain buffer size

				if (centerBuffer.Count > BufferSize)
					{
						centerBuffer.Dequeue(); // Remove the oldest point
						timeBuffer.Dequeue();   // Remove the oldest time
					}

					// 3. Check if we have enough data to calculate
					if (centerBuffer.Count == BufferSize)
					{
						speedAngle = CalculateAndDisplaySpeed(centerBuffer, timeBuffer, PixelsPerCm, BufferSize);
						
						// --- START PAUSE AFTER A SUCCESSFUL MEASUREMENT ---
						isPaused = true;
						pauseStartTime = DateTime.Now;
						GD.Print($"Detection paused for {PauseDurationSeconds} seconds...");
						return speedAngle;
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
			Cv2.ImShow("feed",currentFrame);

			int key = Cv2.WaitKey(1);
			if (key == 27) // ESC key
				break;
		}
		return speedAngle;
	}
	
	private static List<double> CalculateAndDisplaySpeed(Queue<Point2f> centers, Queue<double> times, double PixelsPerCm,int BufferSize)
		{
		// Get the oldest point/time (start)
	Point2f centerStart = centers.Peek(); 
	double timeStart = times.Peek();
	List<double> l = new List<double>(); 

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
		GD.Print($"\n--- AVERAGE SPEED & ANGLE CALCULATED (Over {BufferSize} frames) ---");
		GD.Print($"Net Distance: {distanceCm:F2} cm");
		GD.Print($"Total Time Elapsed: {timeSeconds * 1000:F2} ms");
		GD.Print($"Average Rolling Speed: {speedMetersPerSecond:F2} m/s");
		GD.Print($"Horizontal Angle: {angleDegrees:F2} degrees");
		Console.ResetColor();
		l.Add(speedMetersPerSecond);
		l.Add(angleRadians);
		return l;
	}
	else
	{
		GD.Print("Error: Time elapsed was zero or negative.");
	}
	return l;
	}
	
	
}
