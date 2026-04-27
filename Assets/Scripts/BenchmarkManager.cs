// using UnityEngine;

// public class BenchmarkManager
// {
//     Stopwatch stopwatch = Stopwatch.StartNew();

//     List<Vector2> hull = JarvisHull.ComputeHull(pointManager.Points);

//     stopwatch.Stop();

//     float ms = stopwatch.ElapsedTicks * 1000f / Stopwatch.Frequency;
//     jarvisTimeText.text = $"Jarvis : {ms:F4} ms";
// }