using ManagedBass;

namespace SekoRenderer;

public class AudiosurfMediaPlayer
{
    public int UsedChannel { get; set; }
    public bool UsesFloat { get; set; } = true;
    
    public bool Init()
    {
        //Load FLAC plugin
        //Load WMA plugin
        
        //maybe 0xffffffff
        var bassInit = Bass.Init();
        
        return bassInit;
    }

    public double GetDuration(string path)
    {
        var theFlags = (BassFlags)0x80020100; // FLOAT, PRESCAN, DECODE
        var otherFlags = (BassFlags)0x80020000; // PRESCAN, DECODE
        var tempChannel = Bass.CreateStream(path, Flags: theFlags);
        var durationBytes = Bass.ChannelGetLength(tempChannel);
        var duration = Bass.ChannelBytes2Seconds(tempChannel, durationBytes);
        Bass.StreamFree(tempChannel);
        return duration;
    }
    
    public bool Play(string path)
    {
        var theFlags = (BassFlags)0x80020100; // FLOAT, PRESCAN, DECODE
        var otherFlags = (BassFlags)0x80020000; // PRESCAN, DECODE
        Bass.StreamFree(UsedChannel);
        UsedChannel = Bass.CreateStream(path, Flags: theFlags);
        var result = Bass.ChannelPlay(UsedChannel);
        return result;
    }
    
    public bool StartPrescan(string path)
    {
        var theFlags = (BassFlags)0x80020100; // FLOAT, PRESCAN, DECODE
        var otherFlags = (BassFlags)0x80020000; // PRESCAN, DECODE
        Bass.StreamFree(UsedChannel);
        UsedChannel = Bass.CreateStream(path, Flags: theFlags);
        return UsedChannel != 0;
    }
    
    public double GetPosition()
    {
        var positionBytes = Bass.ChannelGetPosition(UsedChannel);
        var position = Bass.ChannelBytes2Seconds(UsedChannel, positionBytes);
        return position;
    }
    
    public bool SetPosition(double position)
    {
        var positionBytes = Bass.ChannelSeconds2Bytes(UsedChannel, position);
        var result = Bass.ChannelSetPosition(UsedChannel, positionBytes);
        return result;
    }
    
    public bool SetVolume(double volume)
    {
        var result = Bass.ChannelSetAttribute(UsedChannel, ChannelAttribute.Volume, volume);
        return result;
    }
    
    public bool Stop()
    {
        var result = Bass.ChannelStop(UsedChannel);
        return result;
    }
    
    public bool Pause()
    {
        var result = Bass.ChannelPause(UsedChannel);
        return result;
    }
    
    public bool Resume()
    {
        var result = Bass.ChannelPlay(UsedChannel);
        return result;
    }
    
    public int GetFFT512KISS(float[] fftData)
    {
        //idk
        var channelInfo = Bass.ChannelGetInfo(UsedChannel);

        
        
        return -1;
    }
}