using System.Speech.Synthesis;

using Clifton.Core.ModuleManagement;

namespace ModuleVoiceSpeak
{
	public class Speak : IModule
	{
		public void Say(string text)
		{
			SpeechSynthesizer synth = new SpeechSynthesizer();
			synth.SetOutputToDefaultAudioDevice();
			synth.Speak(text);
		}
	}
}
