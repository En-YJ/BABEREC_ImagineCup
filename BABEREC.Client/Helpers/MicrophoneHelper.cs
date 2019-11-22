using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage;

namespace BABEREC.Client.Helpers
{
    /// <summary>
    /// 마이크 헬퍼
    /// </summary>
    internal class MicrophoneHelper
    {
        private const uint CHANNEL = 1;
        private const uint BITS_PER_SAMPLE = 16;            //음성을 텍스트로 바꾸는데 가장 적당한 타입
        private const uint SAMPLE_RATE = 16000;

        private AudioFileOutputNode _audioFileOutputNode;
        private AudioGraph _audioGraph;             //추후에 여기를 작업해야지 이기능을 수정해야지 업그레이드 가능(뭐가 달라지는지는 모르겠음)
        private string _outputFilename;
        private StorageFile _storageFile;

        /// <summary>
        ///     레코딩 시작
        /// </summary>
        /// <param name="recordingFilename"></param>
        /// <returns></returns>
        public async Task StartRecordingAsync(string recordingFilename)
        {
            if (_outputFilename == recordingFilename) return;
            _outputFilename = recordingFilename;
            _storageFile = await ApplicationData.Current.LocalFolder
                .CreateFileAsync(_outputFilename, CreationCollisionOption.ReplaceExisting);

            if (_audioGraph == null) await InitialiseAudioGraph();
            await InitialiseAudioFileOutputNode();
            await InitialiseAudioFeed();

            _audioGraph.Start();
        }

        /// <summary>
        /// 오디오 그래프 초기화
        /// </summary>
        /// <returns></returns>
        private async Task InitialiseAudioGraph()
        {
            // Prompt the user for permission to access the microphone. This request will only happen
            // once, it will not re-prompt if the user rejects the permission.
            var permissionGained = await AudioCapturePermissions.RequestMicrophonePermission();
            if (permissionGained == false) throw new InvalidOperationException("Failed to get microphone rights!");

            var audioGraphSettings = new AudioGraphSettings(AudioRenderCategory.Speech);
            var audioGraphResult = await AudioGraph.CreateAsync(audioGraphSettings);

            if (audioGraphResult.Status != AudioGraphCreationStatus.Success)
                throw new InvalidOperationException("AudioGraph creation error !");

            _audioGraph = audioGraphResult.Graph;
            //_audioGraph.QuantumStarted
        }

        /// <summary>
        /// 오디오 파일 초기화
        /// </summary>
        /// <returns></returns>
        private async Task InitialiseAudioFileOutputNode()
        {
            if (_audioGraph == null) return;
            var outputProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
            outputProfile.Audio = AudioEncodingProperties.CreatePcm(SAMPLE_RATE, CHANNEL, BITS_PER_SAMPLE);

            var outputResult = await _audioGraph.CreateFileOutputNodeAsync(_storageFile, outputProfile);

            if (outputResult.Status != AudioFileNodeCreationStatus.Success)
                throw new InvalidOperationException("AudioFileNode creation error !");

            _audioFileOutputNode = outputResult.FileOutputNode;
        }

        /// <summary>
        /// 오디오 피드 초기화
        /// </summary>
        /// <returns></returns>
        private async Task InitialiseAudioFeed()
        {
            if (_audioGraph == null || _audioFileOutputNode == null) return;

            var defaultAudioCaptureId = MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default);
            var microphone = await DeviceInformation.CreateFromIdAsync(defaultAudioCaptureId);

            var inputProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
            var inputResult =
                await _audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Speech, inputProfile.Audio, microphone);

            if (inputResult.Status != AudioDeviceNodeCreationStatus.Success)
                throw new InvalidOperationException("AudioDeviceNode creation error !");

            inputResult.DeviceInputNode.AddOutgoingConnection(_audioFileOutputNode);
        }

        /// <summary>
        ///     레코딩 종료
        /// </summary>
        /// <returns></returns>
        public async Task StopRecordingAsync()
        {
            if (_audioGraph == null)
                throw new NullReferenceException("You have to start recording first !");

            if (_outputFilename == null)
                throw new NullReferenceException("You have to start recording first !");

            _audioGraph.Stop();
            await _audioFileOutputNode.FinalizeAsync();
        }

        /// <summary>
        ///     레코딩 삭제
        /// </summary>
        /// <returns></returns>
        public async Task RemoveRecordingAsync(string removeFileName = null)
        {
            if (_outputFilename == null)
                throw new NullReferenceException("You have to start recording first !");
            try
            {
                if (string.IsNullOrEmpty(removeFileName)) removeFileName = _outputFilename;
                var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(removeFileName);
                if (item == null) return;
                await item.DeleteAsync();
                _outputFilename = string.Empty;
            }
            catch (Exception)
            {
            }
        }
    }
}
