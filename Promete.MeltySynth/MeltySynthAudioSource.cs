﻿using MeltySynth;
using Promete.Audio;

namespace Promete.MeltySynth;

public class MeltySynthAudioSource : IAudioSource
{
    private readonly short[] _bufferLeft;
    private readonly short[] _bufferRight;

    private readonly object _mutex;
    private readonly MidiFileSequencer _sequencer;

    private readonly Synthesizer _synthesizer;

    public MeltySynthAudioSource(string soundFontPath)
    {
        _synthesizer = new Synthesizer(soundFontPath, SampleRate);
        _sequencer = new MidiFileSequencer(_synthesizer);

        _bufferLeft = new short[2000];
        _bufferRight = new short[2000];

        _mutex = new object();
    }

    public int? Samples => null;
    public int Channels => 2;
    public int Bits => 16;
    public int SampleRate => 44100;

    public (int loadedSize, bool isFinished) FillSamples(short[] buffer, int offset)
    {
        lock (_mutex)
        {
            _sequencer.RenderInt16(_bufferLeft, _bufferRight);
        }

        for (var t = 0; t < _bufferLeft.AsSpan().Length; t++)
        {
            buffer[t * 2] = _bufferLeft[t];
            buffer[t * 2 + 1] = _bufferRight[t];
        }

        return (_bufferLeft.Length, false);
    }

    public void Play(MidiFile midiFile, bool loop)
    {
        lock (_mutex)
        {
            _sequencer.Play(midiFile, loop);
        }
    }

    public void Stop()
    {
        lock (_mutex)
        {
            _sequencer.Stop();
        }
    }
}
