namespace MouthOfTruth.Game.Face
{
    public class FaceCaptureResult
    {
        public FaceCaptureResult(string faceFramesDirectoryPath, int capturedFrameCount)
        {
            FaceFramesDirectoryPath = string.IsNullOrEmpty(faceFramesDirectoryPath) ? string.Empty : faceFramesDirectoryPath;
            CapturedFrameCount = capturedFrameCount;
        }

        public string FaceFramesDirectoryPath { get; }

        public int CapturedFrameCount { get; }
    }
}
