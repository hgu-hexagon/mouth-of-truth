namespace MouthOfTruth.Game.Face
{
    public class FaceCaptureResult
    {
        public FaceCaptureResult(
            string faceFramesDirectoryPath,
            int capturedFrameCount)
        {
            FaceFramesDirectoryPath = faceFramesDirectoryPath ?? string.Empty;
            CapturedFrameCount = capturedFrameCount;
        }

        public string FaceFramesDirectoryPath { get; }

        public int CapturedFrameCount { get; }
    }
}
