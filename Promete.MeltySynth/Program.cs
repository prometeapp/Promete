// See https://aka.ms/new-console-template for more information

using MeltySynth;
using Promete;
using Promete.Audio;
using Promete.GLDesktop;
using Promete.MeltySynth;

var app = PrometeApp.Create()
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>();

namespace Promete.MeltySynth
{
    public class MainScene : Scene
    {
        public override void OnStart()
        {
            var audioPlayer = new AudioPlayer();
            var source = new MeltySynthAudioSource("./soundfont.sf2");
            source.Play(new MidiFile("song.mid", MidiFileLoopType.RpgMaker), true);

            audioPlayer.BufferSize = 4000;
            audioPlayer.Play(source);
        }
    }
}
