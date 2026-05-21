using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MouthOfTruth.Game.Face
{
    public class WebcamFaceCaptureInputAdapter : IFaceCaptureInputAdapter
    {
        private const float CAPTURE_INTERVAL_SECONDS = 0.15f;
        private const int JPEG_QUALITY = 80;
        private const int MINIMUM_READY_DIMENSION = 16;

        private Texture2D mCaptureTexture;
        private WebCamTexture mWebCamTexture;
        private string mActiveCaptureDirectoryPath;
        private string mSelectedDeviceName;
        private float mElapsedSinceCaptureSeconds;
        private int mCapturedFrameCount;
        private bool mIsCollecting;

        public WebcamFaceCaptureInputAdapter()
        {
            mSelectedDeviceName = selectDefaultDeviceName();
        }

        public bool HasAvailableDevice()
        {
            return string.IsNullOrWhiteSpace(mSelectedDeviceName) == false;
        }

        public void Reset()
        {
            stopCollection();
            deleteCaptureDirectoryIfSafe();
            clearCaptureState();
        }

        public void BeginCollection(string questionID)
        {
            if (HasAvailableDevice() == false)
            {
                return;
            }

            stopCollection();
            deleteCaptureDirectoryIfSafe();
            clearCaptureState();
            mActiveCaptureDirectoryPath = FaceFrameWorkspacePaths.BuildCaptureDirectoryPath(questionID);
            Directory.CreateDirectory(mActiveCaptureDirectoryPath);
            mCapturedFrameCount = 0;
            mElapsedSinceCaptureSeconds = CAPTURE_INTERVAL_SECONDS;
            ensureWebCamTextureStarted();
            mIsCollecting = true;
        }

        public void PauseCollection()
        {
            mIsCollecting = false;
        }

        public void ResumeCollection()
        {
            if (HasAvailableDevice() == false)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(mActiveCaptureDirectoryPath))
            {
                BeginCollection("question");
                return;
            }

            ensureWebCamTextureStarted();
            mElapsedSinceCaptureSeconds = CAPTURE_INTERVAL_SECONDS;
            mIsCollecting = true;
        }

        public void CancelCollection()
        {
            Reset();
        }

        public void Update(float deltaTimeSeconds)
        {
            if (mIsCollecting == false || mWebCamTexture == null || mWebCamTexture.isPlaying == false)
            {
                return;
            }

            if (mWebCamTexture.didUpdateThisFrame == false)
            {
                return;
            }

            if (mWebCamTexture.width < MINIMUM_READY_DIMENSION || mWebCamTexture.height < MINIMUM_READY_DIMENSION)
            {
                return;
            }

            mElapsedSinceCaptureSeconds += Mathf.Max(0.0f, deltaTimeSeconds);

            if (mElapsedSinceCaptureSeconds < CAPTURE_INTERVAL_SECONDS)
            {
                return;
            }

            captureCurrentFrame();
            mElapsedSinceCaptureSeconds = 0.0f;
        }

        public Task<FaceCaptureResult> CompleteCollectionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stopCollection();

            string completedDirectoryPath = mCapturedFrameCount > 0
                ? mActiveCaptureDirectoryPath
                : string.Empty;
            FaceCaptureResult faceCaptureResult = new FaceCaptureResult(completedDirectoryPath, mCapturedFrameCount);
            clearCaptureState();
            return Task.FromResult(faceCaptureResult);
        }

        private void captureCurrentFrame()
        {
            if (string.IsNullOrWhiteSpace(mActiveCaptureDirectoryPath))
            {
                return;
            }

            int captureWidth = mWebCamTexture.width;
            int captureHeight = mWebCamTexture.height;

            if (mCaptureTexture == null || mCaptureTexture.width != captureWidth || mCaptureTexture.height != captureHeight)
            {
                mCaptureTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            }

            mCaptureTexture.SetPixels32(mWebCamTexture.GetPixels32());
            mCaptureTexture.Apply(false, false);

            byte[] jpgBytes = ImageConversion.EncodeToJPG(mCaptureTexture, JPEG_QUALITY);
            string frameFilePath = Path.Combine(mActiveCaptureDirectoryPath, $"frame_{mCapturedFrameCount + 1:D5}.jpg");
            File.WriteAllBytes(frameFilePath, jpgBytes);
            mCapturedFrameCount += 1;
        }

        private void ensureWebCamTextureStarted()
        {
            if (mWebCamTexture != null && mWebCamTexture.isPlaying)
            {
                return;
            }

            if (HasAvailableDevice() == false)
            {
                throw new InvalidOperationException("No webcam input device is available.");
            }

            if (mWebCamTexture == null)
            {
                mWebCamTexture = new WebCamTexture(mSelectedDeviceName);
            }

            mWebCamTexture.Play();
        }

        private void stopCollection()
        {
            mIsCollecting = false;

            if (mWebCamTexture != null && mWebCamTexture.isPlaying)
            {
                mWebCamTexture.Stop();
            }
        }

        private void deleteCaptureDirectoryIfSafe()
        {
            if (string.IsNullOrWhiteSpace(mActiveCaptureDirectoryPath))
            {
                return;
            }

            if (Directory.Exists(mActiveCaptureDirectoryPath) == false)
            {
                return;
            }

            if (mCapturedFrameCount > 0)
            {
                return;
            }

            Directory.Delete(mActiveCaptureDirectoryPath, recursive: true);
        }

        private void clearCaptureState()
        {
            mActiveCaptureDirectoryPath = string.Empty;
            mCapturedFrameCount = 0;
            mElapsedSinceCaptureSeconds = 0.0f;

            if (mCaptureTexture != null)
            {
                UnityEngine.Object.Destroy(mCaptureTexture);
                mCaptureTexture = null;
            }
        }

        private string selectDefaultDeviceName()
        {
            if (WebCamTexture.devices == null || WebCamTexture.devices.Length == 0)
            {
                return string.Empty;
            }

            return WebCamTexture.devices[0].name;
        }
    }
}
