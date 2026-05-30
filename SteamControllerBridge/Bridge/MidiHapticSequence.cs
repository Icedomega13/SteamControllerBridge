namespace SteamControllerBridge.Bridge;

internal sealed class MidiHapticSequence
{
    public MidiHapticSequence(IReadOnlyList<MidiHapticEvent> events)
    {
        Events = events;
    }

    public IReadOnlyList<MidiHapticEvent> Events { get; }

    public static MidiHapticSequence Load(string path)
    {
        var reader = new MidiReader(File.ReadAllBytes(path));
        return reader.Read();
    }
}

internal sealed record MidiHapticEvent(int DelayMs, int Channel, int Note, ushort Frequency, byte Velocity, int DurationMs);

internal sealed class MidiReader
{
    private readonly byte[] _data;
    private int _position;

    public MidiReader(byte[] data)
    {
        _data = data;
    }

    public MidiHapticSequence Read()
    {
        Expect("MThd");
        var headerLength = ReadInt32();
        if (headerLength < 6)
        {
            throw new InvalidDataException("Invalid MIDI header.");
        }

        _ = ReadInt16();
        var trackCount = ReadInt16();
        var division = ReadInt16();
        if ((division & 0x8000) != 0)
        {
            throw new InvalidDataException("SMPTE MIDI timing is not supported.");
        }

        _position += headerLength - 6;
        var ticksPerQuarter = Math.Max(1, division);
        var merged = new List<TimedMidiNote>();
        for (var track = 0; track < trackCount && _position < _data.Length; track++)
        {
            merged.AddRange(ReadTrack(ticksPerQuarter));
        }

        var ordered = merged
            .OrderBy(e => e.TimeMs)
            .ToList();
        var events = new List<MidiHapticEvent>();
        var previous = 0;
        foreach (var note in ordered)
        {
            var delay = Math.Clamp(note.TimeMs - previous, 0, 4000);
            previous = note.TimeMs;
            events.Add(new MidiHapticEvent(delay, note.Channel, note.Note, NoteToFrequency(note.Note), note.Velocity, note.DurationMs));
        }

        return new MidiHapticSequence(events);
    }

    private List<TimedMidiNote> ReadTrack(int ticksPerQuarter)
    {
        Expect("MTrk");
        var length = ReadInt32();
        var end = Math.Min(_data.Length, _position + length);
        var notes = new List<TimedMidiNote>();
        var activeNotes = new Dictionary<(int Channel, int Note), Queue<ActiveMidiNote>>();
        var absoluteTicks = 0;
        var tempoMicros = 500_000;
        var runningStatus = 0;
        var pendingMicros = 0.0;
        var absoluteMs = 0;

        while (_position < end)
        {
            var delta = ReadVariableLength();
            absoluteTicks += delta;
            pendingMicros += delta * (tempoMicros / (double)ticksPerQuarter);
            var wholeMs = (int)(pendingMicros / 1000.0);
            if (wholeMs > 0)
            {
                absoluteMs += wholeMs;
                pendingMicros -= wholeMs * 1000.0;
            }

            var status = ReadByte();
            if (status < 0x80)
            {
                if (runningStatus == 0)
                {
                    throw new InvalidDataException("MIDI running status without previous status.");
                }

                _position--;
                status = runningStatus;
            }
            else if (status < 0xF0)
            {
                runningStatus = status;
            }

            if (status == 0xFF)
            {
                var metaType = ReadByte();
                var metaLength = ReadVariableLength();
                if (metaType == 0x2F)
                {
                    _position += metaLength;
                    break;
                }

                if (metaType == 0x51 && metaLength == 3)
                {
                    tempoMicros = (ReadByte() << 16) | (ReadByte() << 8) | ReadByte();
                }
                else
                {
                    _position += metaLength;
                }

                continue;
            }

            if (status == 0xF0 || status == 0xF7)
            {
                _position += ReadVariableLength();
                continue;
            }

            var command = status & 0xF0;
            var channel = status & 0x0F;
            if (command == 0xC0 || command == 0xD0)
            {
                _position++;
                continue;
            }

            var first = ReadByte();
            var second = ReadByte();
            if (command == 0x90 && second > 0)
            {
                var key = (channel, first);
                if (!activeNotes.TryGetValue(key, out var queue))
                {
                    queue = new Queue<ActiveMidiNote>();
                    activeNotes[key] = queue;
                }

                queue.Enqueue(new ActiveMidiNote(absoluteTicks, absoluteMs, (byte)Math.Clamp(second, 1, 127)));
            }
            else if (command == 0x80 || command == 0x90)
            {
                CloseActiveNote(activeNotes, notes, channel, first, absoluteTicks, absoluteMs);
            }
        }

        foreach (var pair in activeNotes)
        {
            while (pair.Value.Count > 0)
            {
                CloseActiveNote(activeNotes, notes, pair.Key.Channel, pair.Key.Note, absoluteTicks, absoluteMs);
            }
        }

        _position = end;
        return notes;
    }

    private static void CloseActiveNote(
        Dictionary<(int Channel, int Note), Queue<ActiveMidiNote>> activeNotes,
        List<TimedMidiNote> notes,
        int channel,
        int note,
        int endTick,
        int endMs)
    {
        var key = (channel, note);
        if (!activeNotes.TryGetValue(key, out var queue) || queue.Count == 0)
        {
            return;
        }

        var started = queue.Dequeue();
        notes.Add(new TimedMidiNote(
            started.Tick,
            started.TimeMs,
            Math.Max(started.TimeMs + 30, endMs),
            channel,
            note,
            started.Velocity));
    }

    private static ushort NoteToFrequency(int note)
    {
        var frequency = 440.0 * Math.Pow(2, (note - 69) / 12.0);
        return (ushort)Math.Clamp((int)Math.Round(frequency), 30, 5000);
    }

    private void Expect(string marker)
    {
        foreach (var expected in marker)
        {
            if (ReadByte() != expected)
            {
                throw new InvalidDataException($"Expected MIDI marker {marker}.");
            }
        }
    }

    private int ReadInt16()
    {
        return (ReadByte() << 8) | ReadByte();
    }

    private int ReadInt32()
    {
        return (ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte();
    }

    private int ReadVariableLength()
    {
        var value = 0;
        for (var i = 0; i < 4; i++)
        {
            var b = ReadByte();
            value = (value << 7) | (b & 0x7F);
            if ((b & 0x80) == 0)
            {
                return value;
            }
        }

        return value;
    }

    private int ReadByte()
    {
        if (_position >= _data.Length)
        {
            throw new EndOfStreamException("Unexpected end of MIDI file.");
        }

        return _data[_position++];
    }

    private sealed record ActiveMidiNote(int Tick, int TimeMs, byte Velocity);

    private sealed record TimedMidiNote(int Tick, int TimeMs, int EndMs, int Channel, int Note, byte Velocity)
    {
        public int DurationMs => Math.Max(30, EndMs - TimeMs);
    }
}
